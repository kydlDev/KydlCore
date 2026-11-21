using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Bootstrap;
using KydlCore.Common;
using KydlCore.Configuration;
using KydlCore.Features.Hub.Contracts;
using KydlCore.Features.Hub.Domain;
using KydlCore.Presentation.Hub.Pages;
using KydlCore.Ui;
using UnityEngine;
using UnityEngine.UIElements;
using KydlCore.Core;

namespace KydlCore.Features.Hub.Infrastructure;

/// <summary>
/// The GAME side of the KY hub (PLAN T-034): the key, the window, the input lock, the list of loaded KydlDev mods. What the window looks like is
/// <see cref="HubView"/> (Unity project, shown by the preview too), what it lists is <see cref="HubModel"/> (pure, tested); mods talk to it
/// only through <see cref="KyHub"/>. Escape closes the top window (the lock makes the game ignore Escape while it is open, like TextInput).
/// </summary>
internal static class HubHost
{
    const string WindowId = KyWindowId.Hub;

    static WindowStack Stack { get { return KyWindows.Stack; } }       // the one stack of the hub and the KyUi windows
    static HubView _view;
    static float _openedAt;                   // Time.time of the open (the close line says how long: two closes within 2.5 s are no longer 'identical' for the log's repeat filter)
    static ConfigPage _config;                // the auto Settings page that is on screen (disposed when another page replaces it)

    internal static bool IsOpen { get { return _view != null; } }

    /// <summary>Every frame (Plugin.Update): the hub key opens / closes the hub, Escape closes the top window. Returns whether the hub is open.</summary>
    internal static bool Tick()
    {
        if (KeyCapture.Blocks) { var returnedAt36 = IsOpen; if (CoreLog.Due("HubHost.Tick:36", 2d)) ModLog.Every("HubHost.Tick:36", 2d, $"HubHost.Tick: returns {CoreLog.Shown(returnedAt36)} (line 36)"); return returnedAt36; }          // a key is being chosen for a setting (maybe this very key): it must not toggle the hub
        KeyCode key = ModSettings.HubKey != null ? ModSettings.HubKey.Value : KeyCode.None;
        if (!IsOpen)
        {
            if (key == KeyCode.None || !ZInput.GetKeyDown(key, true)) { if (CoreLog.Due("HubHost.Tick:40", 2d)) ModLog.Every("HubHost.Tick:40", 2d, "HubHost.Tick: returns false (line 40)"); return false; }
            if (Chat.instance != null && Chat.instance.HasFocus() || global::Console.IsVisible() || Menu.IsVisible())
            {
                ModLog.Verbose("HubHost: key pressed while chat / console / game menu is open - not opened");
                return false;
            }
            Open(null, null);
            { var returnedAt47 = IsOpen; if (CoreLog.Due("HubHost.Tick:47", 2d)) ModLog.Every("HubHost.Tick:47", 2d, $"HubHost.Tick: returns {CoreLog.Shown(returnedAt47)} (line 47)"); return returnedAt47; }
        }
        if (key != KeyCode.None && ZInput.GetKeyDown(key, true)) Close("key");      // Escape: KyWindows.Tick closes the front window
        { var returnedAt50 = IsOpen; if (CoreLog.Due("HubHost.Tick:50", 2d)) ModLog.Every("HubHost.Tick:50", 2d, $"HubHost.Tick: returns {CoreLog.Shown(returnedAt50)} (line 50)"); return returnedAt50; }
    }

    /// <summary>Opens the hub (or just selects: it is open already) and shows `mod` / `tab`; null = where it was.</summary>
    internal static void Open(string mod, string tab)
    {
        SyncMods();
        if (mod != null && !KyHub.Model.Select(mod, tab))
            ModLog.Warn("HubHost: no mod '" + mod + "' / tab '" + tab + "' in the hub - showing the last page");
        if (IsOpen) { Stack.BringToFront(WindowId); ModLog.Verbose("HubHost: already open, selection changed"); return; }
        var root = UiKit.Root;
        if (root == null) { ModLog.Warn("HubHost: UI Toolkit is not available, the hub cannot open"); return; }
        KyWindows.EnsureWired();
        _view = new HubView(KyHub.Model, k => Loc.T(k), Page, () => Close("X"));
        root.Add(_view.Root);
        Stack.Open(WindowId);
        _openedAt = Time.time;
        ModLog.Verbose("HubHost: opened, " + KyHub.Model.ModCount + " mod(s), page " + KyHub.Model.SelectedMod + " / " + KyHub.Model.SelectedTab);
    }

    static string OpenFor() { return (Time.time - _openedAt).ToString("F1"); }      // own method: Time.time cannot be compiled outside Unity, so Close() with nothing open stays unit-testable

    internal static void Close(string why)
    {
        if (_view == null) { if (CoreLog.Due("HubHost.Close:74", 2d)) ModLog.Every("HubHost.Close:74", 2d, "HubHost.Close: returns (line 74)"); return; }
        _view.Root.RemoveFromHierarchy();
        _view.Dispose();
        _view = null;
        DisposeConfig();
        Stack.Close(WindowId);
        ModLog.Verbose("HubHost: closed (" + why + ") after " + OpenFor() + " s open");
    }

    /// <summary>KyTab.Refresh: build the page again when it is the one on screen.</summary>
    internal static void Refresh(string mod, string tab)
    {
        if (CoreLog.Due("HubHost.Refresh", 2d)) ModLog.Every("HubHost.Refresh", 2d, $"HubHost.Refresh(mod={CoreLog.Shown(mod)}, tab={CoreLog.Shown(tab)})");
        if (_view != null && KyHub.Model.SelectedMod == mod && KyHub.Model.SelectedTab == tab) _view.Refresh();
    }

    // Row 1 = every loaded plugin whose GUID starts "com.kydldev." (BepInEx Chainloader); a SetModTitle name wins over the plugin's own name.
    static void SyncMods()
    {
        int before = KyHub.Model.ModCount;
        foreach (PluginInfo info in Chainloader.PluginInfos.Values)
        {
            string guid = info.Metadata.GUID;
            if (!HubModel.IsHubGuid(guid)) continue;
            KyHub.Model.AddMod(guid, KyHub.TitleOf(guid) ?? info.Metadata.Name);
        }
        ModLog.Verbose("HubHost: mods listed " + before + " -> " + KyHub.Model.ModCount);
        SplitSettings();
    }

    // User 2026-11-16: every config category of a mod is its own row-2 tab, for every mod (sections that belong together are one tab, see
    // ConfigGroups). The mods register ONE "Settings" tab (or none: the automatic one) that shows all their config entries; the hub shows
    // that tab as one tab per group. Worked out every time the hub opens: a mod may have made new sections since (a row for a new player).
    static readonly Dictionary<string, List<ConfigGroups.Group>> Groups = new Dictionary<string, List<ConfigGroups.Group>>();

    static void SplitSettings()
    {
        foreach (PluginInfo info in Chainloader.PluginInfos.Values)
        {
            string guid = info.Metadata.GUID;
            if (!HubModel.IsHubGuid(guid) || info.Instance == null || info.Instance.Config == null) continue;
            var sections = new List<string>();
            foreach (BepInEx.Configuration.ConfigDefinition def in info.Instance.Config.Keys)
                if (!sections.Contains(def.Section)) sections.Add(def.Section);
            List<ConfigGroups.Group> groups = ConfigGroups.Of(sections);
            Groups[guid] = groups;
            var names = new List<string>();
            foreach (ConfigGroups.Group g in groups) names.Add(g.Name);
            KyHub.Model.SplitTab(guid, HubModel.AutoTabId, names);
            ModLog.Verbose("HubHost: Settings of " + guid + " shown as " + (names.Count < 2 ? "one tab" : names.Count + " tabs (" + string.Join(", ", names.ToArray()) + ")"));
        }
    }

    // The page of one part of a mod's Settings tab: the config entries of that group's sections.
    static VisualElement GroupPage(string mod, string part)
    {
        PluginInfo info;
        List<ConfigGroups.Group> groups;
        if (!Chainloader.PluginInfos.TryGetValue(mod, out info) || info.Instance == null || !Groups.TryGetValue(mod, out groups)) { if (CoreLog.On) ModLog.Verbose("HubHost.GroupPage: returns null (line 132)"); return null; }
        ConfigGroups.Group group = groups.Find(g => g.Name == part);
        if (group == null) { if (CoreLog.On) ModLog.Verbose("HubHost.GroupPage: returns null (line 134)"); return null; }
        // sections made since the hub was opened (a new player's colour row) still belong to their tab
        foreach (BepInEx.Configuration.ConfigDefinition def in info.Instance.Config.Keys)
            if (ConfigGroups.NameOf(def.Section) == part && !group.Sections.Contains(def.Section)) group.Sections.Add(def.Section);
        try
        {
            // a mod's own panel at the end of the tab (KyHub.SetGroupPanel) draws some sections itself: they are not drawn as plain entries
            KyHub.GroupPanel panel = KyHub.PanelOf(mod, part);
            List<string> plain = ConfigGroups.Shown(group.Sections, panel == null ? null : panel.Replaces);
            // never ConfigPage with no sections: that means "every section of the mod"
            if (plain.Count > 0) _config = new ConfigPage(info.Instance.Config, plain.ToArray());
            if (panel == null) { var returnedAt145 = _config.Root; if (CoreLog.On) ModLog.Verbose($"HubHost.GroupPage: returns {CoreLog.Shown(returnedAt145)} (line 145)"); return returnedAt145; }
            var page = new VisualElement { name = "kydl-group-page" };
            page.style.flexShrink = 0;
            // the panel goes above the entries (KyHub.SetGroupPanel atTop / SetGroupPage) or after them
            VisualElement extra = panel.Build();
            if (panel.AtTop && extra != null) page.Add(extra);
            if (_config != null) page.Add(_config.Root);
            if (!panel.AtTop && extra != null) page.Add(extra);
            { var returnedAt153 = page; if (CoreLog.On) ModLog.Verbose($"HubHost.GroupPage: returns {CoreLog.Shown(returnedAt153)} (line 153)"); return returnedAt153; }
        }
        catch (Exception e)
        {
            ModLog.Error("HubHost: the '" + part + "' settings page of " + mod + " failed: " + e);
            return null;
        }
    }

    // The page factory of HubView: the mod's own page, or the auto Settings page (all config entries) of a mod without tabs.
    static VisualElement Page(string mod, string tab)
    {
        DisposeConfig();
        VisualElement page = null;
        string part = HubModel.PartOf(tab);
        if (part != null) page = global::KydlCore.Ui.UiLog.Within(mod, () => GroupPage(mod, part));
        else if (KyHub.Model.IsAutoTab(mod)) page = global::KydlCore.Ui.UiLog.Within(mod, () => AutoPage(mod));
        else page = KyHub.BuildPage(mod, tab);
        if (page != null)
        {
            // every page, also one a mod built itself, is shown with KydlCore's text sizes and font; repeated while it is on screen,
            // because a page may add rows later (a colour editor that opens, another bubble kind chosen)
            VisualElement shown = page;
            global::KydlCore.Presentation.Hub.Pages.ConfigPage.EnforceText(shown);
            shown.schedule.Execute(() => global::KydlCore.Presentation.Hub.Pages.ConfigPage.EnforceText(shown)).Every(EnforceTextMs);
        }
        KyHub.RaiseTabShown(mod, tab);
        { var returnedAt180 = page; if (CoreLog.On) ModLog.Verbose($"HubHost.Page: returns {CoreLog.Shown(returnedAt180)} (line 180)"); return returnedAt180; }
    }

    const long EnforceTextMs = 200;

    static VisualElement AutoPage(string mod)
    {
        PluginInfo info;
        if (!Chainloader.PluginInfos.TryGetValue(mod, out info) || info.Instance == null)
        {
            ModLog.Warn("HubHost: no loaded plugin '" + mod + "' for its Settings page");
            return null;
        }
        try
        {
            _config = new ConfigPage(info.Instance.Config);
            { var returnedAt196 = _config.Root; if (CoreLog.On) ModLog.Verbose($"HubHost.AutoPage: returns {CoreLog.Shown(returnedAt196)} (line 196)"); return returnedAt196; }
        }
        catch (Exception e)
        {
            ModLog.Error("HubHost: the Settings page of " + mod + " failed: " + e);
            return null;
        }
    }

    static void DisposeConfig()
    {
        if (CoreLog.Due("HubHost.DisposeConfig", 2d)) ModLog.Every("HubHost.DisposeConfig", 2d, $"HubHost.DisposeConfig()");
        if (_config == null) { if (CoreLog.Due("HubHost.DisposeConfig:208", 2d)) ModLog.Every("HubHost.DisposeConfig:208", 2d, "HubHost.DisposeConfig: returns (line 208)"); return; }
        _config.Dispose();
        _config = null;
    }
}
