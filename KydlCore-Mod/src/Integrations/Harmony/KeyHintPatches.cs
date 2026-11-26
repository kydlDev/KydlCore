using HarmonyLib;
using KydlCore.Common;
using KydlCore.Integrations.Valheim;

namespace KydlCore.Integrations.Harmony;

/// <summary>KyKeyHints panels: the row template is taken when the game's KeyHints start, the panel replaces the game's hints while its item is held.</summary>
[HarmonyPatch(typeof(KeyHints), "Start")]
internal static class KeyHintsStart
{
    static void Postfix(KeyHints __instance)
    {
        bool ready = KeyHintPanels.Start(__instance);
        ModLog.Verbose($"KeyHintsStart: KyKeyHints template ready={ready}");
    }
}

[HarmonyPatch(typeof(KeyHints), "UpdateHints")]
internal static class KeyHintsUpdate
{
    static bool Prefix(KeyHints __instance)
    {
        bool runGame = KeyHintPanels.Update(__instance);
        if (KydlCore.Core.CoreLog.Due("KeyHintsUpdate", 5d)) ModLog.Every("KeyHintsUpdate", 5d, $"KeyHintsUpdate: game hints run={runGame}");
        return runGame;
    }
}

[HarmonyPatch(typeof(ZInput), nameof(ZInput.Save))]
internal static class ZInputSaveKeyHints
{
    static void Postfix()
    {
        int marked = KyKeyHints.MarkAllDirty();
        ModLog.Verbose($"ZInputSaveKeyHints: key bindings saved - {marked} KyKeyHints panel(s) redrawn");
    }
}
