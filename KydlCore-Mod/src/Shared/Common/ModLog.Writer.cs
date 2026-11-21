using System;
using System.Collections.Generic;
using System.Threading;
using BepInEx.Logging;

namespace KydlCore.Common;

/// <summary>
/// The background log writer (GunPowdered D-147 / D-152, user 2026-11-21: stutter, "can we move the logging on another thread"; PROJECT_RULES 38).
/// BepInEx writes every line synchronously to all its listeners (the log file, the console window, GameProbe, the flight recorder) on the thread that
/// logs - the game's main thread. Once <see cref="StartBackgroundWriter"/> ran (KydlCore's Plugin.Awake), Info / Debug lines are only queued there and
/// one worker thread hands them to BepInEx in order. Warnings and errors stay synchronous: the queue is written out first, then the line, on the calling
/// thread - so the order is kept and nothing important is lost if the game crashes. The text (with its time stamp) is made when the line is logged.
/// Before the writer starts (and in the unit tests) every line is written at once, as before.
/// </summary>
public static partial class ModLog
{
    struct PendingLine
    {
        internal ManualLogSource Source;
        internal LogLevel Level;
        internal string Text;
    }

    const int MaxPending = 20000;                     // a flood beyond this is written at once (back-pressure) instead of growing without end
    static readonly Queue<PendingLine> Pending = new Queue<PendingLine>();
    static readonly object PendingLock = new object(); // guards the queue
    static readonly object WriteLock = new object();   // one writer at a time: the order of the lines is kept
    static readonly AutoResetEvent Wake = new AutoResetEvent(false);
    static Thread _writer;
    static volatile bool _background;

    /// <summary>Whether Info / Debug lines go through the background writer.</summary>
    public static bool BackgroundWriting => _background;

    /// <summary>Starts the background writer (once; KydlCore's Plugin.Awake). Expected outcome: true when it runs.</summary>
    public static bool StartBackgroundWriter()
    {
        lock (PendingLock)
        {
            if (_writer != null) return _background;
            _writer = new Thread(WriterLoop) { IsBackground = true, Name = "KydlCore log writer", Priority = ThreadPriority.BelowNormal };
            _background = true;
            _writer.Start();
        }
        return _background;
    }

    /// <summary>Writes every queued line now, on the calling thread (game quit, a lab step that reads the log). Expected outcome: how many were written.</summary>
    public static int FlushPending()
    {
        lock (WriteLock) return Drain();
    }

    // Write's last step: queue an everyday line, write a warning / error (after the queue) at once.
    static void Emit(ManualLogSource source, LogLevel level, string text)
    {
        bool urgent = (level & (LogLevel.Warning | LogLevel.Error | LogLevel.Fatal)) != 0;
        if (_background && !urgent)
        {
            bool queued;
            lock (PendingLock)
            {
                queued = Pending.Count < MaxPending;
                if (queued) Pending.Enqueue(new PendingLine { Source = source, Level = level, Text = text });
            }
            if (queued) { Wake.Set(); return; }
        }
        lock (WriteLock)
        {
            Drain();
            source.Log(level, text);
        }
    }

    // Hands every queued line to BepInEx, oldest first. Called with WriteLock held. A failing line is skipped: logging never takes the game down.
    static int Drain()
    {
        int written = 0;
        while (true)
        {
            PendingLine p;
            lock (PendingLock)
            {
                if (Pending.Count == 0) return written;
                p = Pending.Dequeue();
            }
            try { p.Source.Log(p.Level, p.Text); written++; }
            catch (ObjectDisposedException) { }      // the BepInEx log is closed while the game shuts down
            catch (Exception) { }                    // last resort
        }
    }

    static void WriterLoop()
    {
        while (true)
        {
            Wake.WaitOne(250);
            try { lock (WriteLock) Drain(); }
            catch (Exception) { }                    // the writer thread must never die
        }
    }
}
