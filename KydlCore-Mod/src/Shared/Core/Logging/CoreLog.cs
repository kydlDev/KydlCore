using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace KydlCore.Core
{
    /// <summary>
    /// Debug logging for KydlCore's pure code (Shared/Core and the features' Domain / Application / Execution): PROJECT_RULES 8 "every method logs"
    /// (user 2026-11-20). The pure code is also compiled into the unit tests of KydlCore and of every mod (they link Shared/Core), which run without the
    /// game, BepInEx or ModLog, so it cannot call ModLog itself. KydlCore's Plugin.Awake connects this to ModLog (Sink / DebugOn); anywhere else nothing
    /// is connected and every call does nothing. Internal on purpose: each assembly that compiles Shared/Core gets its own copy and no public API changes.
    /// </summary>
    internal static class CoreLog
    {
        static readonly Dictionary<string, long> LastWritten = new Dictionary<string, long>();
        static readonly Stopwatch Clock = Stopwatch.StartNew();

        /// <summary>Where the lines go (ModLog.Verbose in KydlCore; null elsewhere).</summary>
        internal static Action<string> Sink { get; set; }

        /// <summary>Whether debug logging is on (KydlCore's [DebugLog] switch; null = off).</summary>
        internal static Func<bool> DebugOn { get; set; }

        /// <summary>True when a line would be written: callers check it before building the text, so nothing is paid with debug logging off.</summary>
        internal static bool On => Sink != null && DebugOn != null && DebugOn();

        /// <summary>Writes the line when debug logging is on.</summary>
        internal static void Verbose(string message)
        {
            if (On) Sink(message);
        }

        /// <summary>Writes the line at most once per <paramref name="seconds"/> for this key (code that runs every frame). Expected outcome: true when written.</summary>
        internal static bool Every(string key, double seconds, string message)
        {
            if (!On) return false;
            long now = Clock.ElapsedMilliseconds;
            lock (LastWritten)
            {
                if (LastWritten.TryGetValue(key, out long last) && now - last < seconds * 1000d) return false;
                LastWritten[key] = now;
            }
            Sink(message);
            return true;
        }
    }
}
