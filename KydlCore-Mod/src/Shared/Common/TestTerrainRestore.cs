using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using KydlCore.Core;

namespace KydlCore.Common;

/// <summary>
/// Puts the arena's ground back to the template (user 2026-11-20, D-142, "Template as loaded"): the first time a terrain compiler of the lab world is seen its saved terrain
/// data (ZDO TCData: every raise / lower / level / paint operation so far) is copied; each later test start writes that copy back, so craters and levelling of earlier
/// tests are gone and the template's own levelling stays. Called by <see cref="TestWorldCleanup.Run"/>. Game code (compiled into every mod; only test hooks call it).
/// </summary>
public static class TestTerrainRestore
{
    /// AppDomain key of the copy (zone of the terrain compiler, <see cref="TestCleanupRules.TerrainKey"/> -> its TCData bytes), shared by all mods of the session.
    /// Keyed by zone, not ZDOID: a relog gives every ZDO a new id, and the ZDOID-keyed copy wrote one zone's levelling into another (GunPowdered
    /// 20261124-001640: hills, holes, floating slabs, torn terrain edges). Zones do not move, so the copy stays right across a relog.
    public const string ZoneSnapshotKey = "Kydl.TestCleanup.TerrainByZone";
    static readonly AccessTools.FieldRef<TerrainComp, ZNetView> NView = AccessTools.FieldRefAccess<TerrainComp, ZNetView>("m_nview");
    static readonly System.Reflection.MethodInfo SaveMethod = AccessTools.Method(typeof(TerrainComp), "Save");
    static readonly System.Reflection.MethodInfo CheckLoadMethod = AccessTools.Method(typeof(TerrainComp), "CheckLoad");

    /// Copies the terrain data of every terrain compiler near `spot` that has no copy yet, then restores the ones that changed since. Returns how many were restored.
    public static int Run(ModContext ctx, Vector3 spot)
    {
        if (CoreLog.On) ModLog.Verbose(ctx, $"TestTerrainRestore.Run(spot={CoreLog.Shown(spot)})");
        var domain = System.AppDomain.CurrentDomain;
        if (!(domain.GetData(ZoneSnapshotKey) is Dictionary<(int X, int Z), byte[]> copy)) { copy = new Dictionary<(int X, int Z), byte[]>(); domain.SetData(ZoneSnapshotKey, copy); }
        int restored = 0;
        foreach (var comp in Object.FindObjectsByType<TerrainComp>(FindObjectsSortMode.None))
        {
            var nview = NView(comp);
            if (nview == null || !nview.IsValid()) continue;
            var d = comp.transform.position - spot; d.y = 0f;
            if (d.magnitude > TestCleanupRules.RangeMetres + TestCleanupRules.ZoneMetres) continue;
            if (!nview.IsOwner()) nview.ClaimOwnership();
            var at = comp.transform.position;
            var zone = TestCleanupRules.TerrainKey(at.x, at.z);      // the zone, not the ZDOID: a relog gives the compiler a new id, its zone stays
            if (!copy.TryGetValue(zone, out var first))
            {
                SaveMethod?.Invoke(comp, new object[] { false });      // an untouched compiler has no data yet: Save writes the pristine state
                copy[zone] = nview.GetZDO().GetByteArray(ZDOVars.s_TCData);
                ModLog.Verbose(ctx, $"TestTerrainRestore: copy taken - zone={zone.X},{zone.Z}, at={at.ToString("F0")}, bytes={(copy[zone] == null ? 0 : copy[zone].Length)}, zdo={nview.GetZDO().m_uid}");
                continue;
            }
            var nowBytes = nview.GetZDO().GetByteArray(ZDOVars.s_TCData);
            ModLog.Verbose(ctx, $"TestTerrainRestore: compare - zone={zone.X},{zone.Z}, at={at.ToString("F0")}, copy={(first == null ? -1 : first.Length)} bytes, now={(nowBytes == null ? -1 : nowBytes.Length)} bytes, revision={nview.GetZDO().DataRevision}, zdo={nview.GetZDO().m_uid}");
            if (first == null || !TestCleanupRules.TerrainChanged(first, nowBytes)) continue;
            nview.GetZDO().Set(ZDOVars.s_TCData, first);
            CheckLoadMethod?.Invoke(comp, null);
            ModLog.Info(ctx, $"TestTerrainRestore: put back - zone={zone.X},{zone.Z}, at={at.ToString("F0")}, bytes={first.Length}");
            restored++;
        }
        if (restored > 0)
        {
            // CheckLoad only queues the heightmap rebuild; until it ran, the next test step read the ground from the old mesh (GunPowdered a18persist1: the ground
            // read 10 m too low, a rock went in underground). Rebuild now and sync the colliders, so the ground is right when Run returns.
            Heightmap.ForceGenerateAll();
            Physics.SyncTransforms();
            ModLog.Info(ctx, $"TestTerrainRestore: {restored} terrain area(s) put back to the template - heightmaps rebuilt, colliders synced");
        }
        { var returnedAt62 = restored; if (CoreLog.On) ModLog.Verbose(ctx, $"TestTerrainRestore.Run: returns {CoreLog.Shown(returnedAt62)} (line 62)"); return returnedAt62; }
    }
}
