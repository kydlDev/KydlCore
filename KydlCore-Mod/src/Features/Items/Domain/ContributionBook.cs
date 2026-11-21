using System;
using System.Collections.Generic;
using KydlCore.Core;

namespace KydlCore.Features.Items.Domain;

/// <summary>One accepted contribution: id, owner, prefab name.</summary>
internal readonly struct ContributionRecord
{
    internal string Id { get; }
    internal string ModGuid { get; }
    internal string Prefab { get; }
    internal ContributionRecord(string id, string modGuid, string prefab) { Id = id ?? ""; ModGuid = modGuid ?? ""; Prefab = prefab ?? ""; }
}

/// <summary>
/// Which contribution ids and prefabs are taken (spec 5: duplicate identifiers are rejected, also across mods). One id per prefab and one prefab per id.
/// Pure: tested in tests/Unit.
/// </summary>
internal sealed class ContributionBook
{
    internal const string NoPrefab = "no prefab";
    readonly Dictionary<string, ContributionRecord> _byId = new Dictionary<string, ContributionRecord>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, string> _idByPrefab = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    internal int Count { get { return _byId.Count; } }
    internal bool Contains(string id) { return id != null && _byId.ContainsKey(id); }

    /// Expected outcome: Success with the new count; Rejected when the id or the prefab is already taken (the reason names the holder).
    internal OperationResponse<int> Add(ContributionRecord record)
    {
        if (CoreLog.Due("ContributionBook.Add", 2d)) CoreLog.Every("ContributionBook.Add", 2d, $"ContributionBook.Add(record={CoreLog.Shown(record)})");
        if (string.IsNullOrEmpty(record.Prefab)) { var returnedAt33 = OperationResponse<int>.Rejected(NoPrefab); if (CoreLog.Due("ContributionBook.Add:33", 2d)) CoreLog.Every("ContributionBook.Add:33", 2d, $"ContributionBook.Add: returns {CoreLog.Shown(returnedAt33)} (line 33)"); return returnedAt33; }
        ContributionRecord holder;
        if (_byId.TryGetValue(record.Id, out holder)) { var returnedAt35 = OperationResponse<int>.Rejected("the id '" + record.Id + "' is already registered by " + holder.ModGuid); if (CoreLog.Due("ContributionBook.Add:35", 2d)) CoreLog.Every("ContributionBook.Add:35", 2d, $"ContributionBook.Add: returns {CoreLog.Shown(returnedAt35)} (line 35)"); return returnedAt35; }
        string otherId;
        if (_idByPrefab.TryGetValue(record.Prefab, out otherId)) { var returnedAt37 = OperationResponse<int>.Rejected("the prefab '" + record.Prefab + "' is already contributed as '" + otherId + "'"); if (CoreLog.Due("ContributionBook.Add:37", 2d)) CoreLog.Every("ContributionBook.Add:37", 2d, $"ContributionBook.Add: returns {CoreLog.Shown(returnedAt37)} (line 37)"); return returnedAt37; }
        _byId[record.Id] = record;
        _idByPrefab[record.Prefab] = record.Id;
        { var returnedAt40 = OperationResponse<int>.Success(_byId.Count); if (CoreLog.Due("ContributionBook.Add:40", 2d)) CoreLog.Every("ContributionBook.Add:40", 2d, $"ContributionBook.Add: returns {CoreLog.Shown(returnedAt40)} (line 40)"); return returnedAt40; }
    }

    /// Expected outcome: true when the id is not taken any more (also when it never was).
    internal bool Remove(string id)
    {
        if (CoreLog.Due("ContributionBook.Remove", 2d)) CoreLog.Every("ContributionBook.Remove", 2d, $"ContributionBook.Remove(id={CoreLog.Shown(id)})");
        ContributionRecord r = default(ContributionRecord);   // assigned up front: a chained out-variable breaks Stryker mutants (Safe Mode)
        if (id != null && _byId.TryGetValue(id, out r)) { _byId.Remove(id); _idByPrefab.Remove(r.Prefab); }
        { var returnedAt49 = !Contains(id); if (CoreLog.Due("ContributionBook.Remove:49", 2d)) CoreLog.Every("ContributionBook.Remove:49", 2d, $"ContributionBook.Remove: returns {CoreLog.Shown(returnedAt49)} (line 49)"); return returnedAt49; }
    }

    /// The record of an id (Prefab "" when unknown).
    internal ContributionRecord Find(string id)
    {
        if (CoreLog.Due("ContributionBook.Find", 2d)) CoreLog.Every("ContributionBook.Find", 2d, $"ContributionBook.Find(id={CoreLog.Shown(id)})");
        ContributionRecord r = default(ContributionRecord);   // assigned up front: a chained out-variable breaks Stryker mutants (Safe Mode)
        { var returnedAt57 = id != null && _byId.TryGetValue(id, out r) ? r : new ContributionRecord(id, "", ""); if (CoreLog.Due("ContributionBook.Find:57", 2d)) CoreLog.Every("ContributionBook.Find:57", 2d, $"ContributionBook.Find: returns {CoreLog.Shown(returnedAt57)} (line 57)"); return returnedAt57; }
    }
}
