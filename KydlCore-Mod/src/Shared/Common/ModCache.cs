using System.IO;
using BepInEx;
using KydlCore.Core;

namespace KydlCore.Common;

/// <summary>
/// Files a mod writes for itself that are not config (dumps, generated data, caches) go to BepInEx/cache/&lt;mod name&gt;/ (user, 2026-10-27;
/// PROJECT_RULES 18). Never into BepInEx/config (that folder is for .cfg files the player edits) or next to the DLL.
/// </summary>
public static class ModCache
{
    /// Expected outcome: the full path of BepInEx/cache/&lt;modName&gt;/; the folder exists after the call.
    public static string Dir(string modName)
    {
        if (CoreLog.On) ModLog.Every("ModCache.Dir", 2d, $"ModCache.Dir(modName={CoreLog.Shown(modName)})");
        string dir = Path.Combine(Paths.CachePath, modName);
        Directory.CreateDirectory(dir);
        { var returnedAt19 = dir; if (CoreLog.On) ModLog.Every("ModCache.Dir:19", 2d, $"ModCache.Dir: returns {CoreLog.Shown(returnedAt19)} (line 19)"); return returnedAt19; }
    }

    /// Expected outcome: the full path of BepInEx/cache/&lt;modName&gt;/&lt;fileName&gt; (folder created, file not touched).
    public static string File(string modName, string fileName) { return Path.Combine(Dir(modName), fileName); }
}
