using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;

namespace KydlCore.Common;

/// <summary>Finds the <see cref="ModContext"/> of a mod. Public entry points of the shared helpers pass <c>Assembly.GetCallingAssembly()</c> (they are NoInlining, so that is the mod).</summary>
public static class ModContexts
{
    sealed class Last { public Assembly Asm; public ModContext Ctx; }

    static readonly object Gate = new();
    static readonly Dictionary<Assembly, ModContext> ByAssembly = new();
    static volatile Last _last;       // a mod logs thousands of times in a row: the previous answer is the answer

    /// Expected outcome: the one context of this assembly (created on first use, named after the assembly).
    public static ModContext For(Assembly assembly)
    {
        if (assembly == null) assembly = typeof(ModContexts).Assembly;
        var l = _last;
        if (l != null && ReferenceEquals(l.Asm, assembly)) return l.Ctx;
        lock (Gate)
        {
            if (!ByAssembly.TryGetValue(assembly, out var ctx)) { ctx = new ModContext(assembly.GetName().Name); ByAssembly[assembly] = ctx; }
            _last = new Last { Asm = assembly, Ctx = ctx };
            return ctx;
        }
    }

    /// Test support: puts every context back to "never used" (name = assembly name, no log source, no session line, empty repeat filter). Expected outcome: true.
    internal static bool ResetAllForTests()
    {
        lock (Gate)
        {
            foreach (var c in ByAssembly.Values)
            {
                c.Name = c.DefaultName; c.Source = null; c.SessionStarted = false; c.DebugOn = null;
                lock (c.RepeatLock) { c.LastWritten.Clear(); c.OkSeen.Clear(); c.EveryLast.Clear(); }
                c.English = new Dictionary<string, string>();
            }
            return true;
        }
    }

    /// Test support: forget the repeat filter of every context (a fresh game launch) but keep their log sources and sessions. Expected outcome: true.
    internal static bool ResetRepeatsForTests()
    {
        lock (Gate) { foreach (var c in ByAssembly.Values) lock (c.RepeatLock) { c.LastWritten.Clear(); c.OkSeen.Clear(); c.EveryLast.Clear(); } return true; }
    }

    /// Test support: the context that has been initialised (its session line was written); null when none.
    internal static ModContext AnyInitialisedForTests() { lock (Gate) { foreach (var c in ByAssembly.Values) if (c.SessionStarted) return c; return null; } }

    /// Every mod's registered English text table (Loc.Register), for the game's localization (Integrations/Valheim/GameText). Expected outcome: one list entry per mod that registered texts.
    internal static List<KeyValuePair<string, Dictionary<string, string>>> EnglishTables()
    {
        var tables = new List<KeyValuePair<string, Dictionary<string, string>>>();
        lock (Gate) { foreach (var c in ByAssembly.Values) if (c.English != null && c.English.Count > 0) tables.Add(new KeyValuePair<string, Dictionary<string, string>>(c.Name, c.English)); }
        return tables;
    }

    /// Test support: the names of all contexts created so far.
    internal static string[] Names() { lock (Gate) { var n = new List<string>(); foreach (var c in ByAssembly.Values) n.Add(c.Name); return n.ToArray(); } }

    /// Expected outcome: the context of the assembly that declares <paramref name="type"/> (for helpers that are handed a mod type, e.g. its InGameTests class).
    public static ModContext For(Type type) { return For(type?.Assembly); }
}
