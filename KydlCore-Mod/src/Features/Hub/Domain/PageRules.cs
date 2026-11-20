using KydlCore.Core;
namespace KydlCore.Features.Hub.Domain;

/// <summary>T-098: the game-free decision of the page host (unit-tested).</summary>
internal static class PageRules
{
    /// <summary>Whether the content on screen must be replaced: nothing shown yet, an empty State (always rebuild), or a different State.</summary>
    internal static bool NeedsRebuild(string shown, string next)
    {
        if (CoreLog.On) CoreLog.Verbose($"PageRules.NeedsRebuild(shown={shown}, next={next})");
        if (shown == null) { if (CoreLog.On) CoreLog.Verbose("PageRules.NeedsRebuild: returns true (line 11)"); return true; }
        if (string.IsNullOrEmpty(next)) { if (CoreLog.On) CoreLog.Verbose("PageRules.NeedsRebuild: returns true (line 12)"); return true; }
        { var returnedAt13 = !string.Equals(shown, next, System.StringComparison.Ordinal); if (CoreLog.On) CoreLog.Verbose($"PageRules.NeedsRebuild: returns {returnedAt13} (line 13)"); return returnedAt13; }
    }
}
