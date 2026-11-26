using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using KydlCore.Core;
using UnityEngine;

namespace KydlCore.Common;

/// <summary>
/// SHARED (KydlCore 1.2.0, replaces Jotunn's SkillManager.AddSkill, D-104). A mod's own skill: its SkillType number is the stable hash of the
/// identifier (the same number Jotunn gave it, so saved levels of players stay), the skills screen shows "$skill_&lt;number&gt;", which KydlCore
/// registers as the display name. The game accepts the number through the Skills patches (Integrations/Harmony/SkillPatches).
/// Not ported: the "raiseskill / resetskill &lt;name&gt;" cheat console commands for custom skills.
/// </summary>
public static class KySkills
{
    static readonly Dictionary<Skills.SkillType, Skills.SkillDef> Defs = new();

    /// <summary>Adds the skill once (a second call with the same identifier returns the same type). Expected outcome: the skill's SkillType.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Skills.SkillType Add(string identifier, string name, string description, Sprite icon, float increaseStep = 1f)
    {
        var ctx = ModContexts.For(Assembly.GetCallingAssembly());
        var type = (Skills.SkillType)KySkillRules.TypeNumber(identifier);
        if (!Defs.ContainsKey(type))
        {
            Defs[type] = new Skills.SkillDef { m_skill = type, m_icon = icon, m_description = description ?? "", m_increseStep = increaseStep };
            string token = $"skill_{(int)type}";
            var table = ModContexts.For(typeof(KySkills)).English;
            table[token] = name ?? identifier;
            table[$"{token}_description"] = description ?? "";
            Loc.GameSink?.Invoke(new Dictionary<string, string> { { token, name ?? identifier } });
        }
        ModLog.Verbose(ctx, $"KySkills: '{identifier}' = {(int)type} ('{name}', step {increaseStep}, icon={icon != null}) - skills added {Defs.Count}");
        return type;
    }

    /// <summary>
    /// Sets the icon of a skill added before (Jotunn read SkillConfig.Icon late, so mods filled it in after AddSkill, e.g. RidingCrop's rendered
    /// crop icon). The game reads the definition when it draws the skills screen. Expected outcome: true when the skill is one of the mods' skills.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool SetIcon(Skills.SkillType type, Sprite icon)
    {
        var ctx = ModContexts.For(Assembly.GetCallingAssembly());
        bool known = Defs.TryGetValue(type, out Skills.SkillDef def);
        if (known) def.m_icon = icon;
        ModLog.Verbose(ctx, $"KySkills.SetIcon: {(int)type} icon={icon != null} -> {(known ? "set" : "not a mod skill, ignored")}");
        return known;
    }

    /// <summary>True when the number is one of the mods' skills (Skills patches).</summary>
    internal static bool IsCustom(Skills.SkillType type)
    {
        bool custom = Defs.ContainsKey(type);
        if (CoreLog.Due("KySkills.IsCustom", 5d)) ModLog.Every("KySkills.IsCustom", 5d, $"KySkills.IsCustom: {(int)type} -> {custom}");
        return custom;
    }

    /// <summary>The mod skill's definition (null for a game skill).</summary>
    internal static Skills.SkillDef Def(Skills.SkillType type)
    {
        Defs.TryGetValue(type, out Skills.SkillDef def);
        if (CoreLog.Due("KySkills.Def", 5d)) ModLog.Every("KySkills.Def", 5d, $"KySkills.Def: {(int)type} -> {(def == null ? "<none>" : "custom")}");
        return def;
    }
}
