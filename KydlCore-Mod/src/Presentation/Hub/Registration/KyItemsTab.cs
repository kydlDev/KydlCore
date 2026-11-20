using System.Linq;
using KydlCore.Bootstrap;
using KydlCore.Common;
using KydlCore.Configuration;
using KydlCore.Features.Hub.Contracts;
using KydlCore.Features.Hub.Infrastructure;
using KydlCore.Features.Items.Contracts;
using KydlCore.Presentation.Hub.Pages;
using KydlCore.Ui;
using UnityEngine.UIElements;
using KydlCore.Core;

namespace KydlCore.Presentation.Hub.Registration;

/// <summary>
/// KydlCore's own tabs in the KY hub (PLAN T-041, DESIGN 6, open question U1): "Settings" (its own config, the same page as the automatic one, which a
/// mod loses as soon as it registers any tab) and "KY-Items" (the list of the user's items / pieces found by <see cref="KyItems"/>). The KY-Items tab
/// exists only while the registry has entries (user: shown only when one of his mods with items is installed). Game / UI code: tested in game.
/// </summary>
internal static class KyItemsTab
{
    internal const string SettingsId = "settings", ItemsId = "ky-items", MaterialsId = "ky-materials";
    const int ItemsOrder = 10, MaterialsOrder = 11;

    /// <summary>Registers the Settings tab and follows the registry: the KY-Items tab appears and disappears with its entries.</summary>
    /// Expected outcome: true when the Settings tab is registered.
    internal static bool Init()
    {
        var settings = KyHub.AddTab(Plugin.ModGuid, SettingsId, "Settings", () => KyHub.ConfigEntries(Plugin.Instance.Config), 0);
        KyItems.Changed -= Sync;
        KyItems.Changed += Sync;
        Sync();
        ModLog.Verbose("KyItemsTab: Settings tab " + (settings != null ? "registered" : "FAILED") + ", following KyItems.Changed");
        return settings != null;
    }

    static void Sync()
    {
        if (CoreLog.On) ModLog.Every("KyItemsTab.Sync", 2d, $"KyItemsTab.Sync()");
        SyncTab(ItemsId, "KY-Items", ItemsOrder, e => e.Kind != KyKind.Material);
        SyncTab(MaterialsId, "Materials", MaterialsOrder, e => e.Kind == KyKind.Material);
    }

    // One tab per kind of list: shown only while it has entries (Materials = ingredients, gatherables and world deposits of the user's mods).
    static void SyncTab(string id, string title, int order, System.Func<KyEntry, bool> wanted)
    {
        int count = KyItems.All.Count(wanted);
        if (count > 0) KyHub.AddTab(Plugin.ModGuid, id, title, () => Build(wanted, id), order);
        else KyHub.RemoveTab(Plugin.ModGuid, id);
        ModLog.Verbose("KyItemsTab: " + (count > 0 ? title + " tab shown (" + count + " prefab(s))" : "no entries, " + title + " tab hidden"));
        if (count > 0) HubHost.Refresh(Plugin.ModGuid, id);
    }

    // The page: the user's prefabs grouped by mod, text at the settings page size (D-061).
    static VisualElement Build(System.Func<KyEntry, bool> wanted, string id)
    {
        var page = new VisualElement { name = "kydl-kyitems-page" };
        page.style.flexShrink = 0;
        var shown = KyItems.All.Where(wanted).ToList();
        var groups = shown.GroupBy(e => string.IsNullOrEmpty(e.ModName) ? e.ModGuid : e.ModName).ToList();
        bool grid = Grid;
        page.Add(Header(UiControls.Note(shown.Count + " prefab(s) of " + groups.Count + " source(s). " + (id == MaterialsId
            ? "Ingredients, gatherables and world deposits the KydlDev mods contribute."
            : "Items and pieces the KydlDev mods contribute; pieces in the '" + KyItems.CategoryName + "' build category are included.")), grid, id));
        foreach (var group in groups)
        {
            page.Add(UiControls.Heading(group.Key));
            if (grid) page.Add(ItemGridView.Build(group.Select(Card).ToList()));
            else foreach (KyEntry entry in group) page.Add(Row(entry));
        }
        ConfigPage.ScaleFonts(page);
        ModLog.Verbose("KyItemsTab: " + id + " page built (" + (grid ? "grid" : "list") + "), " + shown.Count + " prefab(s) in " + groups.Count + " group(s)");
        return page;
    }

    /// <summary>Builds the KY-Items (or, for <see cref="MaterialsId"/>, the Materials) page as the hub would (for test hooks).</summary>
    internal static VisualElement BuildPage(string id)
    {
        if (CoreLog.On) ModLog.Verbose($"KyItemsTab.BuildPage(id={id})");
        return id == MaterialsId ? Build(e => e.Kind == KyKind.Material, id) : Build(e => e.Kind != KyKind.Material, id);
    }

    /// <summary>True when the pages show the grid ([General] KY-Items View).</summary>
    internal static bool Grid => ModSettings.KyItemsView != null && ModSettings.KyItemsView.Value == ItemsView.Grid;

    /// <summary>
    /// Switches between the list and the grid (the button at the top right of the page) and rebuilds both pages. Expected outcome: the new view
    /// (true = grid).
    /// </summary>
    internal static bool ToggleView()
    {
        if (ModSettings.KyItemsView == null) { if (CoreLog.On) ModLog.Every("KyItemsTab.ToggleView:92", 2d, "KyItemsTab.ToggleView: returns false (line 92)"); return false; }
        ModSettings.KyItemsView.Value = Grid ? ItemsView.List : ItemsView.Grid;
        ModLog.Info("KyItemsTab: view " + ModSettings.KyItemsView.Value);
        HubHost.Refresh(Plugin.ModGuid, ItemsId);
        HubHost.Refresh(Plugin.ModGuid, MaterialsId);
        { var returnedAt97 = Grid; if (CoreLog.On) ModLog.Every("KyItemsTab.ToggleView:97", 2d, $"KyItemsTab.ToggleView: returns {returnedAt97} (line 97)"); return returnedAt97; }
    }

    // the note on the left, the view switch on the right (top right of the page, user 2026-11-19)
    static VisualElement Header(VisualElement note, bool grid, string id)
    {
        if (CoreLog.On) ModLog.Every("KyItemsTab.Header", 2d, $"KyItemsTab.Header(note={note}, grid={grid}, id={id})");
        var row = new VisualElement { name = "kydl-kyitems-header" };
        row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.FlexStart; row.style.flexShrink = 0;
        note.style.flexGrow = 1; note.style.flexShrink = 1;
        row.Add(note);
        var toggle = UiControls.Button(grid ? "List view" : "Grid view", () => ToggleView());
        toggle.name = "kydl-kyitems-view";
        toggle.style.flexShrink = 0; toggle.style.marginLeft = 12;
        row.Add(toggle);
        { var returnedAt112 = row; if (CoreLog.On) ModLog.Every("KyItemsTab.Header:112", 2d, $"KyItemsTab.Header: returns {returnedAt112} (line 112)"); return returnedAt112; }
    }

    static ItemCard Card(KyEntry entry)
    {
        if (CoreLog.On) ModLog.Verbose($"KyItemsTab.Card(entry={entry})");
        { var returnedAt118 = new ItemCard { Title = entry.DisplayName, Subtitle = entry.Prefab, Icon = entry.Icon }; if (CoreLog.On) ModLog.Verbose($"KyItemsTab.Card: returns {returnedAt118} (line 118)"); return returnedAt118; }
    }

    static VisualElement Row(KyEntry entry)
    {
        if (CoreLog.On) ModLog.Every("KyItemsTab.Row", 2d, $"KyItemsTab.Row(entry={entry})");
        var r = new VisualElement();
        r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.flexShrink = 0; r.style.marginTop = 2; r.style.marginBottom = 2;
        var icon = new VisualElement { pickingMode = PickingMode.Ignore };
        icon.style.width = 28; icon.style.height = 28; icon.style.flexShrink = 0; icon.style.marginRight = 8;
        if (entry.Icon != null) icon.style.backgroundImage = new StyleBackground(entry.Icon);
        r.Add(icon);
        var name = UiKit.Label(entry.DisplayName, UiKit.NameSize, UiKit.NameColor);
        name.style.flexGrow = 1; name.style.whiteSpace = WhiteSpace.NoWrap;
        r.Add(name);
        r.Add(UiKit.Label(entry.Kind + "  " + (string.IsNullOrEmpty(entry.Id) ? entry.Prefab : entry.Id), UiKit.SmallSize, UiKit.TextDim));
        { var returnedAt134 = r; if (CoreLog.On) ModLog.Every("KyItemsTab.Row:134", 2d, $"KyItemsTab.Row: returns {returnedAt134} (line 134)"); return returnedAt134; }
    }
}
