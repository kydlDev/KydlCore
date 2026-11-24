using KydlCore.Core;

namespace KydlCore.Tests.Unit;

/// <summary>The decision of the TestLab start-of-test cleanup (D-142): what a test leaves behind is removed, the template's own objects stay.</summary>
[TestFixture]
public class TestCleanupRulesTests
{
    [TestCase(CleanupKind.Drop, true)]
    [TestCase(CleanupKind.Drop, false)]
    [TestCase(CleanupKind.Creature, true)]
    [TestCase(CleanupKind.Creature, false)]
    public void DropsAndCreatures_AreAlwaysRemoved(CleanupKind kind, bool inTemplate)
    {
        Assert.That(TestCleanupRules.ShouldRemove(kind, inTemplate, 10f), Is.True);
    }

    [TestCase(CleanupKind.Log)]
    [TestCase(CleanupKind.Stump)]
    [TestCase(CleanupKind.Piece)]
    public void LogsStumpsAndPieces_AreRemovedOnlyWhenNotInTheTemplate(CleanupKind kind)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(TestCleanupRules.ShouldRemove(kind, false, 10f), Is.True, "left by a test");
            Assert.That(TestCleanupRules.ShouldRemove(kind, true, 10f), Is.False, "part of the template");
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Other_IsNeverRemoved(bool inTemplate)
    {
        Assert.That(TestCleanupRules.ShouldRemove(CleanupKind.Other, inTemplate, 0f), Is.False);
    }

    [TestCase(CleanupKind.Drop)]
    [TestCase(CleanupKind.Creature)]
    [TestCase(CleanupKind.Log)]
    [TestCase(CleanupKind.Piece)]
    public void FarFromTheSpot_IsLeftAlone_ButTheRangeEdgeCounts(CleanupKind kind)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(TestCleanupRules.ShouldRemove(kind, false, TestCleanupRules.RangeMetres), Is.True, "exactly at the range");
            Assert.That(TestCleanupRules.ShouldRemove(kind, false, TestCleanupRules.RangeMetres + 0.1f), Is.False, "beyond it");
        }
    }

    // GunPowdered 20261124-001640: the copy was keyed by ZDOID, a relog gave new ids and another zone's levelling went into the arena (torn terrain).
    // The key is the zone (ZoneSystem.GetZone maths: 64 m zones centred on multiples of 64), which a relog cannot change.
    [TestCase(0f, 0f, 0, 0)]
    [TestCase(31.9f, -31.9f, 0, 0)]
    [TestCase(32f, 32f, 1, 1)]
    [TestCase(-32.1f, -32f, -1, 0)]
    [TestCase(128f, 0f, 2, 0)]
    [TestCase(192f, 0f, 3, 0)]
    [TestCase(115f, 28f, 2, 0)]
    public void TerrainKey_IsTheZoneOfThePoint(float x, float z, int zoneX, int zoneZ)
    {
        Assert.That(TestCleanupRules.TerrainKey(x, z), Is.EqualTo((zoneX, zoneZ)));
    }

    [Test]
    public void TerrainKey_NeighbourZonesDiffer_SameZoneMatches()
    {
        Assert.That(TestCleanupRules.TerrainKey(128f, 0f), Is.Not.EqualTo(TestCleanupRules.TerrainKey(192f, 0f)), "the two zones of the a18kneel3 mix-up");
        Assert.That(TestCleanupRules.TerrainKey(100f, 10f), Is.EqualTo(TestCleanupRules.TerrainKey(150f, -20f)), "two points of one zone");
    }

    [Test]
    public void TerrainChanged_SameBytes_IsFalse_AnyDifferenceIsTrue()
    {
        var copy = new byte[] { 1, 2, 3 };
        using (Assert.EnterMultipleScope())
        {
            Assert.That(TestCleanupRules.TerrainChanged(copy, new byte[] { 1, 2, 3 }), Is.False, "same");
            Assert.That(TestCleanupRules.TerrainChanged(copy, new byte[] { 1, 2, 4 }), Is.True, "last byte differs");
            Assert.That(TestCleanupRules.TerrainChanged(copy, new byte[] { 1, 2 }), Is.True, "shorter");
            Assert.That(TestCleanupRules.TerrainChanged(copy, null), Is.True, "data gone");
            Assert.That(TestCleanupRules.TerrainChanged(null, copy), Is.True, "data appeared");
            Assert.That(TestCleanupRules.TerrainChanged(null, null), Is.False, "never any data");
        }
    }
}