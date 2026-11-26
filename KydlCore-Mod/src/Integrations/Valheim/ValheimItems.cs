using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using KydlCore.Common;
using KydlCore.Core;
using KydlCore.Features.Items.Contracts;
using UnityEngine;

namespace KydlCore.Integrations.Valheim;

/// <summary>
/// The Valheim side of KY-Items without Jotunn (KydlCore 1.2.0, D-104; replaces Integrations/Jotunn/JotunnItems). A contribution's prefab goes into
/// the network scene (KyPrefabs), items and their recipes into ObjectDB (every time an ObjectDB wakes up or copies the menu one), pieces into the
/// Hammer's table in the KY-Items build category (KyBuildCategory). LEGACY: prefabs that not-yet-migrated mods still add through Jotunn are read
/// from Jotunn's ModRegistry by reflection when Jotunn is installed. Game code: tested in game (TestLab KydlCore suite).
/// </summary>
internal sealed class ValheimItems : IKyGameSide
{
    static readonly List<KeyValuePair<string, KyContribution>> Pending = new();
    static readonly List<Action<string>> Listeners = new();
    static readonly MethodInfo UpdateRegisters = AccessTools.Method(typeof(ObjectDB), "UpdateRegisters");

    public bool Hook(Action<string> refresh)
    {
        bool hooked = refresh != null;
        if (hooked && !Listeners.Contains(refresh)) Listeners.Add(refresh);
        ModLog.Verbose($"ValheimItems.Hook: listener {(hooked ? "added" : "missing")} - listeners={Listeners.Count} (called after every ObjectDB set-up)");
        return hooked;
    }

    public Piece.PieceCategory? Category
    {
        get
        {
            Piece.PieceCategory? cat = KyBuildCategory.Find(KyItems.CategoryName);
            if (CoreLog.Due("ValheimItems.Category", 5d)) ModLog.Every("ValheimItems.Category", 5d, $"ValheimItems.Category: '{KyItems.CategoryName}' = {(cat.HasValue ? ((int)cat.Value).ToString() : "<none>")}");
            return cat;
        }
    }

    public IEnumerable<KyFound> LegacyScan()
    {
        var found = new List<KyFound>();
        Type registry = KyBuildCategory.JotunnPresent ? AccessTools.TypeByName("Jotunn.Utils.ModRegistry") : null;
        if (registry != null)
        {
            Read(registry, "GetItems", "ItemPrefab", KyKind.Item, found);
            Read(registry, "GetPieces", "PiecePrefab", KyKind.Piece, found);
        }
        ModLog.Verbose($"ValheimItems.LegacyScan: Jotunn {(registry == null ? "not installed - nothing to read" : "installed")} - {found.Count} prefab(s) of any mod (the registry keeps the com.kydldev.* ones)");
        return found;
    }

    static int Read(Type registry, string method, string prefabProperty, KyKind kind, List<KyFound> found)
    {
        int read = 0;
        try
        {
            var all = AccessTools.Method(registry, method, Type.EmptyTypes)?.Invoke(null, null) as IEnumerable;
            if (all != null)
                foreach (object entry in all)
                {
                    var t = Traverse.Create(entry);
                    var prefab = t.Property(prefabProperty).GetValue<GameObject>();
                    object source = t.Property("SourceMod").GetValue();
                    var guid = source == null ? null : Traverse.Create(source).Property("GUID").GetValue<string>();
                    var name = source == null ? null : Traverse.Create(source).Property("Name").GetValue<string>();
                    if (prefab != null && guid != null) { found.Add(new KyFound(prefab, guid, name, kind)); read++; }
                }
        }
        catch (TargetInvocationException e) { ModLog.Warn($"ValheimItems: Jotunn's ModRegistry.{method} failed ({e.InnerException?.GetType().FullName}: {e.InnerException?.Message}) - legacy {kind}s not listed"); }
        ModLog.Verbose($"ValheimItems.Read: ModRegistry.{method} -> {read} {kind}(s)");
        return read;
    }

    public IEnumerable<GameObject> PiecesIn(Piece.PieceCategory category)
    {
        PieceTable table = Hammer();
        var pieces = table == null ? new List<GameObject>() : table.m_pieces.Where(p => p != null && p.GetComponent<Piece>() != null && p.GetComponent<Piece>().m_category == category).ToList();
        ModLog.Verbose($"ValheimItems.PiecesIn: category {(int)category} - {pieces.Count} piece(s) (hammer table={table != null})");
        return pieces;
    }

    public KyResult Register(KyContribution c, string id)
    {
        if (c.Kind == KyKind.Material && c.Prefab.GetComponent<ItemDrop>() == null)
        {
            ModLog.Verbose($"ValheimItems.Register: {id} rejected - a material without an ItemDrop");
            return KyResult.Rejected(id, "a material added to the game by KydlCore must be an item (ItemDrop); a deposit is added by its mod (RegisterInGame = false)");
        }
        if (c.Kind == KyKind.Piece && c.Prefab.GetComponent<Piece>() == null)
        {
            ModLog.Verbose($"ValheimItems.Register: {id} rejected - a piece without a Piece component");
            return KyResult.Rejected(id, "a piece must have a Piece component");
        }
        KyPrefabs.Add(c.Prefab);
        Pending.Add(new KeyValuePair<string, KyContribution>(id, c));
        int applied = ObjectDB.instance != null && ObjectDB.instance.m_items.Count > 0 ? Apply(ObjectDB.instance, true) : 0;
        ModLog.Verbose($"ValheimItems.Register: {id} ({c.Kind}) kept for the game - pending={Pending.Count}, applied now={applied}");
        return KyResult.Ok(id);
    }

    /// <summary>ObjectDB woke up (world) or copied the menu one: every contribution goes in, then the registry reads the game again. Expected outcome: the number of contributions put in.</summary>
    internal static int OnObjectDB(ObjectDB db, bool world)
    {
        int applied = Apply(db, world);
        foreach (Action<string> refresh in Listeners.ToArray()) refresh(world ? "KydlCore registered the items and pieces" : "the menu ObjectDB was copied");
        ModLog.Verbose($"ValheimItems.OnObjectDB: {(world ? "world" : "menu")} ObjectDB ({db.m_items.Count} items) - {applied} contribution(s) put in, listeners={Listeners.Count}");
        return applied;
    }

    static int Apply(ObjectDB db, bool withPieces)
    {
        int applied = 0;
        if (db == null || db.m_items.Count == 0) { ModLog.Verbose($"ValheimItems.Apply: no ObjectDB with items yet (db={db != null}) - nothing put in"); return applied; }
        foreach (var kv in Pending)
        {
            KyContribution c = kv.Value;
            bool ok = c.Kind == KyKind.Piece ? (withPieces && AddPiece(c, kv.Key)) : AddItem(db, c, kv.Key);
            if (ok) applied++;
        }
        UpdateRegisters?.Invoke(db, null);
        if (withPieces) KyBuildCategory.MarkRefresh();
        ModLog.Verbose($"ValheimItems.Apply: {applied} of {Pending.Count} contribution(s) in ObjectDB '{db.name}' (pieces {(withPieces ? "too" : "skipped: menu")})");
        return applied;
    }

    static bool AddItem(ObjectDB db, KyContribution c, string id)
    {
        int hash = c.Prefab.name.GetStableHashCode();
        bool known = db.m_items.Any(i => i != null && i.name.GetStableHashCode() == hash);
        if (!known) db.m_items.Add(c.Prefab);
        bool recipe = c.Recipe != null && AddRecipe(db, c, id);
        ModLog.Verbose($"ValheimItems.AddItem: {id} '{c.Prefab.name}' {(known ? "already in" : "added to")} ObjectDB, recipe={recipe}");
        return db.m_items.Contains(c.Prefab) || known;
    }

    static bool AddRecipe(ObjectDB db, KyContribution c, string id)
    {
        string name = $"Recipe_{c.Prefab.name}";
        if (db.m_recipes.Any(r => r != null && r.name == name)) { ModLog.Verbose($"ValheimItems.AddRecipe: {id} '{name}' already in ObjectDB"); return true; }
        var recipe = ScriptableObject.CreateInstance<Recipe>();
        recipe.name = name;
        recipe.m_item = c.Prefab.GetComponent<ItemDrop>();
        recipe.m_amount = Math.Max(1, c.Recipe.Amount);
        recipe.m_enabled = true;
        recipe.m_craftingStation = Station(c.Recipe.CraftingStation);
        recipe.m_minStationLevel = Math.Max(1, c.Recipe.MinStationLevel);
        recipe.m_resources = Requirements(c.Recipe, id);
        db.m_recipes.Add(recipe);
        ModLog.Verbose($"ValheimItems.AddRecipe: {id} '{name}' - station '{c.Recipe.CraftingStation}' found={recipe.m_craftingStation != null}, level {recipe.m_minStationLevel}, {recipe.m_resources.Length} requirement(s), amount {recipe.m_amount}");
        return recipe.m_item != null;
    }

    static bool AddPiece(KyContribution c, string id)
    {
        PieceTable table = Hammer();
        var piece = c.Prefab.GetComponent<Piece>();
        if (table == null) { ModLog.Verbose($"ValheimItems.AddPiece: {id} waits - the Hammer's table is not loaded"); return false; }
        piece.m_category = KyBuildCategory.GetOrCreate(KyItems.CategoryName);
        if (c.Recipe != null)
        {
            piece.m_craftingStation = Station(c.Recipe.CraftingStation);
            piece.m_resources = Requirements(c.Recipe, id);
        }
        bool known = table.m_pieces.Contains(c.Prefab);
        if (!known) table.m_pieces.Add(c.Prefab);
        ModLog.Verbose($"ValheimItems.AddPiece: {id} '{c.Prefab.name}' {(known ? "already in" : "added to")} the Hammer, category {(int)piece.m_category} ({KyItems.CategoryName}), {piece.m_resources.Length} requirement(s)");
        return table.m_pieces.Contains(c.Prefab);
    }

    static CraftingStation Station(string name)
    {
        CraftingStation station = string.IsNullOrEmpty(name) ? null : KyPrefabs.Get(name)?.GetComponent<CraftingStation>();
        if (!string.IsNullOrEmpty(name) && station == null) ModLog.Warn($"ValheimItems: crafting station '{name}' not found - the recipe needs no station");
        else if (CoreLog.On) ModLog.Verbose($"ValheimItems.Station: '{name}' -> {(station == null ? "<none>" : station.m_name)}");
        return station;
    }

    static Piece.Requirement[] Requirements(KyRecipe r, string id)
    {
        var list = new List<Piece.Requirement>();
        foreach (KyResource x in r.Resources ?? new KyResource[0])
        {
            if (x == null || string.IsNullOrEmpty(x.Item)) continue;
            var drop = KyPrefabs.Get(x.Item)?.GetComponent<ItemDrop>();
            if (drop == null) { ModLog.Warn($"ValheimItems: {id} needs '{x.Item}', which is not an item in the game - requirement left out"); continue; }
            list.Add(new Piece.Requirement { m_resItem = drop, m_amount = x.Amount, m_amountPerLevel = x.AmountPerLevel, m_recover = x.Recover });
        }
        ModLog.Verbose($"ValheimItems.Requirements: {id} - {list.Count} of {(r.Resources ?? new KyResource[0]).Length} requirement(s) resolved");
        return list.ToArray();
    }

    static PieceTable Hammer()
    {
        GameObject hammer = ObjectDB.instance == null ? null : ObjectDB.instance.GetItemPrefab("Hammer");
        ItemDrop drop = hammer == null ? null : hammer.GetComponent<ItemDrop>();
        PieceTable table = drop == null ? null : drop.m_itemData.m_shared.m_buildPieces;
        if (CoreLog.Due("ValheimItems.Hammer", 5d)) ModLog.Every("ValheimItems.Hammer", 5d, $"ValheimItems.Hammer: hammer={hammer != null}, table={(table == null ? "<none>" : table.name)}");
        return table;
    }
}
