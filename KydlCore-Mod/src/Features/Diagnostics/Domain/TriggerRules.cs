using System;
using System.Text;
using System.Text.RegularExpressions;
using KydlCore.Core;

namespace KydlCore.Features.Diagnostics.Domain;

/// <summary>
/// Reads the log lines that start a flight recorder dump. DrakesTestLab has no event a mod can subscribe to (TESTLAB-AUTHORING.md: it writes
/// its report and its own log lines), so a test result is read from the lab's line "Test &lt;Suite&gt;/&lt;test&gt;: Passed" or
/// "Test &lt;Suite&gt;/&lt;test&gt;: Failed - &lt;why&gt;", and the end of every test from "Test state reset.".
/// Pure C#: no Unity, no game types, no logging, so it is unit tested.
/// </summary>
internal static class TriggerRules
{
    /// <summary>The BepInEx log source name of DrakesTestLab.</summary>
    internal const string LabSource = "DrakesTestLab";

    /// <summary>The lab's line after every test.</summary>
    internal const string LabReset = "Test state reset.";

    static readonly Regex LabResult = new(@"^Test (?<test>\S.*?): (?<result>Passed|Failed)(?: - (?<why>.*))?$",
        RegexOptions.Singleline | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(200));

    /// <summary>Is this the lab's "Test X: Passed / Failed" line? Gives the test name, whether it failed and the reason the lab gave.</summary>
    internal static bool TryLabResult(string source, string text, out string test, out bool failed, out string why)
    {
        if (CoreLog.On) CoreLog.Verbose($"TriggerRules.TryLabResult(source={CoreLog.Shown(source)}, text={CoreLog.Shown(text)})");
        test = null;
        failed = false;
        why = null;
        if (source != LabSource || string.IsNullOrEmpty(text))
        {
            { var returnedAt34 = test != null; if (CoreLog.On) CoreLog.Verbose($"TriggerRules.TryLabResult: returns {CoreLog.Shown(returnedAt34)} (line 34)"); return returnedAt34; }
        }

        Match m;
        try
        {
            m = LabResult.Match(text.Trim());
        }
        catch (RegexMatchTimeoutException)
        {
            { var returnedAt44 = test != null; if (CoreLog.On) CoreLog.Verbose($"TriggerRules.TryLabResult: returns {CoreLog.Shown(returnedAt44)} (line 44)"); return returnedAt44; }
        }

        if (!m.Success)
        {
            { var returnedAt49 = test != null; if (CoreLog.On) CoreLog.Verbose($"TriggerRules.TryLabResult: returns {CoreLog.Shown(returnedAt49)} (line 49)"); return returnedAt49; }
        }

        test = m.Groups["test"].Value;
        failed = m.Groups["result"].Value == "Failed";
        why = m.Groups["why"].Success ? m.Groups["why"].Value : "";
        { var returnedAt55 = test != null; if (CoreLog.On) CoreLog.Verbose($"TriggerRules.TryLabResult: returns {CoreLog.Shown(returnedAt55)} (line 55)"); return returnedAt55; }
    }

    /// <summary>Is this the lab's line after every test ("Test state reset.")?</summary>
    internal static bool IsLabReset(string source, string text) =>
        source == LabSource && text != null && text.Trim() == LabReset;

    /// <summary>A file-name part made from a reason: letters, digits and '-' only, at most <paramref name="max"/> characters, never empty.</summary>
    internal static string Slug(string reason, int max = 40)
    {
        if (CoreLog.On) CoreLog.Every("TriggerRules.Slug", 2d, $"TriggerRules.Slug(reason={CoreLog.Shown(reason)}, max={CoreLog.Shown(max)})");
        var sb = new StringBuilder();
        bool dash = false;
        foreach (char ch in reason ?? "")
        {
            if (char.IsLetterOrDigit(ch) && ch < 128)
            {
                sb.Append(char.ToLowerInvariant(ch));
                dash = false;
            }
            else if (!dash && sb.Length > 0)
            {
                sb.Append('-');
                dash = true;
            }

            if (sb.Length >= max)
            {
                break;
            }
        }

        string slug = sb.ToString().Trim('-');
        { var returnedAt88 = slug.Length == 0 ? "dump" : slug; if (CoreLog.On) CoreLog.Every("TriggerRules.Slug:88", 2d, $"TriggerRules.Slug: returns {CoreLog.Shown(returnedAt88)} (line 88)"); return returnedAt88; }
    }

    /// <summary>The text cut to <paramref name="max"/> characters (with "..." when cut), first line only; never null.</summary>
    internal static string Short(string text, int max)
    {
        if (CoreLog.On) CoreLog.Verbose($"TriggerRules.Short(text={CoreLog.Shown(text)}, max={CoreLog.Shown(max)})");
        string first = (text ?? "").Split('\n')[0].TrimEnd('\r');
        { var returnedAt96 = first.Length <= max ? first : first.Substring(0, Math.Max(0, max)) + "..."; if (CoreLog.On) CoreLog.Verbose($"TriggerRules.Short: returns {CoreLog.Shown(returnedAt96)} (line 96)"); return returnedAt96; }
    }
}
