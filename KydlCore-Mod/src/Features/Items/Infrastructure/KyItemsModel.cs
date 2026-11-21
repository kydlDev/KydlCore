using System;
using System.Collections.Generic;
using System.Linq;
using KydlCore.Features.Items.Contracts;
using KydlCore.Core;
using KydlCore.Common;

namespace KydlCore.Features.Items.Infrastructure;

/// <summary>
/// The pure part of the KY-Items registry (PLAN T-040, DESIGN 6): the list of entries with one entry per prefab name, the lookups and the rule
/// "which mod GUID is the user's". No Unity calls and no logging (like WindowStack / HubModel): <see cref="KyItems"/> finds the prefabs in the game
/// and puts them here.
/// </summary>
internal sealed class KyItemsModel
{
    internal const string GuidPrefix = "com.kydldev.";

    readonly List<KyEntry> _all = new List<KyEntry>();
    readonly Dictionary<string, KyEntry> _byPrefab = new Dictionary<string, KyEntry>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Every entry in the order they were added.</summary>
    internal IReadOnlyList<KyEntry> All { get { return _all; } }

    /// <summary>
    /// Adds an entry. Expected outcome: true when it is new; false (nothing changes) for null, an empty prefab name, or a prefab name that is
    /// already there (the first entry wins: the same prefab found by two ways is listed once).
    /// </summary>
    internal bool Add(KyEntry entry)
    {
        if (CoreLog.Due("KyItemsModel.Add", 2d)) ModLog.Every("KyItemsModel.Add", 2d, $"KyItemsModel.Add(entry={CoreLog.Shown(entry)})");
        if (entry == null || string.IsNullOrEmpty(entry.Prefab) || _byPrefab.ContainsKey(entry.Prefab)) { if (CoreLog.Due("KyItemsModel.Add:32", 2d)) ModLog.Every("KyItemsModel.Add:32", 2d, "KyItemsModel.Add: returns false (line 32)"); return false; }
        _byPrefab[entry.Prefab] = entry;
        _all.Add(entry);
        { if (CoreLog.Due("KyItemsModel.Add:35", 2d)) ModLog.Every("KyItemsModel.Add:35", 2d, "KyItemsModel.Add: returns true (line 35)"); return true; }
    }

    /// <summary>The entry of a prefab name (case ignored), or null.</summary>
    internal KyEntry Find(string prefab)
    {
        if (CoreLog.Due("KyItemsModel.Find", 2d)) ModLog.Every("KyItemsModel.Find", 2d, $"KyItemsModel.Find(prefab={CoreLog.Shown(prefab)})");
        if (prefab == null) { if (CoreLog.Due("KyItemsModel.Find:42", 2d)) ModLog.Every("KyItemsModel.Find:42", 2d, "KyItemsModel.Find: returns null (line 42)"); return null; }
        KyEntry e;
        _byPrefab.TryGetValue(prefab, out e);                 // e stays null when the name is unknown
        { var returnedAt45 = e; if (CoreLog.Due("KyItemsModel.Find:45", 2d)) ModLog.Every("KyItemsModel.Find:45", 2d, $"KyItemsModel.Find: returns {CoreLog.Shown(returnedAt45)} (line 45)"); return returnedAt45; }
    }

    /// <summary>The entries of one mod GUID (case ignored), in order; none for null / empty.</summary>
    internal IEnumerable<KyEntry> ByMod(string modGuid)
    {
        if (CoreLog.On) ModLog.Verbose($"KyItemsModel.ByMod(modGuid={CoreLog.Shown(modGuid)})");
        if (string.IsNullOrEmpty(modGuid)) { var returnedAt52 = Enumerable.Empty<KyEntry>(); if (CoreLog.On) ModLog.Verbose($"KyItemsModel.ByMod: returns {CoreLog.Shown(returnedAt52)} (line 52)"); return returnedAt52; }
        return _all.Where(e => string.Equals(e.ModGuid, modGuid, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>
    /// Makes this list the one in <paramref name="next"/>. Expected outcome: true when the set of prefab names changed (a new one or a gone one), so
    /// the registry raises Changed; false when only the same prefabs were found again (their entries are still replaced by the fresh ones).
    /// </summary>
    internal bool ReplaceWith(KyItemsModel next)
    {
        if (CoreLog.Due("KyItemsModel.ReplaceWith", 2d)) ModLog.Every("KyItemsModel.ReplaceWith", 2d, $"KyItemsModel.ReplaceWith(next={CoreLog.Shown(next)})");
        bool changed = !new HashSet<string>(_byPrefab.Keys, StringComparer.OrdinalIgnoreCase).SetEquals(next._byPrefab.Keys);
        _all.Clear(); _byPrefab.Clear();
        foreach (KyEntry e in next._all.ToList()) Add(e);
        { var returnedAt66 = changed; if (CoreLog.Due("KyItemsModel.ReplaceWith:66", 2d)) ModLog.Every("KyItemsModel.ReplaceWith:66", 2d, $"KyItemsModel.ReplaceWith: returns {CoreLog.Shown(returnedAt66)} (line 66)"); return returnedAt66; }
    }

    /// <summary>The user's mods: "com.kydldev.*" (case ignored) with a name after the prefix. Consumers leave out their own GUID themselves.</summary>
    internal static bool IsUserGuid(string guid) { return KydlCore.Features.Items.Domain.KyItemRules.IsUserGuid(guid); }
}
