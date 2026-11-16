using System;
using System.Collections.Generic;

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
        if (string.IsNullOrEmpty(section)) return section;
        int cut = section.IndexOf(Separator, StringComparison.Ordinal);
        return cut > 0 ? section.Substring(0, cut) : section;
    }

    /// <summary>
    /// The tabs for the sections of a mod. Expected outcome: one group per name, each section once, "General" first and "DebugLog" last;
    /// blank section names are left out; an empty list for no sections.
    /// </summary>
    internal static List<Group> Of(IEnumerable<string> sections)
    {
        var groups = new List<Group>();
        if (sections == null) return groups;
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
        return groups;
    }

    static void Move(List<Group> groups, string name, int to)
    {
        Group group = groups.Find(g => g.Name == name);
        if (group == null) return;
        groups.Remove(group);
        groups.Insert(Math.Min(to, groups.Count), group);
    }
}
