using System;
using System.IO;
using System.Linq;
using KydlCore.Common;
using KydlCore.Features.Diagnostics.Contracts;
using KydlCore.Features.Hub.Contracts;
using KydlCore.Features.Items.Contracts;
using UnityEngine.UIElements;

namespace KydlCore.Testing.TestHooks;

/// <summary>
/// KydlCore D-084: hooks for DrakesTestLab (KydlCore-Mod/tests/TestLab/suites/KydlCore). They call the REAL hub / registry code; reads return short values a suite can compare.
/// </summary>
internal static class TestHooks
{
    /// Every test starts here (MIGRATION_DECISIONS C26, lessons TESTLAB_PLAYBOOK): the player on the open arena spot 70 m from the lab start, facing +z, god mode on.
    [TestHook("kc.arena")]
    static string Arena() { return TestArena.MovePlayer(); }

    /// <summary>All Startup_* / World_* checks of InGameTests (the ones Kydl.GameProbe runs): "PASS: ..." or "FAIL: ...".</summary>
    [TestHook("kc.selftest")]
    static string SelfTest() { return TestLabSupport.RunInGameTests(typeof(InGameTests)); }

    [TestHook("kc.hubOpen")]
    static bool HubOpen(string mod = null, string tab = null)
    {
        KyHub.Open(string.IsNullOrEmpty(mod) ? null : mod, string.IsNullOrEmpty(tab) ? null : tab);
        return KyHub.IsOpen;
    }

    /// <summary>
    /// KydlCore D-096: sets the KY-Items view (grid = true) and builds the KY-Items page as the hub does. Returns "grid|list, cards C, rows R,
    /// button B": C = icon cards of the grid, R = entries, B = the view button's text.
    /// </summary>
    [TestHook("kc.kyItemsView")]
    static string KyItemsView(bool grid)
    {
        if (KydlCore.Configuration.ModSettings.KyItemsView == null) return "no setting";
        KydlCore.Configuration.ModSettings.KyItemsView.Value = grid ? KydlCore.Configuration.ItemsView.Grid : KydlCore.Configuration.ItemsView.List;
        var page = KydlCore.Presentation.Hub.Registration.KyItemsTab.BuildPage(KydlCore.Presentation.Hub.Registration.KyItemsTab.ItemsId);
        int cards = page.Query(name: "kydl-item-card").ToList().Count;
        var button = page.Q<Button>("kydl-kyitems-view");
        int entries = KyItems.All.Count(e => e.Kind != KyKind.Material);
        return (KydlCore.Presentation.Hub.Registration.KyItemsTab.Grid ? "grid" : "list") + ", cards " + cards + ", entries " + entries + ", one card each " + (cards == entries) + ", button " + (button != null ? button.text : "none");
    }

    [TestHook("kc.hubIsOpen")]
    static bool HubIsOpen() { return KyHub.IsOpen; }

    [TestHook("kc.hubClose")]
    static bool HubClose() { KyHub.Close(); return KyHub.IsOpen; }

    /// <summary>Mods in the hub's row 1 (comma separated guids); the hub reads them when it opens.</summary>
    [TestHook("kc.hubMods")]
    static string HubMods() { return string.Join(",", KyHub.Model.ModGuids().ToArray()); }

    /// <summary>Number of row-2 tabs the mod has in the hub.</summary>
    [TestHook("kc.hubTabCount")]
    static int HubTabCount(string mod) { return KyHub.Model.TabIds(mod).Length; }

    /// <summary>The mod has at least min row-2 tabs (a numeric limit has to live in the hook: min on a hook step is ignored by the lab).</summary>
    [TestHook("kc.hubTabsAtLeast")]
    static bool HubTabsAtLeast(string mod, int min) { return KyHub.Model.TabIds(mod).Length >= min; }

    /// <summary>The mod has this tab; a tab shown as one tab per config category (1.1.1) still counts.</summary>
    [TestHook("kc.hubHasTab")]
    static bool HubHasTab(string mod, string tab) { return KyHub.Model.HasTab(mod, tab); }

    [TestHook("kc.hubTabs")]
    static string HubTabs(string mod) { return string.Join(",", KyHub.Model.TabIds(mod)); }

    /// <summary>Number of KY-Items entries of one mod (0 = all mods).</summary>
    [TestHook("kc.itemsCount")]
    static int ItemsCount(string mod = null)
    {
        return string.IsNullOrEmpty(mod) ? KyItems.All.Count : KyItems.ByMod(mod).Count();
    }

    /// <summary>At least min KY-Items entries of one mod (empty = all mods); a limit in the hook because min on a hook step is ignored by the lab.</summary>
    [TestHook("kc.itemsAtLeast")]
    static bool ItemsAtLeast(int min, string mod = null) { return ItemsCount(mod) >= min; }

    [TestHook("kc.itemsReady")]
    static bool ItemsReady() { return KyItems.Ready; }

    /// <summary>Spec 5: the real KyItems.Contribute with a game prefab (listing only, never added to the game). Returns the KyResult text.</summary>
    [TestHook("kc.itemsContribute")]
    static string ItemsContribute(string id, string prefab, string owner)
    {
        UnityEngine.GameObject go = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
        if (go == null && ObjectDB.instance != null) go = ObjectDB.instance.GetItemPrefab(prefab);
        return KyItems.Contribute(new KyContribution { Id = id, ModGuid = owner, Prefab = go, Kind = KyKind.Item }).ToString();
    }

    /// <summary>The prefab name listed under a contribution id ("" when none).</summary>
    [TestHook("kc.itemsById")]
    static string ItemsById(string id) { var e = KyItems.FindById(id); return e != null ? e.Prefab : ""; }

    [TestHook("kc.itemsWithdraw")]
    static bool ItemsWithdraw(string id) { return KyItems.Withdraw(id); }

    /// <summary>1.1.0: the real FlightRecorder.Track on the local player (with one fact text). Returns whether it is tracked.</summary>
    [TestHook("kc.flightTrackPlayer")]
    static bool FlightTrackPlayer(string label) { return FlightRecorder.Track(Player.m_localPlayer, label, () => "lab fact"); }

    [TestHook("kc.flightUntrack")]
    static bool FlightUntrack() { return FlightRecorder.Untrack(Player.m_localPlayer); }

    [TestHook("kc.flightNote")]
    static bool FlightNote(string text) { return FlightRecorder.Note(text); }

    /// <summary>The real FlightRecorder.Dump: the dump file's path ("" when none was written).</summary>
    [TestHook("kc.flightDump")]
    static string FlightDump(string reason) { return FlightRecorder.Dump(reason) ?? ""; }

    /// <summary>Whether the newest dump file of this game start exists and holds the text.</summary>
    [TestHook("kc.flightDumpHas")]
    static bool FlightDumpHas(string text)
    {
        string path = FlightRecorder.LastDumpPath;
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) { ModLog.Verbose("TestHooks.FlightDumpHas: returns false - no dump file of this game start (path '" + (path ?? "<none>") + "')"); return false; }
        try
        {
            bool has = File.ReadAllText(path).Contains(text);
            ModLog.Verbose("TestHooks.FlightDumpHas: '" + text + "' in " + Path.GetFileName(path) + " = " + has);
            return has;
        }
        catch (IOException e) { ModLog.Warn("TestHooks.FlightDumpHas: reading " + path + " failed (" + e.GetType().FullName + "): " + e.Message + " - returns false"); return false; }
        catch (UnauthorizedAccessException e) { ModLog.Warn("TestHooks.FlightDumpHas: no access to " + path + " (" + e.GetType().FullName + "): " + e.Message + " - returns false"); return false; }
    }
}
