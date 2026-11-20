using System;
using System.IO;
using System.Linq;
using System.Text;
using KydlCore.Common;
using KydlCore.Features.Diagnostics.Domain;
using KydlCore.Core;

namespace KydlCore.Features.Diagnostics.Infrastructure;

/// <summary>
/// Where the flight recorder's dumps go: BepInEx\cache\KydlCore\flight\&lt;yyyyMMdd-HHmmss-fff&gt;_&lt;reason&gt;.txt (PROJECT_RULES 18: files a
/// mod writes for itself go to its cache folder). Kydl.GameProbe copies the dumps of a session into its session folder. Old dumps are
/// removed at game start (older than the kept days, or more than the kept count).
/// </summary>
internal static class FlightFiles
{
    const string CacheOwner = "KydlCore";
    const string Folder = "flight";

    /// <summary>The dump folder (created when missing).</summary>
    internal static string Dir()
    {
        if (CoreLog.On) ModLog.Every("FlightFiles.Dir", 2d, $"FlightFiles.Dir()");
        string dir = Path.Combine(ModCache.Dir(CacheOwner), Folder);
        Directory.CreateDirectory(dir);
        { var returnedAt27 = dir; if (CoreLog.On) ModLog.Every("FlightFiles.Dir:27", 2d, $"FlightFiles.Dir: returns {CoreLog.Shown(returnedAt27)} (line 27)"); return returnedAt27; }
    }

    /// <summary>Writes one dump.</summary>
    /// <returns>The file's path, or null when it could not be written (the reason is logged).</returns>
    internal static string Write(string reason, string text)
    {
        string path = Path.Combine(Dir(), DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + "_" + TriggerRules.Slug(reason) + ".txt");
        try
        {
            File.WriteAllText(path, text, new UTF8Encoding(false));
        }
        catch (IOException e)
        {
            ModLog.Warn("FlightRecorder: could not write " + path + ": " + e.Message);
            return null;
        }
        catch (UnauthorizedAccessException e)
        {
            ModLog.Warn("FlightRecorder: no permission to write " + path + ": " + e.Message);
            return null;
        }

        { var returnedAt50 = File.Exists(path) ? path : null; if (CoreLog.On) ModLog.Every("FlightFiles.Write:50", 2d, $"FlightFiles.Write: returns {CoreLog.Shown(returnedAt50)} (line 50)"); return returnedAt50; }
    }

    /// <summary>Removes dumps older than <paramref name="keepDays"/> days and the oldest beyond <paramref name="keepMax"/> files.</summary>
    /// <returns>How many files were removed.</returns>
    internal static int Prune(int keepDays, int keepMax)
    {
        int removed = 0;
        try
        {
            var files = new DirectoryInfo(Dir()).GetFiles("*.txt").OrderByDescending(f => f.LastWriteTime).ToList();
            DateTime cutoff = DateTime.Now.AddDays(-Math.Max(1, keepDays));
            for (int i = 0; i < files.Count; i++)
            {
                if (i < keepMax && files[i].LastWriteTime >= cutoff)
                {
                    continue;
                }

                files[i].Delete();
                removed++;
            }
        }
        catch (IOException e)
        {
            ModLog.Warn("FlightRecorder: old dumps not removed: " + e.Message);
        }
        catch (UnauthorizedAccessException e)
        {
            ModLog.Warn("FlightRecorder: no permission to remove old dumps: " + e.Message);
        }

        { var returnedAt82 = removed; if (CoreLog.On) ModLog.Every("FlightFiles.Prune:82", 2d, $"FlightFiles.Prune: returns {CoreLog.Shown(returnedAt82)} (line 82)"); return returnedAt82; }
    }
}
