using System.Collections.Generic;

namespace KydlCore.Common;

/// <summary>
/// SHARED (KydlCore 1.2.0, replaces Jotunn's ConsoleCommand, D-104). A mod's console command: derive from it and hand an instance to
/// <see cref="KyCommands.Add"/>. Rule 33: commands exist only in a game where KydlTools is loaded (KyCommands checks it).
/// </summary>
public abstract class KyCommand
{
    /// <summary>What the player types (lower case is used).</summary>
    public abstract string Name { get; }

    /// <summary>One line shown by the game's "help".</summary>
    public abstract string Help { get; }

    /// <summary>True: only usable with devcommands (the game's cheat switch).</summary>
    public virtual bool IsCheat => false;

    /// <summary>Runs the command with the words typed after its name.</summary>
    public abstract void Run(string[] args);

    /// <summary>Tab completion for the first argument (empty = none).</summary>
    public virtual List<string> Options() => new List<string>();
}
