using HarmonyLib;
using KydlCore.Common;
using KydlCore.Integrations.Valheim;

namespace KydlCore.Integrations.Harmony;

/// <summary>The game loads its language files in Localization.SetupLanguage (and clears its table on a language change): every mod's English texts are added right after (GameText, D-104).</summary>
[HarmonyPatch(typeof(Localization), nameof(Localization.SetupLanguage))]
internal static class LocalizationPatch
{
    static void Postfix(Localization __instance, string language, bool __result)
    {
        int added = GameText.AddAll(__instance);
        ModLog.Verbose($"LocalizationPatch: SetupLanguage('{language}') loaded={__result} - {added} mod word(s) added after it");
    }
}
