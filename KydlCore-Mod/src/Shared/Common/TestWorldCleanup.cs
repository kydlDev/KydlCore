using System.Collections.Generic;
using UnityEngine;
using KydlCore.Core;

namespace KydlCore.Common;

/// <summary>
/// The start-of-test cleanup of the TestLab world (user 2026-11-20, D-142; replaces the "killall + removedrops at the end of every test" rule of 2026-11-18, which left
/// tree trunks, stumps, barrels and loose items on the arena). <see cref="TestArena.MovePlayer"/> - the first step of every test of every mod - calls <see cref="Run"/>:
/// dropped items and creatures within <see cref="TestCleanupRules.RangeMetres"/> of the test spot go, and so does every log, stump and built piece that was not in the
/// world when the FIRST test of the session started (that snapshot is the template; it is shared by all mods through the AppDomain). The decision is TestCleanupRules
/// (unit-tested); this class only reads the game and removes. Game code (compiled into every mod; only test hooks call it).
/// </summary>
public static class TestWorldCleanup
{
    /// AppDomain key of the template snapshot (the ZDOIDs of every object that existed when the first test started).
    public const string TemplateKey = "Kydl.TestCleanup.Template";

    /// Removes what the earlier tests left near `spot`. Expected outcome: "cleanup: removed N (...)" - 0 on a clean arena; the first call only takes the snapshot of the template.
    public static string Run(ModContext ctx, Vector3 spot)
    {
        if (CoreLog.On) ModLog.Verbose(ctx, $"TestWorldCleanup.Run(spot={CoreLog.Shown(spot)})");
        var views = Object.FindObjectsByType<ZNetView>(FindObjectsSortMode.None);
        var domain = System.AppDomain.CurrentDomain;
        if (!(domain.GetData(TemplateKey) is HashSet<ZDOID> template))
        {
            template = new HashSet<ZDOID>();
            foreach (var v in views) if (v != null && v.IsValid()) template.Add(v.GetZDO().m_uid);
            domain.SetData(TemplateKey, template);
            ModLog.Info(ctx, "TestWorldCleanup: template snapshot of " + template.Count + " object(s) taken at the first test");
        }
        var removed = new int[6];
        foreach (var v in views)
        {
            if (v == null || !v.IsValid()) continue;
            var kind = KindOf(v.gameObject);
            if (kind == CleanupKind.Other) continue;
            var d = v.transform.position - spot; d.y = 0f;
            if (!TestCleanupRules.ShouldRemove(kind, template.Contains(v.GetZDO().m_uid), d.magnitude)) continue;
            if (!v.IsOwner()) v.ClaimOwnership();
            v.Destroy();
            removed[(int)kind]++;
        }
        int total = removed[1] + removed[2] + removed[3] + removed[4] + removed[5];
        string text = "cleanup: removed " + total + " (drops " + removed[(int)CleanupKind.Drop] + ", creatures " + removed[(int)CleanupKind.Creature] + ", logs " + removed[(int)CleanupKind.Log]
            + ", stumps " + removed[(int)CleanupKind.Stump] + ", pieces " + removed[(int)CleanupKind.Piece] + ")";
        text += ", terrain " + TestTerrainRestore.Run(ctx, spot) + " restored";
        if (total > 0) ModLog.Info(ctx, "TestWorldCleanup: " + text);
        { var returnedAt49 = text; if (CoreLog.On) ModLog.Verbose(ctx, $"TestWorldCleanup.Run: returns {CoreLog.Shown(returnedAt49)} (line 49)"); return returnedAt49; }
    }

    /// What a world object is (the local player and everything the cleanup never touches are Other).
    static CleanupKind KindOf(GameObject go)
    {
        if (CoreLog.On) CoreLog.Verbose($"TestWorldCleanup.KindOf(go={CoreLog.Shown(go)})");
        CleanupKind kind = CleanupKind.Other;
        if (go.GetComponent<Player>() != null) kind = CleanupKind.Other;
        else if (go.GetComponent<ItemDrop>() != null) kind = CleanupKind.Drop;
        else if (go.GetComponent<Character>() != null) kind = CleanupKind.Creature;
        else if (go.GetComponent<TreeLog>() != null) kind = CleanupKind.Log;
        else if (go.GetComponent<Piece>() != null || go.GetComponent<TerrainModifier>() != null) kind = CleanupKind.Piece;      // a TerrainModifier keeps levelling the ground for as long as it exists
        else if (go.GetComponent<Destructible>() != null && go.name.IndexOf("_stub", System.StringComparison.OrdinalIgnoreCase) >= 0) kind = CleanupKind.Stump;
        { var returnedAt62 = kind; if (CoreLog.On) CoreLog.Verbose($"TestWorldCleanup.KindOf: returns {CoreLog.Shown(returnedAt62)} (line 62)"); return returnedAt62; }
    }
}