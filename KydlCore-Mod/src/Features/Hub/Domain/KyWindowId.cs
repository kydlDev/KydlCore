using KydlCore.Core;
namespace KydlCore.Features.Hub.Domain;

/// <summary>
/// The id of a window in the one <see cref="WindowStack"/> (DESIGN 4, PLAN T-038): a mod's own short id plus its plugin GUID, so two mods can
/// both have a window called "editor". The hub is the reserved id "hub" (it has no GUID, so no mod can take it). Pure text logic, no logging.
/// </summary>
internal static class KyWindowId
{
    internal const string Hub = "hub";

    /// "guid:id", or null when the GUID or the id is null / empty / blank (the caller refuses the window and says why).
    internal static string Make(string modGuid, string id)
    {
        if (CoreLog.On) CoreLog.Every("KyWindowId.Make", 2d, $"KyWindowId.Make(modGuid={modGuid}, id={id})");
        if (string.IsNullOrWhiteSpace(modGuid) || string.IsNullOrWhiteSpace(id)) { if (CoreLog.On) CoreLog.Every("KyWindowId.Make:16", 2d, "KyWindowId.Make: returns null (line 16)"); return null; }
        { var returnedAt17 = modGuid.Trim() + ":" + id.Trim(); if (CoreLog.On) CoreLog.Every("KyWindowId.Make:17", 2d, $"KyWindowId.Make: returns {returnedAt17} (line 17)"); return returnedAt17; }
    }
}
