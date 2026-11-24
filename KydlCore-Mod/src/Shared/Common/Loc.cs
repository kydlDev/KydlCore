using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Jotunn.Managers;
using KydlCore.Core;

namespace KydlCore.Common;

/// <summary>
/// SHARED (KydlCore.dll). Player-facing text of a mod, English only (user, 2026-10-22: no German / other languages), registered with Jotunn. Every key starts with the mod's own prefix
/// (e.g. "epl_"), so two mods never fight over a key. The table is kept per CALLING mod (<see cref="ModContext"/>). Keep the tables in the mod's Unity UI folder (UiText.cs) when the Unity preview
/// shows them too.
/// </summary>
public static class Loc
{
    /// Once, from Plugin.Awake, before any piece / item is added. Expected outcome: the English table is in use for T() before the game runs.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Register(Dictionary<string, string> en) { return Register(ModContexts.For(Assembly.GetCallingAssembly()), en); }

    internal static bool Register(ModContext ctx, Dictionary<string, string> en)
    {
        ctx.English = en ?? new Dictionary<string, string>();
        var loc = LocalizationManager.Instance.GetLocalization();
        loc.AddJsonFile("English", ToJson(ctx.English));
        ModLog.Verbose(ctx, $"Loc: {ctx.English.Count} English text(s) registered");
        return ctx.English.Count == (en != null ? en.Count : 0);
    }

    /// Older mods still pass a German table: it is ignored (English only, user 2026-10-22). Kept only so those mods still build;
    /// drop their German table and call Register(en) when they are next changed.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Register(Dictionary<string, string> en, Dictionary<string, string> de)
    {
        var ctx = ModContexts.For(Assembly.GetCallingAssembly());
        if (de != null) ModLog.Verbose(ctx, $"Loc: a German table ({de.Count} text(s)) was passed and is ignored - English only");
        return Register(ctx, en);
    }

    static string ToJson(Dictionary<string, string> d)
    {
        if (CoreLog.On) ModLog.Verbose($"Loc.ToJson(d={CoreLog.Shown(d)})");
        var sb = new StringBuilder("{");
        bool first = true;
        foreach (var kv in d)
        {
            if (!first) sb.Append(',');
            first = false;
            sb.Append('"').Append(Esc(kv.Key)).Append("\":\"").Append(Esc(kv.Value)).Append('"');
        }
        { var returnedAt51 = sb.Append('}').ToString(); if (CoreLog.On) ModLog.Verbose($"Loc.ToJson: returns {CoreLog.Shown(returnedAt51)} (line 51)"); return returnedAt51; }
    }

    static string Esc(string s) { return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n"); }

    /// "$key" for names / descriptions that Valheim localizes itself.
    public static string Token(string key) { return $"${key}"; }

    /// Localized text for a key, {0} {1} ... replaced by args (filled in here: Localize(text, words) left the {0} in hover texts).
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string T(string key, params string[] args)
    {
        if (CoreLog.Due("Loc.T", 2d)) ModLog.Every("Loc.T", 2d, $"Loc.T(key={CoreLog.Shown(key)})");
        var ctx = ModContexts.For(Assembly.GetCallingAssembly());
        var l = Localization.instance;
        string s;
        if (l != null) s = l.Localize($"${key}");
        else if (!ctx.English.TryGetValue(key, out s)) s = key;
        if (args != null) for (int i = 0; i < args.Length; i++) s = s.Replace($"{{{i}}}", args[i]);
        { var returnedAt70 = s; if (CoreLog.Due("Loc.T:70", 2d)) ModLog.Every("Loc.T:70", 2d, $"Loc.T: returns {CoreLog.Shown(returnedAt70)} (line 70)"); return returnedAt70; }
    }
}
