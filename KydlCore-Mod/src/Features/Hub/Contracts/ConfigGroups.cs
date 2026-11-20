using System;
using System.Collections.Generic;
using KydlCore.Core;
using KydlCore.Common;

namespace KydlCore.Features.Hub.Contracts;

/// <summary>
/// How the config sections of a mod become row-2 tabs of the KY hub (user, 2026-11-16: every category gets its own tab, for every mod;
/// sections that belong together are one tab). A group is named by the part of the section name before " - ": "Staff of Protection -
/// Appearance", "Staff of Protection - Yourself" and "Staff of Protection - Player: Bjorn" are the tab "Staff of Protection"; a section
/// without " - " is its own tab. Order: "General" first, "DebugLog" last, the others as the mod made them. Pure: no Unity / BepInEx types.
/// </summary>
internal static class ConfigGroups
{
    internal const string Separator = " - ", First = "General", Last = "DebugLog";

    /// <summary>One tab: its name and the config sections it shows (in the mod's order).</summary>
    internal sealed class Group
    {
        internal string Name { get; }
        internal List<string> Sections { get; } = new List<string>();
        internal Group(string name) { Name = name; }
    }

    /// <summary>The tab a section belongs to. Expected outcome: the text before the first " - ", or the whole name when there is none (or nothing before it).</summary>
    internal static string NameOf(string section)
    {
        if (CoreLog.On) ModLog.Every("Group.NameOf", 2d, $"Group.NameOf(section={section})");
        if (string.IsNullOrEmpty(section)) { var returnedAt30 = section; if (CoreLog.On) ModLog.Every("Group.NameOf:30", 2d, $"Group.NameOf: returns {returnedAt30} (line 30)"); return returnedAt30; }
        int cut = section.IndexOf(Separator, StringComparison.Ordinal);
        { var returnedAt32 = cut > 0 ? section.Substring(0, cut) : section; if (CoreLog.On) ModLog.Every("Group.NameOf:32", 2d, $"Group.NameOf: returns {returnedAt32} (line 32)"); return returnedAt32; }
    }

    /// <summary>
    /// The tabs for the sections of a mod. Expected outcome: one group per name, each section once, "General" first and "DebugLog" last;
    /// blank section names are left out; an empty list for no sections.
    /// </summary>
    internal static List<Group> Of(IEnumerable<string> sections)
    {
        if (CoreLog.On) ModLog.Every("Group.Of", 2d, $"Group.Of(sections={sections})");
        var groups = new List<Group>();
        if (sections == null) { var returnedAt43 = groups; if (CoreLog.On) ModLog.Every("Group.Of:43", 2d, $"Group.Of: returns {returnedAt43} (line 43)"); return returnedAt43; }
        foreach (string section in sections)
        {
            if (string.IsNullOrEmpty(section)) continue;
            string name = NameOf(section);
            Group group = groups.Find(g => g.Name == name);
            if (group == null) { group = new Group(name); groups.Add(group); }
            if (!group.Sections.Contains(section)) group.Sections.Add(section);
        }

        Move(groups, First, 0);
        Move(groups, Last, groups.Count - 1);
        { var returnedAt55 = groups; if (CoreLog.On) ModLog.Every("Group.Of:55", 2d, $"Group.Of: returns {returnedAt55} (line 55)"); return returnedAt55; }
    }

    /// <summary>
    /// The sections a tab draws as plain entries when a mod's own panel replaces some of them (KyHub.SetGroupPanel). Expected outcome: the given
    /// sections in their order without the ones `replaced` says yes to; all of them when `replaced` is null; an empty list for no sections.
    /// </summary>
    internal static List<string> Shown(IEnumerable<string> sections, Func<string, bool> replaced)
    {
        if (CoreLog.On) ModLog.Verbose($"Group.Shown(sections={sections}, replaced={replaced})");
        var shown = new List<string>();
        if (sections == null) { var returnedAt66 = shown; if (CoreLog.On) ModLog.Verbose($"Group.Shown: returns {returnedAt66} (line 66)"); return returnedAt66; }
        foreach (string section in sections)
            if (replaced == null || !replaced(section)) shown.Add(section);
        { var returnedAt69 = shown; if (CoreLog.On) ModLog.Verbose($"Group.Shown: returns {returnedAt69} (line 69)"); return returnedAt69; }
    }

    static void Move(List<Group> groups, string name, int to)
    {
        if (CoreLog.On) ModLog.Every("Group.Move", 2d, $"Group.Move(groups={groups}, name={name}, to={to})");
        Group group = groups.Find(g => g.Name == name);
        if (group == null) { if (CoreLog.On) ModLog.Every("Group.Move:76", 2d, "Group.Move: returns (line 76)"); return; }
        groups.Remove(group);
        groups.Insert(Math.Min(to, groups.Count), group);
    }
}
