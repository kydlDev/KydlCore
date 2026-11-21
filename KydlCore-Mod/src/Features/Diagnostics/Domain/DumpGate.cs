using System;
using System.Collections.Generic;
using KydlCore.Core;

namespace KydlCore.Features.Diagnostics.Domain;

/// <summary>
/// Decides whether a flight recorder dump may be written now: at most one dump per kind within its gap (a CHECK OFF repeated every frame
/// writes one dump, not thousands) and at most a fixed number per game start (a player's disk never fills up).
/// Pure C#: no Unity, no game types, no logging, so it is unit tested. Not thread-safe: the owner locks.
/// </summary>
internal sealed class DumpGate
{
    readonly Dictionary<string, double> _last = new(StringComparer.Ordinal);

    /// <summary>Dumps allowed so far.</summary>
    internal int Written { get; private set; }

    /// <summary>Dumps refused so far (gap or cap).</summary>
    internal int Refused { get; private set; }

    /// <summary>
    /// May a dump of <paramref name="kind"/> be written at <paramref name="now"/>? An allowed dump is counted at once.
    /// </summary>
    /// <param name="kind">What triggered it ("testlab", "check-off:SteveTheTroll", "manual:SteveTheTroll" ...).</param>
    /// <param name="now">Seconds on the recorder's clock.</param>
    /// <param name="gapSeconds">The shortest time between two dumps of this kind (0 = no gap).</param>
    /// <param name="maxDumps">The most dumps per game start.</param>
    /// <returns>Whether the dump may be written.</returns>
    internal bool Allow(string kind, double now, double gapSeconds, int maxDumps)
    {
        if (CoreLog.Due("DumpGate.Allow", 2d)) CoreLog.Every("DumpGate.Allow", 2d, $"DumpGate.Allow(kind={CoreLog.Shown(kind)}, now={CoreLog.Shown(now)}, gapSeconds={CoreLog.Shown(gapSeconds)}, maxDumps={CoreLog.Shown(maxDumps)})");
        string k = kind ?? "";
        bool tooSoon = _last.TryGetValue(k, out double last) && now - last < gapSeconds;
        bool allowed = !tooSoon && Written < maxDumps;
        if (allowed)
        {
            _last[k] = now;
            Written++;
        }
        else
        {
            Refused++;
        }

        { var returnedAt46 = allowed; if (CoreLog.Due("DumpGate.Allow:46", 2d)) CoreLog.Every("DumpGate.Allow:46", 2d, $"DumpGate.Allow: returns {CoreLog.Shown(returnedAt46)} (line 46)"); return returnedAt46; }
    }
}
