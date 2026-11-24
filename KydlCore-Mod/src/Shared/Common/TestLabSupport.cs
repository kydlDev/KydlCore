using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using KydlCore.Core;

namespace KydlCore.Common;

/// <summary>Helpers the per-mod Testing/TestHooks.cs files share (KydlCore D-084).</summary>
public static class TestLabSupport
{
    /// <summary>
    /// Runs every static, parameterless, string-returning `Startup_*` / `World_*` method of the mod's InGameTests class (the ones Kydl.GameProbe runs) and sums the verdicts.
    /// Expected outcome: "PASS: n passed, m skipped" or "FAIL: k of n failed - Name: detail; ..." (a suite asserts `notContains: "FAIL"`). Each verdict is also written to the log.
    /// </summary>
    public static string RunInGameTests(Type tests, bool includeStartup = true)
    {
        if (tests == null) { if (CoreLog.On) ModLog.Verbose("TestLabSupport.RunInGameTests: returns \"FAIL: no InGameTests class\" (line 29)"); return "FAIL: no InGameTests class"; }
        var ctx = ModContexts.For(tests);
        int pass = 0, skip = 0, fail = 0;
        var bad = new StringBuilder();
        foreach (var m in tests.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            bool startup = m.Name.StartsWith("Startup_", StringComparison.Ordinal), world = m.Name.StartsWith("World_", StringComparison.Ordinal);
            if (!(startup && includeStartup) && !world) continue;
            if (m.GetParameters().Length != 0 || m.ReturnType != typeof(string)) continue;
            string r;
            try { r = (string)m.Invoke(null, null); }
            catch (Exception e) { r = "FAIL: exception " + (e.InnerException ?? e).GetType().Name + ": " + (e.InnerException ?? e).Message; }
            r = r ?? "FAIL: null";
            if (r.StartsWith("PASS", StringComparison.Ordinal)) pass++;
            else if (r.StartsWith("SKIP", StringComparison.Ordinal)) skip++;
            else { fail++; if (bad.Length < 600) bad.Append(bad.Length > 0 ? "; " : "").Append(m.Name).Append(": ").Append(r.Length > 160 ? r.Substring(0, 160) : r); }
            ModLog.Info(ctx, "TestLab self-test " + m.Name + " -> " + (r.Length > 200 ? r.Substring(0, 200) : r));
        }
        if (pass + skip + fail == 0) { var returnedAt47 = "FAIL: " + tests.Name + " has no Startup_* / World_* methods"; if (CoreLog.On) ModLog.Verbose($"TestLabSupport.RunInGameTests: returns {CoreLog.Shown(returnedAt47)} (line 47)"); return returnedAt47; }
        { var returnedAt48 = fail == 0 ? "PASS: " + pass + " passed, " + skip + " skipped" : "FAIL: " + fail + " of " + (pass + skip + fail) + " failed - " + bad; if (CoreLog.On) ModLog.Verbose($"TestLabSupport.RunInGameTests: returns {CoreLog.Shown(returnedAt48)} (line 48)"); return returnedAt48; }
    }
}
