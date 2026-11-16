using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BepInEx.Logging;
using KydlCore.Core;
using KydlCore.Features.Diagnostics.Domain;
using KydlCore.Features.Items.Domain;

namespace KydlCore.Tests.Unit;

/// <summary>
/// Pins down what Stryker found open on 2026-11-16 in four files that were below 80 % (every file must reach 80 %): the exact texts of
/// ContributionBook's refusals, the whole text of a flight recorder dump and its edges, and ModLog's check / note events and throttles.
/// </summary>
[TestFixture]
public class MutationGapTests
{
    // ---- ContributionBook ----------------------------------------------------------------------------------------------------
    [Test]
    public void ContributionRecord_NullParts_AreEmptyTexts()
    {
        var r = new ContributionRecord(null, null, null);
        Assert.That(new[] { r.Id, r.ModGuid, r.Prefab }, Is.EqualTo(new[] { "", "", "" }));
    }

    [Test]
    public void ContributionBook_Refusals_NameTheIdThePrefabAndTheHolder()
    {
        var b = new ContributionBook();
        b.Add(new ContributionRecord("steve:slippers", "com.kydldev.Steve", "Slippers"));

        Assert.Multiple(() =>
        {
            Assert.That(b.Add(new ContributionRecord("steve:slippers", "com.kydldev.Other", "Other")).Reason,
                Is.EqualTo("the id 'steve:slippers' is already registered by com.kydldev.Steve"));
            Assert.That(b.Add(new ContributionRecord("other:slippers", "com.kydldev.Other", "Slippers")).Reason,
                Is.EqualTo("the prefab 'Slippers' is already contributed as 'steve:slippers'"));
        });
    }

    [Test]
    public void ContributionBook_Find_AnUnknownId_HasNoOwnerAndNoPrefab()
    {
        ContributionRecord r = new ContributionBook().Find("nobody:nothing");
        Assert.That(new[] { r.Id, r.ModGuid, r.Prefab }, Is.EqualTo(new[] { "nobody:nothing", "", "" }));
    }

    // ---- FlightBook ----------------------------------------------------------------------------------------------------------
    static readonly DateTime Wall = new DateTime(2026, 11, 5, 11, 32, 21, 500);

    [Test]
    public void FlightBook_Format_TheWholeDump_WithEverythingAtTheEdgeOfTheWindow()
    {
        var book = new FlightBook(10, 50, 10);
        book.Env(new FlightNote(39.9, "env", "too old"));
        book.Env(new FlightNote(40, "env", "fps 60"));
        book.Env(new FlightNote(50, "env", "fps 30"));
        book.Note(new FlightNote(39.5, "A", "too old"));
        book.Note(new FlightNote(40, "TestLab", "at the edge"));
        book.Note(new FlightNote(60, "Mod", "second"));
        book.Start("b", "M", "", 50);
        book.Add("b", new FlightSample(39, 9, 9, 9, 9, "too old"));
        Assert.That(book.Add("b", new FlightSample(40, 1.5, -2.2, 3, 0.5, "f1")), Is.True);
        book.Add("b", new FlightSample(41, 10, 20, 30, 12.34, "f1"));
        book.Start("a", null, "Alpha", 41);
        Assert.That(book.MarkGone("a", 0), Is.True, "gone at time 0 is gone");
        Assert.That(book.MarkGone("a", 5), Is.True, "a second mark keeps the first time");

        string dump = book.Format("why", 100, 60, Wall, 2);

        string[] lines =
        {
            "FLIGHT RECORDER DUMP - why",
            "written 2026-11-05 11:32:21.500, window 60 s (from 11:31:21.500), 2 tracked object(s)",
            "",
            "== environment",
            "11:31:21.500  fps 60",
            "11:31:31.500  fps 30",
            "",
            "== timeline (state changes, decisions, test results, CHECK OFF, errors)",
            "11:31:21.500  [TestLab] at the edge",
            "11:31:41.500  [Mod] second",
            "",
            "==  / Alpha (a)  GONE since 11:30:41.500",
            "time              x         y         z     m/s  facts (\"same\" = unchanged)",
            "",
            "== M / b (b)",
            "time              x         y         z     m/s  facts (\"same\" = unchanged)",
            "11:31:21.500       1.5      -2.2       3.0    0.50  f1",
            "11:31:22.500      10.0      20.0      30.0   12.34  same",
        };
        Assert.That(dump, Is.EqualTo(string.Join(Environment.NewLine, lines) + Environment.NewLine));
    }

    [Test]
    public void FlightBook_Format_NoReason_SaysSo()
    {
        Assert.That(new FlightBook(1, 1, 1).Format(null, 10, 60, Wall, 5), Does.StartWith("FLIGHT RECORDER DUMP - (no reason)" + Environment.NewLine));
    }

    [Test]
    public void FlightBook_Prune_GoneAtTimeZeroCounts_AndExactlyTheLimitIsKept()
    {
        var book = new FlightBook(1, 1, 1);
        book.Start("a", "M", "a", 0);
        book.MarkGone("a", 0);

        Assert.Multiple(() =>
        {
            Assert.That(book.Prune(100, 100), Is.EqualTo(0), "gone for exactly the limit: kept");
            Assert.That(book.Prune(100, 50), Is.EqualTo(1), "gone since time 0 for longer than the limit: dropped");
            Assert.That(book.TrackCount, Is.EqualTo(0));
        });
    }

    // ---- ModLog (reached in the built DLL by reflection, like ModLogTests) ------------------------------------------------------
    const BindingFlags Any = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static readonly Type Log = ModAssembly.Type("KydlCore.Common.ModLog");
    static readonly Type Contexts = ModAssembly.Type("KydlCore.Common.ModContexts");
    static readonly Type Context = ModAssembly.Type("KydlCore.Common.ModContext");

    sealed class CaptureListener : ILogListener
    {
        public readonly List<string> Lines = new List<string>();
        public readonly List<LogLevel> Levels = new List<LogLevel>();
        public void LogEvent(object sender, LogEventArgs e) { Lines.Add(Convert.ToString(e.Data)); Levels.Add(e.Level); }
        public void Dispose() { }
    }

    CaptureListener _listener;
    ManualLogSource _source;
    bool _debug;
    object _ctx;
    readonly List<string> _notes = new List<string>();
    readonly List<string> _checkOffs = new List<string>();
    Delegate _noteHandler, _checkHandler;

    [SetUp]
    public void SetUp()
    {
        ModAssembly.Call("KydlCore.Common.ModContexts", "ResetAllForTests");
        _debug = true;
        _listener = new CaptureListener();
        _source = new ManualLogSource("KydlCoreGapTest");
        BepInEx.Logging.Logger.Listeners.Add(_listener);
        BepInEx.Logging.Logger.Sources.Add(_source);
        ModAssembly.Call("KydlCore.Common.ModLog", "Init", _source, "KydlCore", (Func<bool>)(() => _debug));
        _ctx = Contexts.GetMethod("AnyInitialisedForTests", Any).Invoke(null, null);
        _notes.Clear();
        _checkOffs.Clear();
        _noteHandler = Listen("NoteWritten", _notes);
        _checkHandler = Listen("CheckOffWritten", _checkOffs);
    }

    [TearDown]
    public void TearDown()
    {
        Log.GetEvent("NoteWritten", Any).GetRemoveMethod(true).Invoke(null, new object[] { _noteHandler });
        Log.GetEvent("CheckOffWritten", Any).GetRemoveMethod(true).Invoke(null, new object[] { _checkHandler });
        BepInEx.Logging.Logger.Sources.Remove(_source);
        BepInEx.Logging.Logger.Listeners.Remove(_listener);
        _source.Dispose();
        _listener.Dispose();
        ModAssembly.Call("KydlCore.Common.ModContexts", "ResetAllForTests");
    }

    // Subscribes to an internal event Action<ModContext, string>: the texts are collected.
    static Delegate Listen(string eventName, List<string> into)
    {
        EventInfo ev = Log.GetEvent(eventName, Any);
        ParameterExpression c = Expression.Parameter(Context, "c"), text = Expression.Parameter(typeof(string), "text");
        Delegate handler = Expression.Lambda(ev.EventHandlerType, Expression.Call(Expression.Constant(into), typeof(List<string>).GetMethod("Add"), text), c, text).Compile();
        ev.GetAddMethod(true).Invoke(null, new object[] { handler });
        return handler;
    }

    // An internal ModLog method that takes the mod's context first.
    object OnBehalf(string name, params object[] args)
    {
        var all = new List<object> { _ctx };
        all.AddRange(args);
        MethodInfo m = Log.GetMethods(Any).Single(x => x.Name == name && x.GetParameters().Length == all.Count && x.GetParameters()[0].ParameterType == Context);
        return m.Invoke(null, all.ToArray());
    }

    T Field<T>(string name) { return (T)Context.GetField(name, Any).GetValue(_ctx); }

    List<string> Written() { return _listener.Lines.Skip(1).Select(l => l.Substring(l.IndexOf(" - ", StringComparison.Ordinal) + 3)).ToList(); }       // after the session marker, without the time

    [Test]
    public void OnBehalfOfAMod_Error_IsWrittenAsAnError()
    {
        OnBehalf("Error", "boom");
        Assert.That(Written(), Is.EqualTo(new[] { "boom" }));
        Assert.That(_listener.Levels.Last(), Is.EqualTo(LogLevel.Error));
    }

    [Test]
    public void OnBehalfOfAMod_VerboseWarn_OnlyWithTheDebugSwitchOn()
    {
        _debug = false;
        OnBehalf("VerboseWarn", "quiet");
        Assert.That(Written(), Is.Empty);

        _debug = true;
        OnBehalf("VerboseWarn", "loud");
        Assert.That(Written(), Is.EqualTo(new[] { "loud" }));
        Assert.That(_listener.Levels.Last(), Is.EqualTo(LogLevel.Warning));
    }

    [Test]
    public void DebugOn_FollowsTheModsSwitch()
    {
        PropertyInfo on = Log.GetProperty("DebugOn", Any);
        _debug = true;
        Assert.That((bool)on.GetValue(null), Is.True);
        _debug = false;
        Assert.That((bool)on.GetValue(null), Is.False);
    }

    [Test]
    public void Check_Off_TellsTheListeners_WithTheSameTextAsTheWarning()
    {
        ModAssembly.Call("KydlCore.Common.ModLog", "Check", "coil", 0.5, 0.0, 0.01, "m");
        ModAssembly.Call("KydlCore.Common.ModLog", "CheckRange", "scale", 1.5, 0.9, 1.1, "");

        Assert.That(_checkOffs, Is.EqualTo(new[]
        {
            "coil = 0.5 m (expected 0 +- 0.01 m), off by 0.5 m",
            "scale = 1.5 (expected 0.9 .. 1.1)",
        }));
    }

    [Test]
    public void Check_Ok_ARecentOneIsCounted_NotWritten()
    {
        var seen = Field<Dictionary<string, KeyValuePair<long, int>>>("OkSeen");
        long aSecondAgo = System.Diagnostics.Stopwatch.GetTimestamp() - System.Diagnostics.Stopwatch.Frequency;
        seen["angle"] = new KeyValuePair<long, int>(aSecondAgo, 2);

        ModAssembly.Call("KydlCore.Common.ModLog", "Check", "angle", 1.0, 1.0, 0.5, "deg");

        Assert.Multiple(() =>
        {
            Assert.That(Written(), Is.Empty, "one ok line per check name every 30 s");
            Assert.That(seen["angle"], Is.EqualTo(new KeyValuePair<long, int>(aSecondAgo, 3)), "counted, the time of the written line is kept");
        });
    }

    [Test]
    public void Check_Ok_AfterThirtySeconds_IsWrittenAgain_WithHowManyWereLeftOut()
    {
        var seen = Field<Dictionary<string, KeyValuePair<long, int>>>("OkSeen");
        long longAgo = System.Diagnostics.Stopwatch.GetTimestamp() - (31 * System.Diagnostics.Stopwatch.Frequency);
        seen["angle"] = new KeyValuePair<long, int>(longAgo, 3);

        ModAssembly.Call("KydlCore.Common.ModLog", "Check", "angle", 1.0, 1.0, 0.5, "deg");

        Assert.Multiple(() =>
        {
            Assert.That(Written(), Is.EqualTo(new[] { "CHECK ok: angle = 1 deg (expected 1 +- 0.5 deg) [3 more of this check ok in the last 30 s not written]" }));
            Assert.That(seen["angle"].Value, Is.EqualTo(0), "the count starts again");
            Assert.That(seen["angle"].Key, Is.GreaterThan(longAgo));
        });
    }

    [Test]
    public void Check_Ok_TheFirstOne_HasNoLeftOutNote()
    {
        ModAssembly.Call("KydlCore.Common.ModLog", "Check", "fresh", 1.0, 1.0, 0.5, "deg");
        Assert.That(Written(), Is.EqualTo(new[] { "CHECK ok: fresh = 1 deg (expected 1 +- 0.5 deg)" }));
    }

    [TestCase(500, 501)]
    [TestCase(501, 1)]
    public void Check_Ok_RemembersAtMostFiveHundredCheckNames(int remembered, int afterOneMore)
    {
        var seen = Field<Dictionary<string, KeyValuePair<long, int>>>("OkSeen");
        for (int i = 0; i < remembered; i++) seen["old " + i] = new KeyValuePair<long, int>(0, 0);

        ModAssembly.Call("KydlCore.Common.ModLog", "Check", "one more", 1.0, 1.0, 0.5, "");

        Assert.That(seen.Count, Is.EqualTo(afterOneMore));
    }

    [Test]
    public void ResetRepeats_ForgetsTheLinesSeen()
    {
        OnBehalf("Info", "same");
        OnBehalf("Info", "same");
        Assert.That(Written(), Has.Count.EqualTo(1), "the repeat is filtered");

        Assert.That((bool)OnBehalf("ResetRepeats"), Is.True);
        OnBehalf("Info", "same");
        Assert.That(Written(), Has.Count.EqualTo(2));
    }

    [Test]
    public void State_Decision_Every_WithNothingGiven_WritePlaceholders_AndTellTheListeners()
    {
        ModAssembly.Call("KydlCore.Common.ModLog", "State", null, null, null, null);
        ModAssembly.Call("KydlCore.Common.ModLog", "Decision", null, "left", "");
        ModAssembly.Call("KydlCore.Common.ModLog", "Every", null, 0.0, null);

        string[] expected = { "STATE ?: - -> -", "DECISION ? -> left", "STATUS ?: " };
        Assert.Multiple(() =>
        {
            Assert.That(Written(), Is.EqualTo(expected));
            Assert.That(_notes, Is.EqualTo(expected), "the flight recorder gets the same lines");
        });
    }

    [Test]
    public void Notes_ReachTheListeners_AlsoWithTheDebugLogOff()
    {
        _debug = false;
        ModAssembly.Call("KydlCore.Common.ModLog", "State", "guard", "idle", "fighting", "why");
        Assert.That(_notes, Is.EqualTo(new[] { "STATE guard: idle -> fighting (why)" }));
    }

    [Test]
    public void IsDue_AKeyNeverWritten_IsDue_HoweverLongTheInterval()
    {
        Assert.That((bool)ModAssembly.Call("KydlCore.Common.ModLog", "IsDue", "never written", 1e12), Is.True);
    }

    [Test]
    public void Every_NoKey_SharesTheSlotOfTheEmptyKey()
    {
        Assert.That((bool)ModAssembly.Call("KydlCore.Common.ModLog", "Every", null, 1e12, "a"), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That((bool)ModAssembly.Call("KydlCore.Common.ModLog", "Every", "", 1e12, "b"), Is.False);
            Assert.That((bool)ModAssembly.Call("KydlCore.Common.ModLog", "IsDue", null, 1e12), Is.False);
        });
    }

    [Test]
    public void Every_AndIsDue_MeasureTheTimeSinceTheLastLine()
    {
        // a last line "in the future" can never be due: only "now minus last" says so ("now plus last" would)
        var last = Field<Dictionary<string, double>>("EveryLast");
        last["chase"] = 1e9;

        Assert.Multiple(() =>
        {
            Assert.That((bool)ModAssembly.Call("KydlCore.Common.ModLog", "IsDue", "chase", 60.0), Is.False);
            Assert.That((bool)ModAssembly.Call("KydlCore.Common.ModLog", "Every", "chase", 60.0, "a"), Is.False);
            Assert.That(last["chase"], Is.EqualTo(1e9), "a line that is not due does not move the slot");
        });
    }

    [TestCase(4000, 4001)]
    [TestCase(4001, 1)]
    public void Every_RemembersAtMostFourThousandKeys(int remembered, int afterOneMore)
    {
        var last = Field<Dictionary<string, double>>("EveryLast");
        for (int i = 0; i < remembered; i++) last["old " + i] = 0;

        ModAssembly.Call("KydlCore.Common.ModLog", "Every", "one more", 60.0, "a");

        Assert.That(last.Count, Is.EqualTo(afterOneMore));
    }
}
