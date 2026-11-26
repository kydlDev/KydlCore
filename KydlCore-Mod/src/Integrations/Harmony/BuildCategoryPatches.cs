using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using KydlCore.Common;
using KydlCore.Core;
using KydlCore.Integrations.Valheim;

namespace KydlCore.Integrations.Harmony;

/// <summary>
/// The game patches behind KyBuildCategory (custom Hammer categories without Jotunn, D-106; ported from Jotunn 2.30.2 PieceManager.Patches).
/// Every class has Prepare = KyBuildCategory.OwnPatches: when Jotunn is installed it already patches the same methods, so these stay off.
/// Written against Valheim 1.0.21; the targets are in PatchTargetsTests (run game-api-diff after every game update).
/// </summary>
internal static class BuildCategoryPatchInfo
{
    internal static bool Prepare(string what)
    {
        bool on = KyBuildCategory.OwnPatches;
        ModLog.Verbose($"BuildCategoryPatches: {what} {(on ? "patched by KydlCore" : "left to Jotunn (installed)")}");
        return on;
    }
}

[HarmonyPatch(typeof(Enum), nameof(Enum.GetValues))]
internal static class EnumGetValuesCategories
{
    static bool Prepare() => BuildCategoryPatchInfo.Prepare("Enum.GetValues");
    static void Postfix(Type enumType, ref Array __result) { if (CoreLog.Due("EnumGetValuesCategories", 5d)) ModLog.Every("EnumGetValuesCategories", 5d, $"EnumGetValuesCategories: {enumType?.Name} ({__result?.Length} values)"); if (enumType == typeof(Piece.PieceCategory)) __result = KyBuildCategory.WithOwnValues(__result); }
}

[HarmonyPatch(typeof(Enum), nameof(Enum.GetNames))]
internal static class EnumGetNamesCategories
{
    static bool Prepare() => BuildCategoryPatchInfo.Prepare("Enum.GetNames");
    static void Postfix(Type enumType, ref string[] __result) { if (CoreLog.Due("EnumGetNamesCategories", 5d)) ModLog.Every("EnumGetNamesCategories", 5d, $"EnumGetNamesCategories: {enumType?.Name} ({__result?.Length} names)"); if (enumType == typeof(Piece.PieceCategory)) __result = KyBuildCategory.WithOwnNames(__result); }
}

[HarmonyPatch(typeof(PieceTable), nameof(PieceTable.UpdateAvailable))]
internal static class PieceTableUpdateAvailableCategories
{
    static bool Prepare() => BuildCategoryPatchInfo.Prepare("PieceTable.UpdateAvailable");
    static void Prefix(PieceTable __instance) { int added = KyBuildCategory.ExpandAvailable(__instance); if (CoreLog.Due("PieceTableUpdateAvailableCategories.Prefix", 5d)) ModLog.Every("PieceTableUpdateAvailableCategories.Prefix", 5d, $"PieceTableUpdateAvailableCategories: {__instance?.name} - {added} list(s) added"); }
    static void Postfix(PieceTable __instance) { int size = KyBuildCategory.ResizeSelection(__instance); if (CoreLog.Due("PieceTableUpdateAvailableCategories.Postfix", 5d)) ModLog.Every("PieceTableUpdateAvailableCategories.Postfix", 5d, $"PieceTableUpdateAvailableCategories: {__instance?.name} - selection size {size}"); }

    /// The game sizes its category lists with the constant PieceCategory.Max: replaced by KyBuildCategory.MaxCategory() (vanilla + custom).
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        int vanillaMax = Enum.TryParse("Max", out Piece.PieceCategory max) ? (int)max : 9;
        MethodInfo ours = AccessTools.Method(typeof(KyBuildCategory), nameof(KyBuildCategory.MaxCategory));
        int replaced = 0;
        foreach (CodeInstruction ci in instructions)
        {
            if ((ci.opcode == OpCodes.Call && ci.operand is MethodInfo m && m.Name.Contains("MaxCategory")) || ci.LoadsConstant(vanillaMax))
            {
                replaced++;
                yield return new CodeInstruction(OpCodes.Call, ours).MoveLabelsFrom(ci);
            }
            else yield return ci;
        }
        ModLog.Verbose($"PieceTableUpdateAvailableCategories: {replaced} use(s) of the category maximum {vanillaMax} replaced");
    }
}

[HarmonyPatch(typeof(ByUsagePieceList), nameof(ByUsagePieceList.UpdateAvailableTags))]
internal static class ByUsageUpdateTags
{
    static bool Prepare() => BuildCategoryPatchInfo.Prepare("ByUsagePieceList.UpdateAvailableTags");
    static void Postfix(ByUsagePieceList __instance, PieceTable pieceTable) { int added = KyBuildCategory.AddUsageTags(__instance, pieceTable); if (CoreLog.Due("ByUsageUpdateTags", 5d)) ModLog.Every("ByUsageUpdateTags", 5d, $"ByUsageUpdateTags: {added} custom tag(s) for {pieceTable?.name}"); }
}

[HarmonyPatch(typeof(ByUsagePieceList), nameof(ByUsagePieceList.GetTagDisplayName))]
internal static class ByUsageTagName
{
    static bool Prepare() => BuildCategoryPatchInfo.Prepare("ByUsagePieceList.GetTagDisplayName");
    static bool Prefix(ByUsagePieceList __instance, int index, ref string __result)
    {
        string name = KyBuildCategory.TagName(__instance, index);
        bool runGame = name == null;
        if (!runGame) __result = name;
        if (CoreLog.Due("ByUsageTagName.Prefix", 5d)) ModLog.Every("ByUsageTagName.Prefix", 5d, $"ByUsageTagName: index {index} -> {name ?? "<the game's name>"}, game runs={runGame}"); 
        return runGame;
    }
}

[HarmonyPatch(typeof(ByUsagePieceList), nameof(ByUsagePieceList.GetAvailablePiecesWithTag))]
internal static class ByUsagePiecesWithTag
{
    static bool Prepare() => BuildCategoryPatchInfo.Prepare("ByUsagePieceList.GetAvailablePiecesWithTag");
    static void Postfix(ByUsagePieceList __instance, int tagId, PieceTable pieceTable, IList<Piece> resultOut) { int added = KyBuildCategory.PiecesWithTag(__instance, tagId, pieceTable, resultOut); if (CoreLog.Due("ByUsagePiecesWithTag.Postfix", 5d)) ModLog.Every("ByUsagePiecesWithTag.Postfix", 5d, $"ByUsagePiecesWithTag: tag {tagId} - {added} custom piece(s), {resultOut?.Count} total"); }

    /// The game reads its usage-tag array with the tag id; a custom id is past its end: GetTagById is replaced by a bounds-checked copy.
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo theirs = AccessTools.Method(typeof(ByUsagePieceList), "GetTagById");
        MethodInfo ours = AccessTools.Method(typeof(KyBuildCategory), nameof(KyBuildCategory.SafeGetTagById));
        int replaced = 0;
        foreach (CodeInstruction ci in instructions)
        {
            if (theirs != null && ci.Calls(theirs)) { ci.opcode = OpCodes.Call; ci.operand = ours; replaced++; }
            yield return ci;
        }
        ModLog.Verbose($"ByUsagePiecesWithTag: {replaced} GetTagById call(s) made bounds-safe");
    }
}

[HarmonyPatch(typeof(ByUsagePieceList), "GetTagById")]
internal static class ByUsageGetTagById
{
    static bool Prepare() => BuildCategoryPatchInfo.Prepare("ByUsagePieceList.GetTagById");
    static bool Prefix(ByUsagePieceList __instance, int id, ref Piece.UsageTagFlags __result)
    {
        bool runGame = !KyBuildCategory.IsCustomTagId(__instance, id);
        if (!runGame) __result = (Piece.UsageTagFlags)(-1);
        if (CoreLog.Due("ByUsageGetTagById.Prefix", 5d)) ModLog.Every("ByUsageGetTagById.Prefix", 5d, $"ByUsageGetTagById: id {id} - game runs={runGame}"); 
        return runGame;
    }
}

[HarmonyPatch]
internal static class HudCategoryRefresh
{
    static bool Prepare() => BuildCategoryPatchInfo.Prepare("Hud / Player category refresh");

    static IEnumerable<MethodBase> TargetMethods()
    {
        ModLog.Verbose($"HudCategoryRefresh: targets Hud.Awake, Hud.UpdateBuild, Player.SetPlaceMode (custom categories={KyBuildCategory.MaxCategory()})");
        yield return AccessTools.Method(typeof(Hud), "Awake");
        yield return AccessTools.Method(typeof(Hud), "UpdateBuild");
        yield return AccessTools.Method(typeof(Player), "SetPlaceMode");
    }

    static void Postfix(MethodBase __originalMethod)
    {
        if (CoreLog.Due("HudCategoryRefresh.Postfix", 5d)) ModLog.Every("HudCategoryRefresh.Postfix", 5d, $"HudCategoryRefresh: after {__originalMethod.DeclaringType?.Name}.{__originalMethod.Name}"); 
        if (__originalMethod.Name == "UpdateBuild") KyBuildCategory.RefreshIfNeeded();
        else KyBuildCategory.Refresh();
    }
}
