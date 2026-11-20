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
    /// AppDomain key of the copy (ZDOID of the terrain compiler -> its TCData bytes), shared by all mods of the session.
    public const string SnapshotKey = "Kydl.TestCleanup.Terrain";
    static readonly AccessTools.FieldRef<TerrainComp, ZNetView> NView = AccessTools.FieldRefAccess<TerrainComp, ZNetView>("m_nview");
    static readonly System.Reflection.MethodInfo SaveMethod = AccessTools.Method(typeof(TerrainComp), "Save");
    static readonly System.Reflection.MethodInfo CheckLoadMethod = AccessTools.Method(typeof(TerrainComp), "CheckLoad");

    /// Copies the terrain data of every terrain compiler near `spot` that has no copy yet, then restores the ones that changed since. Returns how many were restored.
    public static int Run(ModContext ctx, Vector3 spot)
    {
        if (CoreLog.On) ModLog.Verbose(ctx, $"TestTerrainRestore.Run(spot={CoreLog.Shown(spot)})");
        var domain = System.AppDomain.CurrentDomain;
        if (!(domain.GetData(SnapshotKey) is Dictionary<ZDOID, byte[]> copy)) { copy = new Dictionary<ZDOID, byte[]>(); domain.SetData(SnapshotKey, copy); }
        int restored = 0;
        foreach (var comp in Object.FindObjectsByType<TerrainComp>(FindObjectsSortMode.None))
        {
            var nview = NView(comp);
            if (nview == null || !nview.IsValid()) continue;
            var d = comp.transform.position - spot; d.y = 0f;
            if (d.magnitude > TestCleanupRules.RangeMetres + TestCleanupRules.ZoneMetres) continue;
            if (!nview.IsOwner()) nview.ClaimOwnership();
            var id = nview.GetZDO().m_uid;
            if (!copy.TryGetValue(id, out var first))
            {
                SaveMethod?.Invoke(comp, new object[] { false });      // an untouched compiler has no data yet: Save writes the pristine state
                copy[id] = nview.GetZDO().GetByteArray(ZDOVars.s_TCData);
                ModLog.Verbose(ctx, "TestTerrainRestore: copy of the terrain data at " + comp.transform.position.ToString("F0") + ", " + (copy[id] == null ? 0 : copy[id].Length) + " bytes");
                continue;
            }
            var nowBytes = nview.GetZDO().GetByteArray(ZDOVars.s_TCData);
            ModLog.Verbose(ctx, "TestTerrainRestore: " + comp.transform.position.ToString("F0") + " copy " + (first == null ? -1 : first.Length) + " bytes, now " + (nowBytes == null ? -1 : nowBytes.Length) + " bytes, revision " + nview.GetZDO().DataRevision);
            if (first == null || !TestCleanupRules.TerrainChanged(first, nowBytes)) continue;
            nview.GetZDO().Set(ZDOVars.s_TCData, first);
            CheckLoadMethod?.Invoke(comp, null);
            restored++;
        }
        if (restored > 0) ModLog.Info(ctx, "TestTerrainRestore: " + restored + " terrain area(s) put back to the template");
        { var returnedAt51 = restored; if (CoreLog.On) ModLog.Verbose(ctx, $"TestTerrainRestore.Run: returns {CoreLog.Shown(returnedAt51)} (line 51)"); return returnedAt51; }
    }
}
