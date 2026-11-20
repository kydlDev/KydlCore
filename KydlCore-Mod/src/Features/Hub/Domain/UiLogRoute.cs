using KydlCore.Core;
namespace KydlCore.Features.Hub.Domain;

/// <summary>
/// The rules for a line written by the shared UI code (KydlCore.Ui.UiLog) while KydlCore draws one mod's tab (PLAN T-050 follow-up, D-071): which
/// debug switch decides whether it shows, and how it is tagged so the BepInEx log can be filtered per mod. Pure text / bool logic: no logging,
/// no Unity, no game (the lookups of the owner's name and switch are in UiLogSinks).
/// </summary>
internal static class UiLogRoute
{
    /// A debug (verbose) line of a mod's UI follows THAT mod's [DebugLog] Enable; a line with no owner (the kit itself, KydlCore's own pages) follows KydlCore's.
    internal static bool ShowVerbose(string owner, bool ownerDebugOn, bool coreDebugOn)
    {
        if (CoreLog.On) CoreLog.Verbose($"UiLogRoute.ShowVerbose(owner={owner}, ownerDebugOn={ownerDebugOn}, coreDebugOn={coreDebugOn})");
        { var returnedAt15 = owner == null ? coreDebugOn : ownerDebugOn; if (CoreLog.On) CoreLog.Verbose($"UiLogRoute.ShowVerbose: returns {returnedAt15} (line 15)"); return returnedAt15; }
    }

    /// "[ModName] message" so a text filter on the mod name finds it; no name = the message as it is.
    internal static string Tag(string ownerName, string message)
    {
        if (CoreLog.On) CoreLog.Verbose($"UiLogRoute.Tag(ownerName={ownerName}, message={message})");
        { var returnedAt22 = string.IsNullOrEmpty(ownerName) ? message : "[" + ownerName + "] " + message; if (CoreLog.On) CoreLog.Verbose($"UiLogRoute.Tag: returns {returnedAt22} (line 22)"); return returnedAt22; }
    }
}
