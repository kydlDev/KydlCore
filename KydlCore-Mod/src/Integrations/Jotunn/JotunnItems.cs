using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using KydlCore.Common;
using KydlCore.Features.Items.Contracts;
using UnityEngine;
using KydlCore.Core;

namespace KydlCore.Integrations.Jotunn;

/// <summary>
/// The Jotunn side of KY-Items (MIGRATION_DECISIONS B13: the registry is Jotunn-free, the game side stays on Jotunn behind <see cref="IKyGameSide"/>
/// so it can be swapped later). Adds contributed prefabs to the game (items with their recipe, pieces to the hammer in the KY-Items category),
/// tells the registry when Jotunn registered everything, reads the build category and - LEGACY until every mod contributes - Jotunn's ModRegistry.
/// Game code: tested in game (TestLab KydlCore suite).
/// </summary>
internal sealed class JotunnItems : IKyGameSide
{
    public bool Hook(Action<string> refresh)
    {
        bool hooked = refresh != null;
        if (!hooked) { var returnedAt26 = hooked; if (CoreLog.Due("JotunnItems.Hook:26", 2d)) ModLog.Every("JotunnItems.Hook:26", 2d, $"JotunnItems.Hook: returns {CoreLog.Shown(returnedAt26)} (line 26)"); return returnedAt26; }
        try
        {
            ItemManager.OnItemsRegistered += () => refresh("Jotunn registered the items");
            PieceManager.OnPiecesRegistered += () => refresh("Jotunn registered the pieces");
            ModLog.Verbose("JotunnItems: waiting for Jotunn's OnItemsRegistered / OnPiecesRegistered");
        }
        catch (TypeLoadException e) { ModLog.Warn($"JotunnItems: Jotunn's registered events are not available ({e.Message})"); hooked = !hooked; }
        return hooked;
    }

    public Piece.PieceCategory? Category
    {
        get
        {
            try { return PieceManager.Instance.GetPieceCategory(KyItems.CategoryName); }      // null while no mod has registered it
            catch (Exception e) { ModLog.Verbose($"JotunnItems: the '{KyItems.CategoryName}' build category could not be read ({e.GetType().FullName}): {e.Message} - no category"); return null; }
        }
    }

    public IEnumerable<KyFound> LegacyScan()
    {
        var found = new List<KyFound>();
        try
        {
            foreach (var ci in ModRegistry.GetItems())
                if (ci != null && ci.ItemPrefab != null && ci.SourceMod != null) found.Add(new KyFound(ci.ItemPrefab, ci.SourceMod.GUID, ci.SourceMod.Name, KyKind.Item));
            foreach (var cp in ModRegistry.GetPieces())
                if (cp != null && cp.PiecePrefab != null && cp.SourceMod != null) found.Add(new KyFound(cp.PiecePrefab, cp.SourceMod.GUID, cp.SourceMod.Name, KyKind.Piece));
        }
        catch (TypeLoadException e) { ModLog.Warn($"JotunnItems: Jotunn's ModRegistry is not available ({e.Message})"); }
        catch (MissingMethodException e) { ModLog.Warn($"JotunnItems: Jotunn's ModRegistry is not available ({e.Message})"); }
        ModLog.Verbose($"JotunnItems: legacy scan found {found.Count} prefab(s) of any mod (the registry keeps the com.kydldev.* ones)");
        return found;
    }

    public IEnumerable<GameObject> PiecesIn(Piece.PieceCategory category)
    {
        try
        {
            GameObject hammer = ObjectDB.instance == null ? null : ObjectDB.instance.GetItemPrefab("Hammer");
            ItemDrop drop = hammer == null ? null : hammer.GetComponent<ItemDrop>();
            PieceTable table = drop == null ? null : drop.m_itemData.m_shared.m_buildPieces;
            if (table == null) { var returnedAt69 = new List<GameObject>(); if (CoreLog.Due("JotunnItems.PiecesIn:69", 2d)) ModLog.Every("JotunnItems.PiecesIn:69", 2d, $"JotunnItems.PiecesIn: returns {CoreLog.Shown(returnedAt69)} (line 69)"); return returnedAt69; }
            return table.m_pieces.Where(p => p != null && p.GetComponent<Piece>() != null && p.GetComponent<Piece>().m_category == category).ToList();
        }
        catch (NullReferenceException e) { ModLog.Verbose($"JotunnItems: the hammer's pieces could not be read: {e.Message}"); return new List<GameObject>(); }
    }

    public KyResult Register(KyContribution c, string id)
    {
        try
        {
            bool materialWithoutItem = c.Kind == KyKind.Material && c.Prefab.GetComponent<ItemDrop>() == null;
            if (materialWithoutItem)
            {
                if (CoreLog.On) ModLog.Verbose($"JotunnItems.Register: {id} rejected - a material without an ItemDrop");
                return KyResult.Rejected(id, "a material added to the game by KydlCore must be an item (ItemDrop); a deposit is added by its mod (RegisterInGame = false)");
            }
            if (c.Kind == KyKind.Item || c.Kind == KyKind.Material)
            {
                var cfg = new ItemConfig { Amount = c.Recipe != null ? Math.Max(1, c.Recipe.Amount) : 1 };
                if (c.Recipe != null)
                {
                    cfg.CraftingStation = c.Recipe.CraftingStation ?? "";
                    cfg.MinStationLevel = Math.Max(1, c.Recipe.MinStationLevel);
                    cfg.Requirements = Requirements(c.Recipe);
                }
                bool added = ItemManager.Instance.AddItem(new CustomItem(c.Prefab, true, cfg));
                ModLog.Verbose($"JotunnItems: item '{id}' {(added ? "added" : "NOT added")} to the game");
                return added ? KyResult.Ok(id) : KyResult.Rejected(id, "Jotunn did not add the item (name taken?)");
            }
            var pcfg = new PieceConfig { PieceTable = "Hammer", Category = KyItems.CategoryName };
            if (c.Recipe != null) { pcfg.CraftingStation = c.Recipe.CraftingStation ?? ""; pcfg.Requirements = Requirements(c.Recipe); }
            bool placed = PieceManager.Instance.AddPiece(new CustomPiece(c.Prefab, true, pcfg));
            ModLog.Verbose($"JotunnItems: piece '{id}' {(placed ? $"added to the hammer, category {KyItems.CategoryName}" : "NOT added")}");
            return placed ? KyResult.Ok(id) : KyResult.Rejected(id, "Jotunn did not add the piece (name taken?)");
        }
        catch (Exception e)       // last resort: Jotunn throws different types for a broken prefab; the reason names it and the contribution is refused
        {
            ModLog.Warn($"JotunnItems: '{id}' could not be added to the game: {e}");
            return KyResult.Rejected(id, $"{e.GetType().Name}: {e.Message}");
        }
    }

    static RequirementConfig[] Requirements(KyRecipe r)
    {
        if (CoreLog.On) ModLog.Verbose($"JotunnItems.Requirements(r={CoreLog.Shown(r)})");
        return (r.Resources ?? new KyResource[0]).Where(x => x != null && !string.IsNullOrEmpty(x.Item))
            .Select(x => new RequirementConfig { Item = x.Item, Amount = x.Amount, AmountPerLevel = x.AmountPerLevel, Recover = x.Recover }).ToArray();
    }
}
