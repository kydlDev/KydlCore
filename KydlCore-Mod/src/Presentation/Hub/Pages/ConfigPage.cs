using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using KydlCore.Common;
using KydlCore.Features.Hub.Infrastructure;
using KydlCore.Ui;
using UnityEngine;
using UnityEngine.UIElements;
using KydlCore.Core;

namespace KydlCore.Presentation.Hub.Pages;

/// <summary>
/// The page of the auto "Settings" tab (PLAN T-033, D-023): one control for EVERY config entry of a mod, grouped by section. A change in the page
/// is written through <c>ConfigEntryBase.BoxedValue</c> (so the mod's own SettingChanged handlers apply it in game at once); a change made in the
/// .cfg file / by code comes in through <c>ConfigFile.SettingChanged</c> and redraws the page (hot reload, both ways, PROJECT_RULES 17). Which
/// control an entry gets, number reading and the Browsable / IsAdminOnly tags are the pure <see cref="ConfigControlMap"/>. Drawing only: tested in
/// game / in the preview, not by unit tests.
/// </summary>
internal sealed partial class ConfigPage : IDisposable
{
    const long PollMs = 250;                  // the config event can come from the file watcher thread: the page redraws on the UI thread
    const int TextMax = 256;
    const float FontScale = HubView.PageFontScale;  // every text of the page is this part of the kit's size (user 2026-10-30: 65%, D-061)
    const float SliderGapPercent = 6f;
    const float SliderMinWidth = 170f;        // a slider / colour editor never gets narrower; a longer setting name is cut with "..." instead

    readonly ConfigFile _file;
    readonly string[] _sections;              // null / empty = every section
    readonly VisualElement _root = new VisualElement { name = "kydl-config-page" };
    volatile bool _dirty;
    bool _writing;
    ConfigEntryBase _writingEntry;            // the entry this page is writing right now
    // text / number boxes by entry: when a write of the page makes the MOD change another entry (a colour row keeps "Hex" in step with
    // Red / Green / Blue), that box is updated in place - no redraw, so a slider that is being dragged is never rebuilt (user 2026-11-16)
    readonly Dictionary<ConfigEntryBase, Action> _followers = new Dictionary<ConfigEntryBase, Action>();
    IVisualElementScheduledItem _poll;

    internal ConfigPage(ConfigFile file, string[] sections = null)
    {
        _file = file;
        _sections = sections != null && sections.Length > 0 ? sections : null;
        _root.style.flexShrink = 0;
        _file.SettingChanged += OnSettingChanged;
        Fill();
        _poll = _root.schedule.Execute(Poll).Every(PollMs);
        ModLog.Verbose("ConfigPage: built for " + (file == null ? "(no file)" : file.ConfigFilePath));
    }

    /// <summary>The page; the hub puts it under the tab row.</summary>
    internal VisualElement Root { get { return _root; } }

    /// <summary>Stops listening to the config file (call when the page is thrown away).</summary>
    public void Dispose()
    {
        _file.SettingChanged -= OnSettingChanged;
        KeyCapture.Cancel("page closed");
        if (_poll != null) _poll.Pause();
        ModLog.Verbose("ConfigPage: disposed");
    }

    void OnSettingChanged(object sender, SettingChangedEventArgs e)
    {
        if (CoreLog.On) ModLog.Verbose($"ConfigPage.OnSettingChanged(sender={CoreLog.Shown(sender)}, e={CoreLog.Shown(e)})");
        if (_writing && ReferenceEquals(e.ChangedSetting, _writingEntry)) { if (CoreLog.On) ModLog.Verbose("ConfigPage.OnSettingChanged: returns (line 66)"); return; }      // our own write: the control already shows the value
        if (_writing)
        {
            // another entry changed BECAUSE of our write (the mod keeps two entries in step): show its new value
            Action follow;
            if (_followers.TryGetValue(e.ChangedSetting, out follow)) follow();
            else _dirty = true;
            { if (CoreLog.On) ModLog.Verbose("ConfigPage.OnSettingChanged: returns (line 73)"); return; }
        }
        _dirty = true;
    }

    // ServerSync flips the ReadOnly tag of its entries when this player's admin state arrives or changes, without a SettingChanged event:
    // the page notices by comparing which entries are locked now with what it drew.
    string _lockSignature = "";

    string LockSignature()
    {
        if (CoreLog.Due("ConfigPage.LockSignature", 2d)) ModLog.Every("ConfigPage.LockSignature", 2d, $"ConfigPage.LockSignature()");
        var sb = new System.Text.StringBuilder();
        foreach (ConfigDefinition def in _file.Keys)
            if (InSections(def.Section))
            {
                IEnumerable<object> tags = Tags(_file[def]);
                sb.Append(ConfigControlMap.IsReadOnly(tags) ? '1' : '0').Append(ConfigControlMap.IsHidden(tags) ? 'h' : 's');   // a tag may change a flag while the game runs
            }
        { var returnedAt92 = sb.ToString(); if (CoreLog.Due("ConfigPage.LockSignature:92", 2d)) ModLog.Every("ConfigPage.LockSignature:92", 2d, $"ConfigPage.LockSignature: returns {CoreLog.Shown(returnedAt92)} (line 92)"); return returnedAt92; }
    }

    void Poll()
    {
        string sig = LockSignature();
        if (sig != _lockSignature) _dirty = true;
        if (!_dirty) { if (CoreLog.On) ModLog.Verbose("ConfigPage.Poll: returns (line 99)"); return; }
        _dirty = false;
        ModLog.Verbose("ConfigPage: a setting changed outside the page, redrawing");
        Fill();
    }

    // --- drawing -------------------------------------------------------------------------------------------------------
    void Fill()
    {
        KeyCapture.Cancel("page redrawn");
        _lockSignature = LockSignature();
        _root.Clear();
        _followers.Clear();
        var shown = new List<ConfigEntryBase>();
        foreach (ConfigDefinition def in _file.Keys)
        {
            ConfigEntryBase entry = _file[def];
            if (InSections(def.Section) && !ConfigControlMap.IsHidden(Tags(entry))) shown.Add(entry);
        }
        foreach (var section in shown.GroupBy(e => e.Definition.Section))        // GroupBy keeps the order of first appearance
        {
            _root.Add(UiControls.Heading(section.Key));
            foreach (ConfigEntryBase entry in section) _root.Add(Control(entry));
        }
        if (shown.Count == 0) _root.Add(UiControls.Note("This mod has no settings."));
        ScaleFonts(_root);
        ModLog.Verbose("ConfigPage: " + shown.Count + " setting(s) shown");
    }

    bool InSections(string section)
    {
        if (CoreLog.Due("ConfigPage.InSections", 2d)) ModLog.Every("ConfigPage.InSections", 2d, $"ConfigPage.InSections(section={CoreLog.Shown(section)})");
        { var returnedAt131 = _sections == null || Array.IndexOf(_sections, section) >= 0; if (CoreLog.Due("ConfigPage.InSections:131", 2d)) ModLog.Every("ConfigPage.InSections:131", 2d, $"ConfigPage.InSections: returns {CoreLog.Shown(returnedAt131)} (line 131)"); return returnedAt131; }
    }

    static IEnumerable<object> Tags(ConfigEntryBase entry)
    {
        if (CoreLog.Due("ConfigPage.Tags", 2d)) ModLog.Every("ConfigPage.Tags", 2d, $"ConfigPage.Tags(entry={CoreLog.Shown(entry)})");
        { var returnedAt137 = entry.Description == null ? null : entry.Description.Tags; if (CoreLog.Due("ConfigPage.Tags:137", 2d)) ModLog.Every("ConfigPage.Tags:137", 2d, $"ConfigPage.Tags: returns {CoreLog.Shown(returnedAt137)} (line 137)"); return returnedAt137; }
    }

    VisualElement Control(ConfigEntryBase entry)
    {
        string key = entry.Definition.Key;
        var box = new VisualElement();
        box.style.flexShrink = 0;
        IEnumerable<object> tags = Tags(entry);
        bool serverLocked = ConfigControlMap.IsReadOnly(tags);              // ServerSync: this player may not change the server's value
        bool locked = serverLocked || (ConfigControlMap.IsAdminOnly(tags) && !MayEditAdminOnly());
        VisualElement control = locked ? LockedValue(entry) : Make(entry, key);
        if (control.name != "kydl-toggle") Compact(control);
        box.Add(control.name == "kydl-toggle" ? control : Row(key, control));
        if (locked) box.Add(UiControls.Note(serverLocked ? "Set by the server: only an admin can change this." : "Admin only: only the host / an admin can change this."));
        string text = entry.Description == null ? null : entry.Description.Description;
        if (!string.IsNullOrEmpty(text)) box.Add(UiControls.Note(text));
        ModLog.Verbose("ConfigPage: control for " + entry.Definition.Section + " / " + key + " (" + entry.SettingType.Name + ")" + (locked ? " locked" : ""));
        return box;
    }

    // One line: the name on the left, the control right of it at the size of its content (a drop-down is only as wide as its text); only a
    // slider / colour editor stretches. The name does not wrap.
    static VisualElement Row(string label, VisualElement control)
    {
        if (CoreLog.Due("ConfigPage.Row", 2d)) ModLog.Every("ConfigPage.Row", 2d, $"ConfigPage.Row(label={CoreLog.Shown(label)}, control={CoreLog.Shown(control)})");
        var r = new VisualElement();
        r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.flexShrink = 0;
        r.style.marginTop = 4; r.style.marginBottom = 4;
        var l = UiKit.Label(label, UiKit.NameSize, UiKit.NameColor);
        // the name takes the width its text needs; only when the text would run into the slider (the slider keeps SliderMinWidth) the name is
        // cut with "..." (user 2026-11-04: "..." only when the text really collides with the slider)
        l.style.flexGrow = 0; l.style.flexShrink = 1; l.style.minWidth = 0; l.style.whiteSpace = WhiteSpace.NoWrap;
        l.style.overflow = Overflow.Hidden; l.style.textOverflow = TextOverflow.Ellipsis; l.style.marginRight = 8;
        l.tooltip = label;
        r.Add(l);
        bool stretch = control.name == "kydl-slider" || control.name == "kydl-color";
        bool textBox = control.name == "kydl-text";
        control.style.flexGrow = stretch ? 1f : 0f;
        control.style.flexShrink = textBox ? 1f : 0f;                 // a text box gives way when the panel is narrow; its text is clipped inside; a slider keeps its minimum, the NAME gives way
        if (stretch) control.style.minWidth = SliderMinWidth;
        else if (textBox) control.style.minWidth = 60;
        if (!stretch) { control.style.alignSelf = Align.FlexStart; control.style.marginLeft = StyleKeyword.Auto; }   // value fields sit at the right edge (D-062)
        else control.style.marginLeft = Length.Percent(SliderGapPercent);      // user 2026-11-04: ~10 % shorter sliders and a gap to the name (percent of the row ~ 10 % of the slider)
        r.Add(control);
        { var returnedAt182 = r; if (CoreLog.Due("ConfigPage.Row:182", 2d)) ModLog.Every("ConfigPage.Row:182", 2d, $"ConfigPage.Row: returns {CoreLog.Shown(returnedAt182)} (line 182)"); return returnedAt182; }
    }

    // The text in the value fields (number / text box, drop-down, colour editor) has the size of the drop-down text (the "F9" of PoolMenuKey, the
    // kit's FontSize), with a small margin inside the box (user 2026-10-30, D-063). The page scale is applied afterwards.
    static void Compact(VisualElement control)
    {
        if (CoreLog.Due("ConfigPage.Compact", 2d)) ModLog.Every("ConfigPage.Compact", 2d, $"ConfigPage.Compact(control={CoreLog.Shown(control)})");
        control.Query<VisualElement>().ForEach(e =>
        {
            if (e.style.fontSize.keyword == StyleKeyword.Undefined && e.style.fontSize.value.value > 0f) e.style.fontSize = UiKit.FontSize;
        });
        control.Query<TextField>().ForEach(f =>
        {
            VisualElement input = f.Q(className: TextField.inputUssClassName) ?? f;
            UiKit.Pad(input, 1, 5);
            input.style.fontSize = UiKit.FontSize;
            f.style.overflow = Overflow.Hidden;                                 // long text (a webhook URL) is clipped by the box, not drawn past its border
            input.style.overflow = Overflow.Hidden;
            input.style.minWidth = 0;
            f.Query<TextElement>().ForEach(t => { t.style.overflow = Overflow.Hidden; t.style.flexShrink = 1; t.style.minWidth = 0; });
        });
        control.Query<Button>().ForEach(b => UiKit.Pad(b, 1, 5));
    }

    // Makes every text of the page smaller by FontScale (the kit's controls set their sizes inline, so the inline value is scaled).
    // D-078 (user 2026-10-30 23:39: the SteveTheTroll Trolls page "autoresizes to a bigger font-size"): a page that builds more of itself later (a new troll chosen in
    // its drop-down) added elements with the kit's full inline sizes after the one-off scaling. Every element is scaled ONCE (marked with ScaledClass), so the scaling
    // can be repeated - KyHub.KeepScaled does that while the page is shown.
    internal const string ScaledClass = "kydl-font-scaled";

    /// <summary>
    /// KydlCore's text rules for a page a MOD built itself (user 2026-11-16: "KydlCore displays information at the font sizes / font families
    /// set in it at all times, it always overrides"): every text is scaled like KydlCore's own pages, and a font a mod set on an element is
    /// taken off again, so the panel's own font shows. Safe to repeat (see ScaleFonts).
    /// </summary>
    internal static void EnforceText(VisualElement root)
    {
        if (CoreLog.On) ModLog.Verbose($"ConfigPage.EnforceText(root={CoreLog.Shown(root)})");
        if (root == null) { if (CoreLog.On) ModLog.Verbose("ConfigPage.EnforceText: returns (line 221)"); return; }
        ScaleFonts(root);
        root.Query<VisualElement>().ForEach(e =>
        {
            if (e.style.unityFontDefinition.keyword == StyleKeyword.Undefined) e.style.unityFontDefinition = StyleKeyword.Null;
            if (e.style.unityFont.keyword == StyleKeyword.Undefined) e.style.unityFont = StyleKeyword.Null;
            // text colours (user 2026-11-16: the colours of setting names and options are KydlCore's, never a mod's): a text colour that is
            // not one of KydlCore's own is replaced - the value colour inside an input box, the name colour everywhere else
            StyleColor colour = e.style.color;
            bool text = e is TextElement || e is TextField;
            if (text && colour.keyword == StyleKeyword.Undefined && !e.ClassListContains(UiKit.ChipClass) && !ThemeColours.IsOwn(colour.value))
                e.style.color = InsideInput(e) ? UiKit.ValueColor : UiKit.NameColor;
        });
    }

    static bool InsideInput(VisualElement e)
    {
        if (CoreLog.On) ModLog.Verbose($"ConfigPage.InsideInput(e={CoreLog.Shown(e)})");
        for (VisualElement at = e; at != null; at = at.parent)
            if (at is TextField) { if (CoreLog.On) ModLog.Verbose("ConfigPage.InsideInput: returns true (line 240)"); return true; }
        { if (CoreLog.On) ModLog.Verbose("ConfigPage.InsideInput: returns false (line 241)"); return false; }
    }

    internal static void ScaleFonts(VisualElement root)
    {
        if (CoreLog.Due("ConfigPage.ScaleFonts", 2d)) ModLog.Every("ConfigPage.ScaleFonts", 2d, $"ConfigPage.ScaleFonts(root={CoreLog.Shown(root)})");
        root.Query<VisualElement>().ForEach(e =>
        {
            if (e.ClassListContains(ScaledClass)) { if (CoreLog.Due("ConfigPage.ScaleFonts:249", 2d)) ModLog.Every("ConfigPage.ScaleFonts:249", 2d, "ConfigPage.ScaleFonts: returns (line 249)"); return; }
            StyleLength fs = e.style.fontSize;
            if (fs.keyword == StyleKeyword.Undefined && fs.value.value > 0f) { e.style.fontSize = fs.value.value * FontScale; e.AddToClassList(ScaledClass); }
        });
    }

    // A client of someone else's server may not change an admin-only entry (the server owns it); in single player and on the host everything is open.
    static bool MayEditAdminOnly()
    {
        if (CoreLog.Due("ConfigPage.MayEditAdminOnly", 2d)) ModLog.Every("ConfigPage.MayEditAdminOnly", 2d, $"ConfigPage.MayEditAdminOnly()");
        { var returnedAt259 = ZNet.instance == null || ZNet.instance.IsServer(); if (CoreLog.Due("ConfigPage.MayEditAdminOnly:259", 2d)) ModLog.Every("ConfigPage.MayEditAdminOnly:259", 2d, $"ConfigPage.MayEditAdminOnly: returns {CoreLog.Shown(returnedAt259)} (line 259)"); return returnedAt259; }
    }

    static VisualElement LockedValue(ConfigEntryBase entry)
    {
        if (CoreLog.Due("ConfigPage.LockedValue", 2d)) ModLog.Every("ConfigPage.LockedValue", 2d, $"ConfigPage.LockedValue(entry={CoreLog.Shown(entry)})");
        { var returnedAt265 = UiControls.Note(entry.GetSerializedValue()); if (CoreLog.Due("ConfigPage.LockedValue:265", 2d)) ModLog.Every("ConfigPage.LockedValue:265", 2d, $"ConfigPage.LockedValue: returns {CoreLog.Shown(returnedAt265)} (line 265)"); return returnedAt265; }
    }

}
