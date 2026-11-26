using HarmonyLib;
using KydlCore.Common;
using KydlCore.Core;

namespace KydlCore.Integrations.Harmony;

/// <summary>The game checks a skill number with Enum.IsDefined: a mod skill (KySkills) is valid too, so saved levels load and Raise works (Jotunn SkillManager before 1.2.0).</summary>
[HarmonyPatch(typeof(Skills), "IsSkillValid")]
internal static class SkillsIsValid
{
    static void Postfix(Skills.SkillType type, ref bool __result)
    {
        if (!__result && KySkills.IsCustom(type)) __result = true;
        if (CoreLog.Due("SkillsIsValid", 5d)) ModLog.Every("SkillsIsValid", 5d, $"SkillsIsValid: {(int)type} -> {__result}");
    }
}

/// <summary>The game finds a skill's definition in its own list: a mod skill's definition comes from KySkills.</summary>
[HarmonyPatch(typeof(Skills), "GetSkillDef")]
internal static class SkillsGetDef
{
    static void Postfix(Skills.SkillType type, ref Skills.SkillDef __result)
    {
        if (__result == null) __result = KySkills.Def(type);
        if (CoreLog.Due("SkillsGetDef", 5d)) ModLog.Every("SkillsGetDef", 5d, $"SkillsGetDef: {(int)type} -> {(__result == null ? "<none>" : "found")}");
    }
}
