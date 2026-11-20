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