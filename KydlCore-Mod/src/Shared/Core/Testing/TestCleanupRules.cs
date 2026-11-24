namespace KydlCore.Core;

/// <summary>
/// The decision of the TestLab start-of-test cleanup (user 2026-11-20, D-142): which world objects a test leaves behind and must be removed before the next test.
/// Pure (no Unity / Valheim types) so it is unit-tested; TestWorldCleanup reads the game, asks here and removes.
/// </summary>
public static class TestCleanupRules
{
    /// Objects farther than this from the test spot are never touched (the lab house and the rest of the world are not the arena's business).
    public const float RangeMetres = 90f, ZoneMetres = 32f;

    /// Should this object be removed? Drops and creatures always (no template object of those kinds matters); logs, stumps and pieces only when they are not part of the
    /// template snapshot taken at the first call (`inTemplate`); everything else never. `distance` is flat metres from the test spot.
    public static bool ShouldRemove(CleanupKind kind, bool inTemplate, float distance)
    {
        if (CoreLog.On) CoreLog.Verbose($"TestCleanupRules.ShouldRemove(kind={CoreLog.Shown(kind)}, inTemplate={CoreLog.Shown(inTemplate)}, distance={CoreLog.Shown(distance)})");
        bool remove;
        if (distance > RangeMetres) remove = false;
        else if (kind == CleanupKind.Drop || kind == CleanupKind.Creature) remove = true;
        else if (kind == CleanupKind.Log || kind == CleanupKind.Stump || kind == CleanupKind.Piece) remove = !inTemplate;
        else remove = false;
        { var returnedAt22 = remove; if (CoreLog.On) CoreLog.Verbose($"TestCleanupRules.ShouldRemove: returns {CoreLog.Shown(returnedAt22)} (line 22)"); return returnedAt22; }
    }

    /// Side of one Valheim zone in metres (ZoneSystem.c_ZoneSize); a terrain compiler sits at its zone's centre.
    public const float ZoneSizeMetres = 64f;

    /// The key the template terrain copy is kept under: the zone that holds the point (same maths as ZoneSystem.GetZone). Not the ZDOID: a relog loads the
    /// world again with NEW ZDOIDs, and an old id then names another zone's terrain compiler (GunPowdered a18kneel3 / 20261124-001640: another zone's
    /// levelling was written into the arena - hills, holes, floating slabs and torn terrain edges). A zone never moves, so the key survives a relog.
    public static (int X, int Z) TerrainKey(float x, float z)
    {
        if (CoreLog.On) CoreLog.Verbose($"TestCleanupRules.TerrainKey(x={CoreLog.Shown(x)}, z={CoreLog.Shown(z)})");
        var key = ((int)System.Math.Floor((x + ZoneSizeMetres / 2f) / ZoneSizeMetres), (int)System.Math.Floor((z + ZoneSizeMetres / 2f) / ZoneSizeMetres));
        { var returnedAt35 = key; if (CoreLog.On) CoreLog.Verbose($"TestCleanupRules.TerrainKey: returns {CoreLog.Shown(returnedAt35)} (line 35)"); return returnedAt35; }
    }

    /// Did the ground change since the copy was taken? Different bytes (or a missing current value) mean yes.
    public static bool TerrainChanged(byte[] copy, byte[] now)
    {
        if (CoreLog.On) CoreLog.Verbose($"TestCleanupRules.TerrainChanged(copy={CoreLog.Shown(copy)}, now={CoreLog.Shown(now)})");
        bool changed = copy == null ? now != null : now == null || copy.Length != now.Length;
        for (int i = 0; !changed && copy != null && now != null && i < copy.Length; i++) changed = copy[i] != now[i];
        { var returnedAt44 = changed; if (CoreLog.On) CoreLog.Verbose($"TestCleanupRules.TerrainChanged: returns {CoreLog.Shown(returnedAt44)} (line 44)"); return returnedAt44; }
    }
}
