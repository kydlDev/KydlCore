using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using BepInEx.Configuration;
using UnityEngine;
using KydlCore.Core;

namespace KydlCore.Common;

/// <summary>
/// SHARED helpers around the player's BepInEx config (lessons from DrainTarPit):
///  * CommaFix: "3,5" typed by hand (European habit) was read by BepInEx as 35. Capture the file before the first Bind, Apply after the last.
///  * DropRemoved: entries that are no longer bound (removed / moved to a Tunable) are dropped, so the file only lists what a player can change.
///  * Watcher: BepInEx does not watch the .cfg file; edits on disk are reloaded live (SettingChanged fires for every changed entry).
/// Rule (user): config descriptions never mention older versions, old names or old values.
/// </summary>
public static class ConfigTools
{
    static readonly Regex Section = new(@"^\s*\[(?<s>[^\]]+)\]\s*$");
    static readonly Regex CommaNumber = new(@"^\s*(?<k>[^=#]+?)\s*=\s*(?<v>-?\d+,\d+)\s*$");

    /// The config file's lines as they were before binding (null when there is no file yet). Call BEFORE the first Bind.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string[] Capture(ConfigFile c)
    {
        var ctx = ModContexts.For(Assembly.GetCallingAssembly());
        try { { var returnedAt31 = c != null && !string.IsNullOrEmpty(c.ConfigFilePath) && File.Exists(c.ConfigFilePath) ? File.ReadAllLines(c.ConfigFilePath) : null; if (CoreLog.On) ModLog.Verbose($"ConfigTools.Capture: returns {CoreLog.Shown(returnedAt31)} (line 31)"); return returnedAt31; } }
        catch (IOException e) { ModLog.Warn(ctx, $"Config comma check skipped (file not readable): {e.Message}"); return null; }
        catch (UnauthorizedAccessException e) { ModLog.Warn(ctx, $"Config comma check skipped (no access): {e.Message}"); return null; }
    }

    /// Call AFTER every Bind. Returns the number of float / double entries whose comma was read as the decimal point.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int ApplyCommaFix(ConfigFile c, string[] lines)
    {
        var ctx = ModContexts.For(Assembly.GetCallingAssembly());
        int fixedCount = 0;
        if (c == null || lines == null) { ModLog.Verbose(ctx, "ConfigTools: no saved config lines, comma check not needed"); return fixedCount; }
        string section = "";
        foreach (var line in lines)
        {
            var sm = Section.Match(line);
            if (sm.Success) { section = sm.Groups["s"].Value.Trim(); continue; }
            var m = CommaNumber.Match(line);
            if (!m.Success) continue;
            var def = new ConfigDefinition(section, m.Groups["k"].Value);
            if (!c.ContainsKey(def)) continue;
            var entry = c[def];
            if (entry.SettingType != typeof(float) && entry.SettingType != typeof(double)) continue;
            double meant;
            if (!double.TryParse(m.Groups["v"].Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out meant)) continue;
            var was = entry.BoxedValue;
            entry.BoxedValue = entry.SettingType == typeof(float) ? (object)(float)meant : meant;
            fixedCount++;
            ModLog.Warn(ctx, "Config [" + section + "] " + def.Key + " = " + m.Groups["v"].Value + ": a comma is read as the decimal point -> " +
                        Convert.ToString(entry.BoxedValue, CultureInfo.InvariantCulture) + " (BepInEx had read " +
                        Convert.ToString(was, CultureInfo.InvariantCulture) + "). Please use a dot in the config file.");
        }
        ModLog.Verbose(ctx, $"ConfigTools: comma check done, {fixedCount} value(s) corrected");
        return fixedCount;
    }

    /// Entries that are not bound any more are dropped from the file. Returns the number of dropped entries.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int DropRemoved(ConfigFile c)
    {
        var ctx = ModContexts.For(Assembly.GetCallingAssembly());
        int dropped = 0;
        try
        {
            // the property is not public in every BepInEx build
            var orphans = typeof(ConfigFile).GetProperty("OrphanedEntries", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var dict = orphans != null ? orphans.GetValue(c, null) as IDictionary : null;
            if (dict != null) { dropped = dict.Count; dict.Clear(); }
            else ModLog.Verbose(ctx, "ConfigTools: OrphanedEntries not found in this BepInEx, old entries stay in the file");
            c.Save();
            if (dropped > 0) ModLog.Info(ctx, $"Config: {dropped} entry(ies) that are no longer settings were removed from the file");
        }
        catch (TargetInvocationException e) { ModLog.Warn(ctx, $"Config: old entries could not be removed: {e.InnerException?.Message}"); }
        catch (IOException e) { ModLog.Warn(ctx, $"Config: the file could not be saved after removing old entries: {e.Message}"); }
        return dropped;
    }
}
