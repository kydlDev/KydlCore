using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;

namespace KydlCore.Common;

/// <summary>
/// The identity and the per-mod state of ONE mod that uses the shared helpers (ModLog, Loc, Tunable, TestArena ...). KydlCore.dll holds the only copy of
/// that code, so what used to be "static in my own DLL" is kept here, one context per calling assembly: a log line written by a mod carries that mod's
/// name, never KydlCore's. A mod needs no setup beyond the usual <c>ModLog.Init</c> in Plugin.Awake (without it the assembly name is the mod name).
/// </summary>
public sealed class ModContext
{
    internal ModContext(string name) { Name = name; DefaultName = name; }
    internal readonly string DefaultName;

    /// The mod's name as it appears in front of its log lines (the assembly name until ModLog.Init sets another).
    public string Name { get; internal set; }

    // ---- ModLog ----
    internal ManualLogSource Source;
    internal bool SessionStarted;
    internal Func<bool> DebugOn;
    internal readonly Dictionary<string, KeyValuePair<long, int>> OkSeen = new();
    internal readonly Dictionary<string, double> LastWritten = new();
    internal readonly object RepeatLock = new();
    /// ModLog.Every: when each status key was last written (Stopwatch seconds), per mod.
    internal readonly Dictionary<string, double> EveryLast = new();

    // ---- Loc ----
    internal Dictionary<string, string> English = new();
}
