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
/// Applied by hand (BuildCategoryPatchInfo.Install) only when Jotunn is not installed: Jotunn already patches the same methods.
/// Written against Valheim 1.0.21; the targets are in PatchTargetsTests (run game-api-diff after every game update).
/// </summary>
internal static class BuildCategoryPatchInfo
{
    /// <summary>
    /// Applied by hand from Plugin.Awake (not [HarmonyPatch]) so they exist only when KydlCore makes the categories itself: with Jotunn installed it already
    /// patches the same methods (and GameProbe's B06 would expect attribute patches that are off on purpose). Expected outcome: the number of methods patched.
    /// </summary>
    internal static int Install(HarmonyLib.Harmony harmony)
    {
        if (!KyBuildCategory.OwnPatches) { ModLog.Verbose("BuildCategoryPatches: not applied - Jotunn is installed and makes the build categories"); return 0; }
        int done = 0;
        done += Patch(harmony, AccessTools.Method(typeof(Enum), nameof(Enum.GetValues)), typeof(EnumGetValuesCategories), post: true);
        done += Patch(harmony, AccessTools.Method(typeof(Enum), nameof(Enum.GetNames)), typeof(EnumGetNamesCategories), post: true);
        done += Patch(harmony, AccessTools.Method(typeof(PieceTable), nameof(PieceTable.UpdateAvailable)), typeof(PieceTableUpdateAvailableCategories), pre: true, post: true, trans: true);
        done += Patch(harmony, AccessTools.Method(typeof(ByUsagePieceList), nameof(ByUsagePieceList.UpdateAvailableTags)), typeof(ByUsageUpdateTags), post: true);
        done += Patch(harmony, AccessTools.Method(typeof(ByUsagePieceList), nameof(ByUsagePieceList.GetTagDisplayName)), typeof(ByUsageTagName), pre: true);
        done += Patch(harmony, AccessTools.Method(typeof(ByUsagePieceList), nameof(ByUsagePieceList.GetAvailablePiecesWithTag)), typeof(ByUsagePiecesWithTag), post: true, trans: true);
        done += Patch(harmony, AccessTools.Method(typeof(ByUsagePieceList), "GetTagById"), typeof(ByUsageGetTagById), pre: true);
        foreach (MethodBase m in HudCategoryRefresh.Targets()) done += Patch(harmony, m, typeof(HudCategoryRefresh), post: true);
        ModLog.Verbose($"BuildCategoryPatches: {done} game method(s) patched by KydlCore (no Jotunn installed)");
        return done;
    }

    static int Patch(HarmonyLib.Harmony harmony, MethodBase target, Type patches, bool pre = false, bool post = false, bool trans = false)
    {
        if (target == null) { ModLog.Warn($"BuildCategoryPatches: a target of {patches.Name} is missing (game update?) - the KY-Items tab may not show"); return 0; }
        HarmonyMethod Hm(string name) => new HarmonyMethod(AccessTools.Method(patches, name));
        harmony.Patch(target, pre ? Hm("Prefix") : null, post ? Hm("Postfix") : null, trans ? Hm("Transpiler") : null);
        if (CoreLog.On) ModLog.Verbose($"BuildCategoryPatches.Patch: {target.DeclaringType?.Name}.{target.Name} <- {patches.Name}");
        return 1;
    }
}

internal static class EnumGetValuesCategories
{
    static void Postfix(Type enumType, ref Array __result) { if (CoreLog.Due("EnumGetValuesCategories", 5d)) ModLog.Every("EnumGetValuesCategories", 5d, $"EnumGetValuesCategories: {enumType?.Name} ({__result?.Length} values)"); if (enumType == typeof(Piece.PieceCategory)) __result = KyBuildCategory.WithOwnValues(__result); }
}

internal static class EnumGetNamesCategories
{
    static void Postfix(Type enumType, ref string[] __result) { if (CoreLog.Due("EnumGetNamesCategories", 5d)) ModLog.Every("EnumGetNamesCategories", 5d, $"EnumGetNamesCategories: {enumType?.Name} ({__result?.Length} names)"); if (enumType == typeof(Piece.PieceCategory)) __result = KyBuildCategory.WithOwnNames(__result); }
}

internal static class PieceTableUpdateAvailableCategories
{
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

internal static class ByUsageUpdateTags
{
    static void Postfix(ByUsagePieceList __instance, PieceTable pieceTable) { int added = KyBuildCategory.AddUsageTags(__instance, pieceTable); if (CoreLog.Due("ByUsageUpdateTags", 5d)) ModLog.Every("ByUsageUpdateTags", 5d, $"ByUsageUpdateTags: {added} custom tag(s) for {pieceTable?.name}"); }
}

internal static class ByUsageTagName
{
    static bool Prefix(ByUsagePieceList __instance, int index, ref string __result)
    {
        string name = KyBuildCategory.TagName(__instance, index);
        bool runGame = name == null;
        if (!runGame) __result = name;
        if (CoreLog.Due("ByUsageTagName.Prefix", 5d)) ModLog.Every("ByUsageTagName.Prefix", 5d, $"ByUsageTagName: index {index} -> {name ?? "<the game's name>"}, game runs={runGame}"); 
        return runGame;
    }
}

internal static class ByUsagePiecesWithTag
{
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

internal static class ByUsageGetTagById
{
    static bool Prefix(ByUsagePieceList __instance, int id, ref Piece.UsageTagFlags __result)
    {
        bool runGame = !KyBuildCategory.IsCustomTagId(__instance, id);
        if (!runGame) __result = (Piece.UsageTagFlags)(-1);
        if (CoreLog.Due("ByUsageGetTagById.Prefix", 5d)) ModLog.Every("ByUsageGetTagById.Prefix", 5d, $"ByUsageGetTagById: id {id} - game runs={runGame}"); 
        return runGame;
    }
}

internal static class HudCategoryRefresh
{

    internal static IEnumerable<MethodBase> Targets()
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
