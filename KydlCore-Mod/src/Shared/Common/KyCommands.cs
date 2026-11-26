using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Bootstrap;
using KydlCore.Core;

namespace KydlCore.Common;

/// <summary>
/// SHARED (KydlCore 1.2.0, replaces Jotunn's CommandManager, D-104). Registers a mod's <see cref="KyCommand"/> as a game console command
/// (Terminal.ConsoleCommand registers itself in the game's command list). Rule 33: refused when KydlTools is not loaded, so no player gets a mod
/// command. The game builds its own list in Terminal.InitTerminal: KydlCore registers every command again after it (Integrations/Harmony/CommandPatches).
/// </summary>
public static class KyCommands
{
    /// <summary>KydlTools' BepInEx GUID: commands exist only when it is loaded (rule 33).</summary>
    public const string KydlToolsGuid = "com.kydldev.KydlTools";
    static readonly List<KyCommand> Commands = new();

    /// <summary>Expected outcome: true when the command is registered (false without KydlTools, for null or for a name already taken by another mod's command).</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Add(KyCommand command)
    {
        var ctx = ModContexts.For(Assembly.GetCallingAssembly());
        bool tools = Chainloader.PluginInfos.ContainsKey(KydlToolsGuid);
        if (command == null || !tools)
        {
            ModLog.Verbose(ctx, $"KyCommands.Add: '{command?.Name}' not registered - command={(command != null)}, KydlTools loaded={tools} (rule 33)");
            return false;
        }
        KyCommand taken = Commands.FirstOrDefault(c => string.Equals(c.Name, command.Name, StringComparison.OrdinalIgnoreCase));
        if (taken != null && taken != command) { ModLog.Warn(ctx, $"KyCommands.Add: '{command.Name}' refused - already registered by {taken.GetType().FullName}"); return false; }
        if (taken == null) Commands.Add(command);
        bool registered = Register(command);
        ModLog.Verbose(ctx, $"KyCommands: '{command.Name}' registered={registered} (cheat={command.IsCheat}, total {Commands.Count})");
        return registered;
    }

    /// <summary>Called after Terminal.InitTerminal: every command is registered again (the game rebuilds its list). Expected outcome: the number registered.</summary>
    internal static int RegisterAll()
    {
        int done = Commands.Count(Register);
        ModLog.Verbose($"KyCommands.RegisterAll: {done} of {Commands.Count} command(s) in the game's console");
        return done;
    }

    static bool Register(KyCommand command)
    {
        new Terminal.ConsoleCommand(command.Name, command.Help, args => Run(command, args), command.IsCheat, optionsFetcher: () => command.Options());
        var list = typeof(Terminal).GetField("commands", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(null) as System.Collections.IDictionary;
        bool known = list != null && list.Contains(command.Name.ToLowerInvariant());
        if (CoreLog.On) ModLog.Verbose($"KyCommands.Register: '{command.Name}' in the game's list={known}");
        return known;
    }

    static void Run(KyCommand command, Terminal.ConsoleEventArgs args)
    {
        string[] words = args.Args == null || args.Args.Length < 2 ? new string[0] : args.Args.Skip(1).ToArray();
        ModLog.Verbose($"KyCommands: '{command.Name}' runs with {words.Length} argument(s): {string.Join(" ", words)}");
        try { command.Run(words); }
        catch (Exception e)       // a mod's command: the console shows the failure instead of the game logging an unhandled exception
        {
            ModLog.Warn($"KyCommands: '{command.Name}' ({command.GetType().FullName}) threw {e.GetType().FullName}: {e.Message} - shown in the console");
            args.Context?.AddString($"{command.Name} failed: {e.Message}");
        }
    }
}
