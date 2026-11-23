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

/// <summary>ConfigPage, part 2 (G04 class size, KydlCore 1.1.2): the control builders - slider, toggle, dropdown, rebind button, colour editor - and the write-back.</summary>
internal sealed partial class ConfigPage
{
    VisualElement Make(ConfigEntryBase entry, string key)
    {
        if (CoreLog.Due("ConfigPage.Make", 2d)) ModLog.Every("ConfigPage.Make", 2d, $"ConfigPage.Make(entry={CoreLog.Shown(entry)}, key={CoreLog.Shown(key)})");
        Type type = entry.SettingType;
        double min, max;
        bool hasRange = Range(entry, out min, out max);
        IList<string> list = ListOf(entry);
        switch (ConfigControlMap.Classify(type, hasRange, list != null))
        {
            case ControlKind.Toggle: { var returnedAt277 = Toggle(entry, key); if (CoreLog.Due("ConfigPage.Make:277", 2d)) ModLog.Every("ConfigPage.Make:277", 2d, $"ConfigPage.Make: returns {CoreLog.Shown(returnedAt277)} (line 277)"); return returnedAt277; }
            case ControlKind.Slider: { var returnedAt278 = Slider(entry, min, max); if (CoreLog.Due("ConfigPage.Make:278", 2d)) ModLog.Every("ConfigPage.Make:278", 2d, $"ConfigPage.Make: returns {CoreLog.Shown(returnedAt278)} (line 278)"); return returnedAt278; }
            case ControlKind.Number: return Sized(Follow(entry, UiControls.TextField(ConfigControlMap.Format(type, entry.BoxedValue), TextMax, t => Write(entry, ConfigControlMap.Parse(type, t))), () => ConfigControlMap.Format(type, entry.BoxedValue)), 160f, "kydl-text");
            case ControlKind.Text: return Sized(Follow(entry, UiControls.TextField((string)entry.BoxedValue, TextMax, t => Write(entry, t)), () => (string)entry.BoxedValue), 360f, "kydl-text");
            case ControlKind.Dropdown: { var returnedAt281 = Dropdown(entry, list); if (CoreLog.Due("ConfigPage.Make:281", 2d)) ModLog.Every("ConfigPage.Make:281", 2d, $"ConfigPage.Make: returns {CoreLog.Shown(returnedAt281)} (line 281)"); return returnedAt281; }
            case ControlKind.Color: { var returnedAt282 = ColorEditor(entry); if (CoreLog.Due("ConfigPage.Make:282", 2d)) ModLog.Every("ConfigPage.Make:282", 2d, $"ConfigPage.Make: returns {CoreLog.Shown(returnedAt282)} (line 282)"); return returnedAt282; }
            case ControlKind.Key: { var returnedAt283 = RebindButton(entry, false); if (CoreLog.Due("ConfigPage.Make:283", 2d)) ModLog.Every("ConfigPage.Make:283", 2d, $"ConfigPage.Make: returns {CoreLog.Shown(returnedAt283)} (line 283)"); return returnedAt283; }
            case ControlKind.Shortcut: { var returnedAt284 = RebindButton(entry, true); if (CoreLog.Due("ConfigPage.Make:284", 2d)) ModLog.Every("ConfigPage.Make:284", 2d, $"ConfigPage.Make: returns {CoreLog.Shown(returnedAt284)} (line 284)"); return returnedAt284; }
            default: { var returnedAt285 = UiControls.Note(entry.GetSerializedValue()); if (CoreLog.Due("ConfigPage.Make:285", 2d)) ModLog.Every("ConfigPage.Make:285", 2d, $"ConfigPage.Make: returns {CoreLog.Shown(returnedAt285)} (line 285)"); return returnedAt285; }
        }
    }

    // Registers a text box as the follower of its entry: the box shows the entry's value again when the mod changed it during a write of this page.
    TextField Follow(ConfigEntryBase entry, TextField field, Func<string> shown)
    {
        if (CoreLog.Due("ConfigPage.Follow", 2d)) ModLog.Every("ConfigPage.Follow", 2d, $"ConfigPage.Follow(entry={CoreLog.Shown(entry)}, field={CoreLog.Shown(field)}, shown={CoreLog.Shown(shown)})");
        _followers[entry] = () => field.SetValueWithoutNotify(shown() ?? "");
        { var returnedAt294 = field; if (CoreLog.Due("ConfigPage.Follow:294", 2d)) ModLog.Every("ConfigPage.Follow:294", 2d, $"ConfigPage.Follow: returns {CoreLog.Shown(returnedAt294)} (line 294)"); return returnedAt294; }
    }

    static VisualElement Sized(VisualElement box, float width, string name)
    {
        if (CoreLog.Due("ConfigPage.Sized", 2d)) ModLog.Every("ConfigPage.Sized", 2d, $"ConfigPage.Sized(box={CoreLog.Shown(box)}, width={CoreLog.Shown(width)}, name={CoreLog.Shown(name)})");
        box.name = name;
        box.style.width = width;
        { var returnedAt302 = box; if (CoreLog.Due("ConfigPage.Sized:302", 2d)) ModLog.Every("ConfigPage.Sized:302", 2d, $"ConfigPage.Sized: returns {CoreLog.Shown(returnedAt302)} (line 302)"); return returnedAt302; }
    }

    VisualElement Slider(ConfigEntryBase entry, double min, double max)
    {
        if (CoreLog.Due("ConfigPage.Slider", 2d)) ModLog.Every("ConfigPage.Slider", 2d, $"ConfigPage.Slider(entry={CoreLog.Shown(entry)}, min={CoreLog.Shown(min)}, max={CoreLog.Shown(max)})");
        Type type = entry.SettingType;
        var slider = UiControls.Slider((float)min, (float)max, Convert.ToSingle(entry.BoxedValue), ConfigControlMap.SliderFormat(type), v => Write(entry, ConfigControlMap.Clamp(type, v, min, max)));
        slider.name = "kydl-slider";
        slider.style.minHeight = 24;
        var input = slider.Q<TextField>();                                      // the number box: narrower and lower (user 2026-10-30, D-061)
        if (input != null) input.style.width = 46;
        { var returnedAt314 = slider; if (CoreLog.Due("ConfigPage.Slider:314", 2d)) ModLog.Every("ConfigPage.Slider:314", 2d, $"ConfigPage.Slider: returns {CoreLog.Shown(returnedAt314)} (line 314)"); return returnedAt314; }
    }

    VisualElement Toggle(ConfigEntryBase entry, string key)
    {
        if (CoreLog.Due("ConfigPage.Toggle", 2d)) ModLog.Every("ConfigPage.Toggle", 2d, $"ConfigPage.Toggle(entry={CoreLog.Shown(entry)}, key={CoreLog.Shown(key)})");
        var t = UiControls.Toggle(key, (bool)entry.BoxedValue, on => Write(entry, on));
        t.name = "kydl-toggle";
        // the kit puts the switch before its text; here the text comes first and the switch sits at the right edge (user 2026-10-30, D-061)
        VisualElement sw = t[0], label = t[1];
        t.Insert(0, label);
        label.style.flexGrow = 1;
        sw.style.marginRight = 0; sw.style.marginLeft = 8;
        t.style.paddingLeft = 0; t.style.marginRight = 0;
        { var returnedAt328 = t; if (CoreLog.Due("ConfigPage.Toggle:328", 2d)) ModLog.Every("ConfigPage.Toggle:328", 2d, $"ConfigPage.Toggle: returns {CoreLog.Shown(returnedAt328)} (line 328)"); return returnedAt328; }
    }

    VisualElement Dropdown(ConfigEntryBase entry, IList<string> list)
    {
        if (CoreLog.Due("ConfigPage.Dropdown", 2d)) ModLog.Every("ConfigPage.Dropdown", 2d, $"ConfigPage.Dropdown(entry={CoreLog.Shown(entry)}, list={CoreLog.Shown(list)})");
        Type type = entry.SettingType;
        IList<string> options = type.IsEnum ? Enum.GetNames(type) : list;
        string current = type.IsEnum ? Enum.GetName(type, entry.BoxedValue) : (string)entry.BoxedValue;
        return UiControls.Dropdown(options, current ?? options.FirstOrDefault() ?? "", s => s,
            chosen => Write(entry, type.IsEnum ? Enum.Parse(type, chosen) : chosen));
    }

    // Press-to-rebind (user 2026-10-30, D-075): the button shows the key; a click makes it wait ("Press a key..."), the next key pressed is saved
    // (a shortcut takes the Shift / Ctrl / Alt held with it), Escape leaves the setting as it is. KeyCapture does the waiting.
    VisualElement RebindButton(ConfigEntryBase entry, bool shortcut)
    {
        if (CoreLog.Due("ConfigPage.RebindButton", 2d)) ModLog.Every("ConfigPage.RebindButton", 2d, $"ConfigPage.RebindButton(entry={CoreLog.Shown(entry)}, shortcut={CoreLog.Shown(shortcut)})");
        Func<string> shown = () => shortcut ? ShortcutLabel((KeyboardShortcut)entry.BoxedValue) : KeyCaptureRules.KeyName((KeyCode)entry.BoxedValue);
        Button button = null;
        button = UiControls.Button(shown(), () =>
        {
            button.text = "Press a key...  (Esc cancels)";
            KeyCapture.Begin(shortcut, (main, mods) =>
            {
                Write(entry, shortcut ? (object)new KeyboardShortcut(main, mods.ToArray()) : main);
                button.text = shown();
            }, () => button.text = shown());
        }, false);
        button.name = "kydl-key";
        { var returnedAt358 = button; if (CoreLog.Due("ConfigPage.RebindButton:358", 2d)) ModLog.Every("ConfigPage.RebindButton:358", 2d, $"ConfigPage.RebindButton: returns {CoreLog.Shown(returnedAt358)} (line 358)"); return returnedAt358; }
    }

    static string ShortcutLabel(KeyboardShortcut s)
    {
        if (CoreLog.Due("ConfigPage.ShortcutLabel", 2d)) ModLog.Every("ConfigPage.ShortcutLabel", 2d, $"ConfigPage.ShortcutLabel(s={CoreLog.Shown(s)})");
        { var returnedAt364 = KeyCaptureRules.ShortcutText(s.MainKey, new List<KeyCode>(s.Modifiers)); if (CoreLog.Due("ConfigPage.ShortcutLabel:364", 2d)) ModLog.Every("ConfigPage.ShortcutLabel:364", 2d, $"ConfigPage.ShortcutLabel: returns {CoreLog.Shown(returnedAt364)} (line 364)"); return returnedAt364; }
    }

    VisualElement ColorEditor(ConfigEntryBase entry)
    {
        if (CoreLog.Due("ConfigPage.ColorEditor", 2d)) ModLog.Every("ConfigPage.ColorEditor", 2d, $"ConfigPage.ColorEditor(entry={CoreLog.Shown(entry)})");
        Color now = (Color)entry.BoxedValue;
        Action<string> set;
        VisualElement picker = UiControls.ColorPicker("#" + ColorUtility.ToHtmlStringRGB(now), hex =>
        {
            Color c;
            if (!ColorUtility.TryParseHtmlString(hex, out c)) { if (CoreLog.Due("ConfigPage.ColorEditor:375", 2d)) ModLog.Every("ConfigPage.ColorEditor:375", 2d, "ConfigPage.ColorEditor: returns (line 375)"); return; }
            c.a = ((Color)entry.BoxedValue).a;                                  // the picker has no alpha: keep the entry's
            Write(entry, c);
        }, out set);
        picker.name = "kydl-color";
        { var returnedAt380 = picker; if (CoreLog.Due("ConfigPage.ColorEditor:380", 2d)) ModLog.Every("ConfigPage.ColorEditor:380", 2d, $"ConfigPage.ColorEditor: returns {CoreLog.Shown(returnedAt380)} (line 380)"); return returnedAt380; }
    }

    // --- writing and reading entries --------------------------------------------------------------------------------------
    void Write(ConfigEntryBase entry, object value)
    {
        if (value == null || Equals(entry.BoxedValue, value)) { if (CoreLog.Due("ConfigPage.Write:386", 2d)) ModLog.Every("ConfigPage.Write:386", 2d, "ConfigPage.Write: returns (line 386)"); return; }
        _writing = true;
        _writingEntry = entry;
        try { entry.BoxedValue = value; }
        finally { _writing = false; _writingEntry = null; }
        ModLog.Verbose("ConfigPage: " + entry.Definition.Section + " / " + entry.Definition.Key + " = " + entry.GetSerializedValue());
    }

    static bool Range(ConfigEntryBase entry, out double min, out double max)
    {
        if (CoreLog.Due("ConfigPage.Range", 2d)) ModLog.Every("ConfigPage.Range", 2d, $"ConfigPage.Range(entry={CoreLog.Shown(entry)})");
        min = max = 0;
        AcceptableValueBase av = entry.Description == null ? null : entry.Description.AcceptableValues;
        if (av == null) { if (CoreLog.Due("ConfigPage.Range:399", 2d)) ModLog.Every("ConfigPage.Range:399", 2d, "ConfigPage.Range: returns false (line 399)"); return false; }
        var lo = av.GetType().GetProperty("MinValue");
        var hi = av.GetType().GetProperty("MaxValue");
        if (lo == null || hi == null) { if (CoreLog.Due("ConfigPage.Range:402", 2d)) ModLog.Every("ConfigPage.Range:402", 2d, "ConfigPage.Range: returns false (line 402)"); return false; }
        min = Convert.ToDouble(lo.GetValue(av, null));
        max = Convert.ToDouble(hi.GetValue(av, null));
        { if (CoreLog.Due("ConfigPage.Range:405", 2d)) ModLog.Every("ConfigPage.Range:405", 2d, "ConfigPage.Range: returns true (line 405)"); return true; }
    }

    static IList<string> ListOf(ConfigEntryBase entry)
    {
        if (CoreLog.Due("ConfigPage.ListOf", 2d)) ModLog.Every("ConfigPage.ListOf", 2d, $"ConfigPage.ListOf(entry={CoreLog.Shown(entry)})");
        AcceptableValueBase av = entry.Description == null ? null : entry.Description.AcceptableValues;
        if (av == null) { if (CoreLog.Due("ConfigPage.ListOf:412", 2d)) ModLog.Every("ConfigPage.ListOf:412", 2d, "ConfigPage.ListOf: returns null (line 412)"); return null; }
        var p = av.GetType().GetProperty("AcceptableValues");
        var values = p == null ? null : p.GetValue(av, null) as string[];
        { var returnedAt415 = values; if (CoreLog.Due("ConfigPage.ListOf:415", 2d)) ModLog.Every("ConfigPage.ListOf:415", 2d, $"ConfigPage.ListOf: returns {CoreLog.Shown(returnedAt415)} (line 415)"); return returnedAt415; }
    }
}
