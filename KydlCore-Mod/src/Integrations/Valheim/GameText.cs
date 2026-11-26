using System;
using System.Collections.Generic;
using System.Reflection;
using KydlCore.Common;
using KydlCore.Core;

namespace KydlCore.Integrations.Valheim;

/// <summary>
/// Puts the mods' English texts (Loc.Register) into the game's own Localization (KydlCore 1.2.0, replaces Jotunn's LocalizationManager, D-104).
/// Localization.AddWord and its result cache are private (assembly_guiutils, decompiled 2026-11-26): reached by reflection once. The game clears its
/// table on every SetupLanguage (startup, language change), so the patch on SetupLanguage adds every table again (English only, rule 15).
/// </summary>
internal static class GameText
{
    const BindingFlags Inst = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static MethodInfo _addWord;
    static FieldInfo _cache;

    /// <summary>Adds every registered table to <paramref name="localization"/>. Expected outcome: the number of words added.</summary>
    internal static int AddAll(Localization localization)
    {
        int added = 0;
        var tables = ModContexts.EnglishTables();
        foreach (var t in tables) added += Add(localization, t.Value);
        ModLog.Verbose($"GameText: {added} word(s) of {tables.Count} mod(s) added to the game's localization ({(localization == null ? "<none>" : localization.GetSelectedLanguage())})");
        return added;
    }

    /// <summary>Adds one mod's table when the game's Localization already exists (a mod that registers after the game set its language). Expected outcome: the number of words added (0 before the game made its Localization).</summary>
    internal static int AddIfLoaded(Dictionary<string, string> table)
    {
        FieldInfo inst = typeof(Localization).GetField("m_instance", BindingFlags.Static | BindingFlags.NonPublic);
        var loc = inst == null ? null : inst.GetValue(null) as Localization;
        int added = loc == null ? 0 : Add(loc, table);
        ModLog.Verbose($"GameText.AddIfLoaded: localization {(loc == null ? "not made yet (the SetupLanguage patch adds the table later)" : "exists")} - added={added}, table={(table == null ? 0 : table.Count)}");
        return added;
    }

    static int Add(Localization localization, Dictionary<string, string> table)
    {
        int added = 0;
        if (localization == null || table == null) { ModLog.Verbose($"GameText.Add: nothing added - localization={(localization != null)}, table={(table == null ? "<none>" : table.Count.ToString())}"); return added; }
        _addWord ??= typeof(Localization).GetMethod("AddWord", Inst, null, new[] { typeof(string), typeof(string) }, null);
        _cache ??= typeof(Localization).GetField("m_cache", Inst);
        if (_addWord == null) { ModLog.Warn("GameText: Localization.AddWord(string, string) not found - mod texts show as [key]"); return added; }
        foreach (var kv in table)
        {
            try { _addWord.Invoke(localization, new object[] { kv.Key, kv.Value ?? "" }); added++; }
            catch (TargetInvocationException e) { ModLog.Warn($"GameText: word '{kv.Key}' not added - {e.InnerException?.GetType().FullName}: {e.InnerException?.Message}; the text shows as [{kv.Key}]"); }
        }
        EvictCache(localization);
        if (CoreLog.On) ModLog.Verbose($"GameText.Add: {added} of {table.Count} word(s) added");
        return added;
    }

    /// The game caches Localize results (LRU, 100 entries): an old "[key]" result would stay after the word is added.
    static bool EvictCache(Localization localization)
    {
        object cache = _cache == null ? null : _cache.GetValue(localization);
        MethodInfo evict = cache == null ? null : cache.GetType().GetMethod("EvictAll", Inst, null, Type.EmptyTypes, null);
        if (evict != null) evict.Invoke(cache, null);
        bool evicted = evict != null;
        if (CoreLog.On) ModLog.Verbose($"GameText.EvictCache: cache={(cache == null ? "<none>" : cache.GetType().Name)}, evicted={evicted}");
        return evicted;
    }
}
