using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Bootstrap;
using HarmonyLib;
using KydlCore.Common;
using KydlCore.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KydlCore.Integrations.Valheim;

/// <summary>
/// Custom Hammer build categories (the shared "KY-Items" tab, rule 10) without Jotunn (KydlCore 1.2.0, D-104 / D-106). Ported from Jotunn 2.30.2
/// PieceManager (only the parts a category needs): a PieceCategory number above the game's own ones, Enum.GetValues / GetNames that list it, one more
/// Hud tab, PieceTable arrays sized for it, and the "by usage" build list (ByUsagePieceList) showing it as a tag.
/// When Jotunn is installed (other mods of the player still bring it), Jotunn already patches all of this: KydlCore then asks Jotunn's PieceManager
/// for the category (by reflection, no reference) and its own patches stay off, so the two never fight over the numbers or the tabs.
/// Game code: tested in game (TestLab KydlCore suite, the Hammer's KY-Items tab).
/// </summary>
internal static class KyBuildCategory
{
    internal const string JotunnGuid = "com.jotunn.jotunn";
    const string TokenPrefix = "kydlcore_cat_";
    static readonly Dictionary<string, Piece.PieceCategory> Own = new();
    static readonly Dictionary<Piece.PieceCategory, string> VanillaLabels = new();
    static readonly Dictionary<object, Dictionary<int, Piece.PieceCategory>> CustomTags = new();
    static bool? _jotunn;
    static bool _refreshNeeded = true;

    static readonly AccessTools.FieldRef<ByUsagePieceList, Piece.UsageTagFlags[]> UsageTags = AccessTools.FieldRefAccess<ByUsagePieceList, Piece.UsageTagFlags[]>("m_usageTags");
    static readonly AccessTools.FieldRef<Player, PieceTable> BuildPieces = AccessTools.FieldRefAccess<Player, PieceTable>("m_buildPieces");
    static readonly System.Reflection.MethodInfo UpdateAvailablePieces = AccessTools.Method(typeof(Player), "UpdateAvailablePiecesList");
    static readonly AccessTools.FieldRef<ByUsagePieceList, List<int>> AvailableTags = AccessTools.FieldRefAccess<ByUsagePieceList, List<int>>("m_availableTags");

    /// <summary>True when Jotunn is loaded: then Jotunn makes the categories and KydlCore's patches stay off (decided once).</summary>
    internal static bool JotunnPresent
    {
        get
        {
            _jotunn ??= Chainloader.PluginInfos.ContainsKey(JotunnGuid);
            return _jotunn.Value;
        }
    }

    /// <summary>KydlCore's own category patches run only without Jotunn.</summary>
    internal static bool OwnPatches => !JotunnPresent;

    /// <summary>The game's "All" category value (100 in Valheim 1.0.21; read by name so a renumbering does not break it).</summary>
    static Piece.PieceCategory AllCategory => Enum.TryParse("All", out Piece.PieceCategory all) ? all : (Piece.PieceCategory)100;

    /// <summary>The game's own last category marker ("Max", 9 in Valheim 1.0.21).</summary>
    static Piece.PieceCategory VanillaMax => Enum.TryParse("Max", out Piece.PieceCategory max) ? max : (Piece.PieceCategory)9;

    /// <summary>The category called <paramref name="name"/>, made when it does not exist. Expected outcome: its number.</summary>
    internal static Piece.PieceCategory GetOrCreate(string name)
    {
        Piece.PieceCategory? known = Find(name);
        if (known.HasValue) { ModLog.Verbose($"KyBuildCategory.GetOrCreate: '{name}' exists = {(int)known.Value}"); return known.Value; }
        if (JotunnPresent) return ViaJotunn(name);
        int count = Enum.GetValues(typeof(Piece.PieceCategory)).Length;        // includes the custom ones (patched GetValues)
        var made = (Piece.PieceCategory)(count - 1);
        if (made >= AllCategory) made++;
        Own[name] = made;
        string token = Token(name);
        var table = ModContexts.For(typeof(KyBuildCategory)).English;
        table[token] = name;
        int words = GameText.AddIfLoaded(new Dictionary<string, string> { { token, name } });
        _refreshNeeded = true;
        CreateTabs();
        ModLog.State("KyBuildCategory", "<none>", name, $"new build category {(int)made}, label ${token}, words added now={words}");
        return made;
    }

    /// <summary>The category's number when it exists (a game one by name, KydlCore's, or Jotunn's). Expected outcome: the number or null.</summary>
    internal static Piece.PieceCategory? Find(string name)
    {
        Piece.PieceCategory? found = null;
        if (string.IsNullOrEmpty(name)) found = null;
        else if (Own.TryGetValue(name, out Piece.PieceCategory own)) found = own;
        else if (Enum.TryParse(name, true, out Piece.PieceCategory parsed)) found = parsed;
        else
        {
            Array values = Enum.GetValues(typeof(Piece.PieceCategory));
            string[] names = Enum.GetNames(typeof(Piece.PieceCategory));
            for (int i = 0; i < names.Length && i < values.Length; i++) if (names[i] == name) { found = (Piece.PieceCategory)values.GetValue(i); break; }
        }
        if (CoreLog.On) ModLog.Verbose($"KyBuildCategory.Find: '{name}' -> {(found.HasValue ? ((int)found.Value).ToString() : "<none>")}");
        return found;
    }

    static Piece.PieceCategory ViaJotunn(string name)
    {
        Type manager = AccessTools.TypeByName("Jotunn.Managers.PieceManager");
        object instance = manager == null ? null : AccessTools.Property(manager, "Instance")?.GetValue(null, null);
        var add = manager == null ? null : AccessTools.Method(manager, "AddPieceCategory", new[] { typeof(string) });
        var result = add == null || instance == null ? Piece.PieceCategory.Misc : (Piece.PieceCategory)add.Invoke(instance, new object[] { name });
        ModLog.State("KyBuildCategory", "<none>", name, $"made by Jotunn (installed by another mod) = {(int)result}, method found={add != null}");
        return result;
    }

    static string Token(string name)
    {
        string token = TokenPrefix + string.Join("_", (name ?? "").ToLowerInvariant().Split(" (){}[]+-!?/\\&%,.:-=<>\n".ToCharArray()));
        if (CoreLog.On) ModLog.Verbose($"KyBuildCategory.Token: '{name}' -> {token}");
        return token;
    }

    /// <summary>Number of category slots the game's arrays must have (vanilla count + the custom ones). Called from transpiled game code.</summary>
    internal static int MaxCategory()
    {
        int max = Enum.GetValues(typeof(Piece.PieceCategory)).Length - 1;
        if (max >= (int)AllCategory) max++;
        if (CoreLog.Due("KyBuildCategory.MaxCategory", 5d)) ModLog.Every("KyBuildCategory.MaxCategory", 5d, $"KyBuildCategory.MaxCategory: returns {max} (custom={Own.Count})"); 
        return max;
    }

    // ---- patch bodies (Integrations/Harmony/BuildCategoryPatches) ----

    internal static Array WithOwnValues(Array values)
    {
        if (Own.Count == 0) { if (CoreLog.Due("KyBuildCategory.WithOwnValues:0", 5d)) ModLog.Every("KyBuildCategory.WithOwnValues:0", 5d, $"KyBuildCategory.WithOwnValues: no custom category - {values.Length} value(s) unchanged"); return values; }
        var all = new Piece.PieceCategory[values.Length + Own.Count];
        values.CopyTo(all, 0);
        Own.Values.CopyTo(all, values.Length);
        if (CoreLog.Due("KyBuildCategory.WithOwnValues", 5d)) ModLog.Every("KyBuildCategory.WithOwnValues", 5d, $"KyBuildCategory.WithOwnValues: {values.Length} + {Own.Count} custom = {all.Length} value(s)"); 
        return all;
    }

    internal static string[] WithOwnNames(string[] names)
    {
        string[] all = Own.Count == 0 ? names : names.AddRangeToArray(Own.Keys.ToArray());
        if (CoreLog.Due("KyBuildCategory.WithOwnNames", 5d)) ModLog.Every("KyBuildCategory.WithOwnNames", 5d, $"KyBuildCategory.WithOwnNames: {names.Length} -> {all.Length} name(s)"); 
        return all;
    }

    internal static int ExpandAvailable(PieceTable table)
    {
        int added = 0;
        if (table.m_availablePieces.Count > 0 || Own.Count > 0)
        {
            var byCategory = Traverse.Create(table).Field("m_availablePiecesByCategory").GetValue<List<List<Piece>>>();
            if (byCategory != null && byCategory.Count > 0)
                for (int i = byCategory.Count; i < MaxCategory(); i++) { byCategory.Add(new List<Piece>()); added++; }
        }
        if (added > 0 && CoreLog.On) ModLog.Verbose($"KyBuildCategory.ExpandAvailable: {added} category list(s) added (max {MaxCategory()})");
        return added;
    }

    internal static int ResizeSelection(PieceTable table)
    {
        var tr = Traverse.Create(table);
        var byCategory = tr.Field("m_availablePiecesByCategory").GetValue<List<List<Piece>>>();
        int size = byCategory == null ? 0 : byCategory.Count;
        foreach (string field in new[] { "m_selectedPiece", "m_lastSelectedPiece" })
        {
            Vector2Int[] arr = tr.Field(field).GetValue<Vector2Int[]>();
            if (arr != null && arr.Length < size) { Array.Resize(ref arr, size); tr.Field(field).SetValue(arr); }
        }
        if (CoreLog.On) ModLog.Verbose($"KyBuildCategory.ResizeSelection: selection arrays sized to {size}");
        return size;
    }

    internal static int AddUsageTags(ByUsagePieceList list, PieceTable table)
    {
        int added = 0;
        if (table == null || Own.Count == 0) { if (CoreLog.Due("KyBuildCategory.AddUsageTags:0", 5d)) ModLog.Every("KyBuildCategory.AddUsageTags:0", 5d, $"KyBuildCategory.AddUsageTags: nothing to add - table={table != null}, custom={Own.Count}"); return added; }
        var byCategory = new Dictionary<Piece.PieceCategory, int>();
        var tags = new Dictionary<int, Piece.PieceCategory>();
        List<int> available = AvailableTags(list);
        int usageCount = UsageTags(list).Length;
        foreach (Piece piece in table.m_availablePieces)
        {
            if (piece == null || byCategory.ContainsKey(piece.m_category) || !Own.ContainsValue(piece.m_category)) continue;
            int id = usageCount + available.Count;
            byCategory[piece.m_category] = id;
            available.Add(id);
            tags[id] = piece.m_category;
            added++;
        }
        CustomTags[list] = tags;
        if (CoreLog.On) ModLog.Verbose($"KyBuildCategory.AddUsageTags: {added} custom tag(s) in the by-usage list");
        return added;
    }

    internal static string TagName(ByUsagePieceList list, int index)
    {
        string name = null;
        List<int> available = AvailableTags(list);
        if (CustomTags.TryGetValue(list, out Dictionary<int, Piece.PieceCategory> tags) && index >= 0 && index < available.Count && tags.TryGetValue(available[index], out Piece.PieceCategory cat))
            name = "$" + Token(Own.First(kv => kv.Value == cat).Key);
        if (CoreLog.Due("KyBuildCategory.TagName", 5d)) ModLog.Every("KyBuildCategory.TagName", 5d, $"KyBuildCategory.TagName: index {index} -> {name ?? "<vanilla>"}"); 
        return name;
    }

    internal static int PiecesWithTag(ByUsagePieceList list, int tagId, PieceTable table, IList<Piece> result)
    {
        int added = 0;
        if (table == null || !CustomTags.TryGetValue(list, out Dictionary<int, Piece.PieceCategory> tags) || !tags.TryGetValue(tagId, out Piece.PieceCategory cat)) { if (CoreLog.Due("KyBuildCategory.PiecesWithTag:0", 5d)) ModLog.Every("KyBuildCategory.PiecesWithTag:0", 5d, $"KyBuildCategory.PiecesWithTag: tag {tagId} is not a custom one - nothing added"); return added; }
        foreach (Piece piece in table.m_availablePieces)
            if (piece != null && piece.m_category == cat && !result.Contains(piece)) { result.Add(piece); added++; }
        if (CoreLog.On) ModLog.Verbose($"KyBuildCategory.PiecesWithTag: tag {tagId} = category {(int)cat} - {added} piece(s)");
        return added;
    }

    /// <summary>GetTagById with a custom tag id (beyond the game's usage tags) must not index past the array: returns -1 flags instead.</summary>
    internal static bool IsCustomTagId(ByUsagePieceList list, int id)
    {
        bool custom = id < 0 || id >= UsageTags(list).Length;
        if (CoreLog.Due("KyBuildCategory.IsCustomTagId", 5d)) ModLog.Every("KyBuildCategory.IsCustomTagId", 5d, $"KyBuildCategory.IsCustomTagId: id {id} -> custom={custom}"); 
        return custom;
    }

    internal static Piece.UsageTagFlags SafeGetTagById(ByUsagePieceList list, int id)
    {
        Piece.UsageTagFlags[] usage = UsageTags(list);
        var flags = id < 0 || id >= usage.Length ? (Piece.UsageTagFlags)(-1) : usage[id];
        if (CoreLog.Due("KyBuildCategory.SafeGetTagById", 5d)) ModLog.Every("KyBuildCategory.SafeGetTagById", 5d, $"KyBuildCategory.SafeGetTagById: id {id} of {usage.Length} -> {(int)flags}"); 
        return flags;
    }

    internal static bool RefreshIfNeeded()
    {
        bool was = _refreshNeeded;
        if (was) { _refreshNeeded = false; Refresh(); }
        if (CoreLog.Due("KyBuildCategory.RefreshIfNeeded", 5d)) ModLog.Every("KyBuildCategory.RefreshIfNeeded", 5d, $"KyBuildCategory.RefreshIfNeeded: refreshed={was}"); 
        return was;
    }

    internal static void MarkRefresh() { _refreshNeeded = true; ModLog.Verbose($"KyBuildCategory.MarkRefresh: refresh needed={_refreshNeeded}"); }

    /// <summary>Hud tabs for every custom category, and the local player's Hammer table showing the categories it has pieces in (Jotunn RefreshCategories, tab layout included).</summary>
    internal static int Refresh()
    {
        int tabs = CreateTabs();
        Player player = Player.m_localPlayer;
        PieceTable table = player == null ? null : BuildPieces(player);
        if (table == null || Hud.instance == null) { if (CoreLog.On) ModLog.Verbose($"KyBuildCategory.Refresh: tabs={tabs}, no build table yet"); return tabs; }
        var visible = new HashSet<Piece.PieceCategory>();
        foreach (GameObject go in table.m_pieces) { Piece p = go == null ? null : go.GetComponent<Piece>(); if (p != null) visible.Add(p.m_category); }
        UpdateTableCategories(table, visible);
        LayoutTabs(table);
        if (CoreLog.On) ModLog.Verbose($"KyBuildCategory.Refresh: tabs={tabs}, table categories={table.m_categories.Count}, visible={visible.Count}");
        return tabs;
    }

    static int CreateTabs()
    {
        Hud hud = Hud.instance;
        if (hud == null || hud.m_pieceCategoryTabs == null || hud.m_pieceCategoryTabs.Length == 0) { if (CoreLog.Due("KyBuildCategory.CreateTabs:0", 5d)) ModLog.Every("KyBuildCategory.CreateTabs:0", 5d, $"KyBuildCategory.CreateTabs: no Hud tabs yet - hud={hud != null}"); return 0; }
        int made = 0;
        for (int i = hud.m_pieceCategoryTabs.Length; i < MaxCategory(); i++)
        {
            GameObject tab = CreateTab(hud);
            hud.m_pieceCategoryTabs = hud.m_pieceCategoryTabs.AddItem(tab).ToArray();
            made++;
        }
        if (made > 0 && Player.m_localPlayer != null && BuildPieces(Player.m_localPlayer) != null) UpdateAvailablePieces?.Invoke(Player.m_localPlayer, null);
        if (made > 0) ModLog.Verbose($"KyBuildCategory: {made} Hud category tab(s) made (now {hud.m_pieceCategoryTabs.Length})");
        return made;
    }

    static GameObject CreateTab(Hud hud)
    {
        GameObject first = hud.m_pieceCategoryTabs[0];
        GameObject tab = UnityEngine.Object.Instantiate(first, first.transform.parent);
        tab.SetActive(false);
        UIInputHandler input = tab.GetComponent<UIInputHandler>() ?? tab.AddComponent<UIInputHandler>();
        var click = AccessTools.Method(typeof(Hud), "OnLeftClickCategory", new[] { typeof(UIInputHandler) });
        if (click != null) input.m_onLeftDown = (Action<UIInputHandler>)Delegate.Combine(input.m_onLeftDown, (Action<UIInputHandler>)Delegate.CreateDelegate(typeof(Action<UIInputHandler>), hud, click));
        foreach (TMP_Text text in tab.GetComponentsInChildren<TMP_Text>(true))
        {
            text.rectTransform.offsetMin = new Vector2(3f, 1f);
            text.rectTransform.offsetMax = new Vector2(-3f, -1f);
            text.enableAutoSizing = true;
            text.fontSizeMin = 10f;
            text.fontSizeMax = 20f;
            text.lineSpacing = 0.8f;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
        }
        if (CoreLog.On) ModLog.Verbose($"KyBuildCategory.CreateTab: tab cloned from '{first.name}', click handler={click != null}");
        return tab;
    }

    static int UpdateTableCategories(PieceTable table, HashSet<Piece.PieceCategory> visible)
    {
        int changed = 0;
        for (int i = 0; i < (int)VanillaMax; i++) changed += Toggle(table, (Piece.PieceCategory)i, visible, VanillaLabel((Piece.PieceCategory)i));
        foreach (var kv in Own) changed += Toggle(table, kv.Value, visible, "$" + Token(kv.Key));
        if (changed > 0 && CoreLog.On) ModLog.Verbose($"KyBuildCategory.UpdateTableCategories: {changed} category entr{(changed == 1 ? "y" : "ies")} changed");
        return changed;
    }

    static int Toggle(PieceTable table, Piece.PieceCategory cat, HashSet<Piece.PieceCategory> visible, string label)
    {
        bool want = visible.Contains(cat), has = table.m_categories.Contains(cat);
        int changed = 0;
        if (want && !has) { table.m_categories.Add(cat); table.m_categoryLabels.Add(label); changed = 1; }
        else if (!want && has) { int i = table.m_categories.IndexOf(cat); table.m_categories.RemoveAt(i); table.m_categoryLabels.RemoveAt(i); changed = 1; }
        if (CoreLog.On) ModLog.Verbose($"KyBuildCategory.Toggle: category {(int)cat} '{label}' wanted={want}, was in table={has} -> changed={changed}");
        return changed;
    }

    static string VanillaLabel(Piece.PieceCategory cat)
    {
        if (!VanillaLabels.ContainsKey(cat))
            foreach (PieceTable t in Resources.FindObjectsOfTypeAll<PieceTable>())
                for (int j = 0; j < t.m_categories.Count && j < t.m_categoryLabels.Count; j++)
                    if (!VanillaLabels.ContainsKey(t.m_categories[j]) && !string.IsNullOrEmpty(t.m_categoryLabels[j])) VanillaLabels[t.m_categories[j]] = t.m_categoryLabels[j];
        string found = VanillaLabels.TryGetValue(cat, out string label) ? label : string.Empty;
        if (CoreLog.On) ModLog.Verbose($"KyBuildCategory.VanillaLabel: {(int)cat} -> '{found}'");
        return found;
    }

    static int LayoutTabs(PieceTable table)
    {
        Hud hud = Hud.instance;
        var first = (RectTransform)hud.m_pieceCategoryTabs[0].transform;
        var root = (RectTransform)hud.m_pieceCategoryRoot.transform;
        var window = (RectTransform)hud.m_pieceSelectionWindow.transform;
        if (first.parent.TryGetComponent(out HorizontalLayoutGroup row)) row.enabled = false;
        Vector2 size = first.rect.size;
        int perRow = Mathf.Max((int)(root.rect.width / size.x), 1);
        int count = table.m_categories.Count;
        if (first.parent.TryGetComponent(out GridLayoutGroup grid)) grid.constraintCount = perRow;
        var origin = new Vector2(-size.x * perRow / 2f + size.x / 2f, (size.y + 1f) * Mathf.Floor((count - 1) / (float)perRow) + 5f);
        int placed = 0;
        for (int i = 0; i < count && i < hud.m_pieceCategoryTabs.Length; i++)
        {
            var rt = hud.m_pieceCategoryTabs[i].GetComponent<RectTransform>();
            rt.anchoredPosition = origin + new Vector2(size.x * (placed % perRow), -(size.y + 1f) * (Mathf.Floor(placed / (float)perRow) + 0.5f));
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            placed++;
        }
        var background = (RectTransform)window.Find("Bkg2");
        if (background != null) background.offsetMax = new Vector2(background.offsetMax.x, (size.y + 1f) * Mathf.Max(0, Mathf.FloorToInt((placed - 1) / (float)perRow)));
        else ModLog.Verbose($"KyBuildCategory.LayoutTabs: background 'Bkg2' not found under '{window.name}' - window not resized");
        Localize localize = hud.GetComponentInParent<Localize>();
        if (localize != null) localize.RefreshLocalization();
        if (CoreLog.On) ModLog.Verbose($"KyBuildCategory.LayoutTabs: {placed} tab(s), {perRow} per row, tab size {size}");
        return placed;
    }
}
