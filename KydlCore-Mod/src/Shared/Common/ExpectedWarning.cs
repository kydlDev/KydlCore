using System;
using System.Collections.Generic;
using System.Threading;

namespace KydlCore.Common;

/// <summary>
/// Test support (KydlCore T-EXPECTWARN, GunPowdered T-023): a scope in which a warning that a test provokes ON PURPOSE (a broken listener, a blank
/// window id) is written as an Info line "expected warning (test): ..." instead of a Warning, and counted, so the console stays clean and the
/// test can assert that the warning really happened. Open one with <see cref="ModLog.ExpectWarning"/>; dispose it (using) when the test is done.
/// A warning that does not contain the text stays a Warning. An empty text matches nothing.
/// </summary>
public sealed class ExpectedWarning : IDisposable
{
    static readonly object Gate = new object();
    static readonly List<ExpectedWarning> Active = new List<ExpectedWarning>();

    int _count;

    internal ExpectedWarning(string text)
    {
        Text = text ?? "";
        ModLog.Verbose($"ExpectedWarning: scope created for a warning containing '{Text}'");
    }

    /// <summary>The text a warning must contain to be taken as expected.</summary>
    public string Text { get; }

    /// <summary>How many warnings this scope has taken so far.</summary>
    public int Count { get { return Volatile.Read(ref _count); } }

    /// Expected outcome: the new scope is active (listed) and counts matching warnings from now on.
    internal static ExpectedWarning Open(string text)
    {
        var scope = new ExpectedWarning(text);
        lock (Gate) Active.Add(scope);
        ModLog.Verbose("ExpectedWarning: scope opened");
        return scope;
    }

    /// Expected outcome: returns whether an active scope took the warning (and counted it).
    internal static bool Absorb(string message)
    {
        bool taken = false;
        lock (Gate)
        {
            foreach (var scope in Active)
            {
                if (scope.Text.Length == 0 || message == null || message.IndexOf(scope.Text, StringComparison.Ordinal) < 0) continue;
                Interlocked.Increment(ref scope._count);
                taken = true;
                break;
            }
        }
        // Info only (never a warning), so this cannot loop back into Absorb.
        if (taken) ModLog.Verbose("ExpectedWarning: a warning was taken as expected");
        else ModLog.Verbose("ExpectedWarning: the warning matches no open scope, it stays a warning");
        return taken;
    }

    /// <summary>Ends the scope: later warnings are warnings again. Calling it twice is fine.</summary>
    public void Dispose()
    {
        bool removed;
        lock (Gate) removed = Active.Remove(this);
        ModLog.Verbose(removed ? "ExpectedWarning: scope closed" : "ExpectedWarning: scope was already closed");
    }
}
