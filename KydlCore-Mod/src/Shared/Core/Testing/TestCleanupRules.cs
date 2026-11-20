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

    /// Did the ground change since the copy was taken? Different bytes (or a missing current value) mean yes.
    public static bool TerrainChanged(byte[] copy, byte[] now)
    {
        if (CoreLog.On) CoreLog.Verbose($"TestCleanupRules.TerrainChanged(copy={CoreLog.Shown(copy)}, now={CoreLog.Shown(now)})");
        bool changed = copy == null ? now != null : now == null || copy.Length != now.Length;
        for (int i = 0; !changed && copy != null && now != null && i < copy.Length; i++) changed = copy[i] != now[i];
        { var returnedAt31 = changed; if (CoreLog.On) CoreLog.Verbose($"TestCleanupRules.TerrainChanged: returns {CoreLog.Shown(returnedAt31)} (line 31)"); return returnedAt31; }
    }
}