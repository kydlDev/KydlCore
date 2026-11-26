using HarmonyLib;
using KydlCore.Common;

namespace KydlCore.Integrations.Harmony;

/// <summary>The game (re)builds its console command list in Terminal.InitTerminal: the mods' KyCommands are registered again after it.</summary>
[HarmonyPatch(typeof(Terminal), "InitTerminal")]
internal static class TerminalInitCommands
{
    static void Postfix()
    {
        int done = KyCommands.RegisterAll();
        ModLog.Verbose($"TerminalInitCommands: {done} mod command(s) registered after the game's list");
    }
}
