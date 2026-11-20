using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using KydlCore.Core;

namespace KydlCore.Features.Diagnostics.Domain;

/// <summary>
/// The flight recorder's memory (user, 2026-11-06: "track everything"): the last N samples of every tracked object, a timeline of notes
/// (state changes, decisions, test results, CHECK OFF) and of environment samples (frame rate, simulation area), and the text of a dump.
/// Everything stays in memory; nothing is written until something goes wrong (the infrastructure writes <see cref="Format"/> to a file).
/// Pure C#: no Unity, no game types, no logging, so it is unit tested. Not thread-safe: the owner locks.
/// </summary>
internal sealed class FlightBook
{
    sealed class Track
    {
        internal string Mod;
        internal string Label;
        internal FlightRing<FlightSample> Samples;
        internal double LastActivity;
        internal double GoneAt = -1;
    }

    readonly Dictionary<string, Track> _tracks = new(StringComparer.Ordinal);
    readonly FlightRing<FlightNote> _notes;
    readonly FlightRing<FlightNote> _env;

    /// <summary>Creates an empty book.</summary>
    /// <param name="samplesPerTrack">Samples kept per tracked object (the window divided by the sample interval).</param>
    /// <param name="notes">Timeline notes kept.</param>
    /// <param name="envSamples">Environment samples kept.</param>
    internal FlightBook(int samplesPerTrack, int notes, int envSamples)
    {
        if (CoreLog.On) CoreLog.Every("Track.FlightBook", 2d, $"Track.FlightBook(samplesPerTrack={samplesPerTrack}, notes={notes}, envSamples={envSamples})");
        SamplesPerTrack = Math.Max(1, samplesPerTrack);
        _notes = new FlightRing<FlightNote>(Math.Max(1, notes));
        _env = new FlightRing<FlightNote>(Math.Max(1, envSamples));
    }

    /// <summary>Samples kept per tracked object.</summary>
    internal int SamplesPerTrack { get; }

    /// <summary>How many objects are tracked (gone ones included until <see cref="Prune"/> drops them).</summary>
    internal int TrackCount => _tracks.Count;

    /// <summary>Is this key tracked?</summary>
    internal bool IsTracked(string key) => key != null && _tracks.ContainsKey(key);

    /// <summary>Starts tracking a key, or renames it and clears its "gone" mark when it is already tracked (its history is kept).</summary>
    /// <returns>Whether the key is tracked afterwards (false for a null or empty key).</returns>
    internal bool Start(string key, string mod, string label, double now)
    {
        if (CoreLog.On) CoreLog.Every("Track.Start", 2d, $"Track.Start(key={key}, mod={mod}, label={label}, now={now})");
        if (string.IsNullOrEmpty(key))
        {
            { var returnedAt59 = IsTracked(key); if (CoreLog.On) CoreLog.Every("Track.Start:59", 2d, $"Track.Start: returns {returnedAt59} (line 59)"); return returnedAt59; }
        }

        if (!_tracks.TryGetValue(key, out Track t))
        {
            t = new Track { Samples = new FlightRing<FlightSample>(SamplesPerTrack) };
            _tracks[key] = t;
        }

        t.Mod = mod ?? "";
        t.Label = string.IsNullOrEmpty(label) ? key : label;
        t.LastActivity = now;
        t.GoneAt = -1;
        { var returnedAt72 = IsTracked(key); if (CoreLog.On) CoreLog.Every("Track.Start:72", 2d, $"Track.Start: returns {returnedAt72} (line 72)"); return returnedAt72; }
    }

    /// <summary>Stops tracking a key and forgets its samples.</summary>
    /// <returns>Whether the key was tracked before.</returns>
    internal bool Stop(string key) => key != null && _tracks.Remove(key);

    /// <summary>Adds a sample to a tracked key (a sample also clears the key's "gone" mark: the object came back).</summary>
    /// <returns>Whether the sample was stored (false for a key that is not tracked).</returns>
    internal bool Add(string key, FlightSample sample)
    {
        if (CoreLog.On) CoreLog.Every("Track.Add", 2d, $"Track.Add(key={key}, sample={sample})");
        if (key == null || !_tracks.TryGetValue(key, out Track t))
        {
            { var returnedAt86 = IsTracked(key); if (CoreLog.On) CoreLog.Every("Track.Add:86", 2d, $"Track.Add: returns {returnedAt86} (line 86)"); return returnedAt86; }
        }

        t.Samples.Add(sample);
        t.LastActivity = sample.Time;
        t.GoneAt = -1;
        { var returnedAt92 = t.Samples.Count > 0; if (CoreLog.On) CoreLog.Every("Track.Add:92", 2d, $"Track.Add: returns {returnedAt92} (line 92)"); return returnedAt92; }
    }

    /// <summary>Marks a tracked key as gone (its object and its saved data no longer exist); the history stays until <see cref="Prune"/>.</summary>
    /// <returns>Whether the key is marked gone afterwards.</returns>
    internal bool MarkGone(string key, double now)
    {
        if (CoreLog.On) CoreLog.Every("Track.MarkGone", 2d, $"Track.MarkGone(key={key}, now={now})");
        if (key == null || !_tracks.TryGetValue(key, out Track t))
        {
            { var returnedAt102 = IsTracked(key); if (CoreLog.On) CoreLog.Every("Track.MarkGone:102", 2d, $"Track.MarkGone: returns {returnedAt102} (line 102)"); return returnedAt102; }
        }

        if (t.GoneAt < 0)
        {
            t.GoneAt = now;
        }

        { var returnedAt110 = t.GoneAt >= 0; if (CoreLog.On) CoreLog.Every("Track.MarkGone:110", 2d, $"Track.MarkGone: returns {returnedAt110} (line 110)"); return returnedAt110; }
    }

    /// <summary>Drops the keys that have been gone for longer than <paramref name="goneSeconds"/>.</summary>
    /// <returns>How many were dropped.</returns>
    internal int Prune(double now, double goneSeconds)
    {
        if (CoreLog.On) CoreLog.Every("Track.Prune", 2d, $"Track.Prune(now={now}, goneSeconds={goneSeconds})");
        var old = _tracks.Where(kv => kv.Value.GoneAt >= 0 && now - kv.Value.GoneAt > goneSeconds).Select(kv => kv.Key).ToList();
        foreach (string key in old)
        {
            _tracks.Remove(key);
        }

        { var returnedAt124 = old.Count; if (CoreLog.On) CoreLog.Every("Track.Prune:124", 2d, $"Track.Prune: returns {returnedAt124} (line 124)"); return returnedAt124; }
    }

    /// <summary>Adds a timeline note. Returns how many notes are kept now.</summary>
    internal int Note(FlightNote note) => _notes.Add(note);

    /// <summary>Adds an environment sample. Returns how many are kept now.</summary>
    internal int Env(FlightNote sample) => _env.Add(sample);

    /// <summary>
    /// The text of a dump: the reason, the environment, the notes and every tracked object's samples of the last
    /// <paramref name="windowSeconds"/>, newest activity first, at most <paramref name="maxTracks"/> objects. Times are wall-clock times
    /// (a sample's time is placed relative to <paramref name="wallNow"/>), so they line up with the BepInEx log.
    /// </summary>
    internal string Format(string reason, double now, double windowSeconds, DateTime wallNow, int maxTracks)
    {
        if (CoreLog.On) CoreLog.Every("Track.Format", 2d, $"Track.Format(reason={reason}, now={now}, windowSeconds={windowSeconds}, wallNow={wallNow}, maxTracks={maxTracks})");
        double from = now - Math.Max(1, windowSeconds);
        var sb = new StringBuilder();
        sb.Append("FLIGHT RECORDER DUMP - ").AppendLine(reason ?? "(no reason)");
        sb.Append("written ").Append(wallNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture))
          .Append(", window ").Append(Num(windowSeconds, "0")).Append(" s (from ").Append(Clock(from, now, wallNow)).Append("), ")
          .Append(_tracks.Count).AppendLine(" tracked object(s)");
        sb.AppendLine();

        sb.AppendLine("== environment");
        foreach (FlightNote e in _env.Items(x => x.Time >= from))
        {
            sb.Append(Clock(e.Time, now, wallNow)).Append("  ").AppendLine(e.Text);
        }

        sb.AppendLine();
        sb.AppendLine("== timeline (state changes, decisions, test results, CHECK OFF, errors)");
        foreach (FlightNote n in _notes.Items(x => x.Time >= from))
        {
            sb.Append(Clock(n.Time, now, wallNow)).Append("  [").Append(n.Source).Append("] ").AppendLine(n.Text);
        }

        foreach (var kv in _tracks.OrderByDescending(kv => kv.Value.LastActivity).ThenBy(kv => kv.Key, StringComparer.Ordinal).Take(Math.Max(1, maxTracks)))
        {
            AppendTrack(sb, kv.Key, kv.Value, from, now, wallNow);
        }

        if (_tracks.Count > maxTracks)
        {
            sb.AppendLine().Append("(").Append(_tracks.Count - maxTracks).AppendLine(" more tracked object(s) not written)");
        }

        { var returnedAt172 = sb.ToString(); if (CoreLog.On) CoreLog.Every("Track.Format:172", 2d, $"Track.Format: returns {returnedAt172} (line 172)"); return returnedAt172; }
    }

    static void AppendTrack(StringBuilder sb, string key, Track t, double from, double now, DateTime wallNow)
    {
        if (CoreLog.On) CoreLog.Every("Track.AppendTrack", 2d, $"Track.AppendTrack(sb={sb}, key={key}, t={t}, from={from}, now={now}, wallNow={wallNow})");
        sb.AppendLine();
        sb.Append("== ").Append(t.Mod).Append(" / ").Append(t.Label).Append(" (").Append(key).Append(')');
        if (t.GoneAt >= 0)
        {
            sb.Append("  GONE since ").Append(Clock(t.GoneAt, now, wallNow));
        }

        sb.AppendLine();
        sb.AppendLine("time              x         y         z     m/s  facts (\"same\" = unchanged)");
        string previous = null;
        foreach (FlightSample s in t.Samples.Items(x => x.Time >= from))
        {
            sb.Append(Clock(s.Time, now, wallNow)).Append(' ')
              .Append(Num(s.X, "0.0").PadLeft(9)).Append(' ').Append(Num(s.Y, "0.0").PadLeft(9)).Append(' ').Append(Num(s.Z, "0.0").PadLeft(9)).Append(' ')
              .Append(Num(s.Speed, "0.00").PadLeft(7)).Append("  ").AppendLine(s.Facts == previous ? "same" : s.Facts);
            previous = s.Facts;
        }
    }

    static string Clock(double t, double now, DateTime wallNow) =>
        wallNow.AddSeconds(t - now).ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);

    static string Num(double v, string format) => v.ToString(format, CultureInfo.InvariantCulture);
}
