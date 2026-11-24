using System;
using System.Collections.Generic;
using KydlCore.Core;
using KydlCore.Common;

namespace KydlCore.Features.Hub.Contracts;

/// <summary>A block of rows under one heading.</summary>
public sealed class KySection
{
    /// <summary>The heading ("" = none).</summary>
    public string Title { get; private set; }
    /// <summary>The rows, top to bottom.</summary>
    public IReadOnlyList<KyRow> Rows => _rows;

    // read-only from outside (user 2026-11-24): a mod adds rows only through Value(...), Note(...), Slider(...) and the others
    readonly List<KyRow> _rows = new List<KyRow>();

    /// <summary>Made by <see cref="KyPage.Section"/>.</summary>
    internal KySection(string title) { Title = title ?? ""; }

    /// <summary>A label and its value; where the value changes with quality, maxValue is shown in a second column.</summary>
    public KySection Value(string label, string value, string maxValue = "")
    {
        if (CoreLog.On) ModLog.Verbose($"KySection.Value(label={CoreLog.Shown(label)}, value={CoreLog.Shown(value)}, maxValue={CoreLog.Shown(maxValue)})");
        _rows.Add(new KyValueRow(label, value, maxValue));
        { var returnedAt27 = this; if (CoreLog.On) ModLog.Verbose($"KySection.Value: returns {CoreLog.Shown(returnedAt27)} (line 27)"); return returnedAt27; }
    }

    /// <summary>A small header over the value columns of the rows below (e.g. "Quality 1" | "Quality 4"). Use it before rows that have a maxValue.</summary>
    public KySection Columns(string value, string maxValue)
    {
        if (CoreLog.On) ModLog.Verbose($"KySection.Columns(value={CoreLog.Shown(value)}, maxValue={CoreLog.Shown(maxValue)})");
        _rows.Add(new KyColumnsRow(value, maxValue));
        { var returnedAt35 = this; if (CoreLog.On) ModLog.Verbose($"KySection.Columns: returns {CoreLog.Shown(returnedAt35)} (line 35)"); return returnedAt35; }
    }

    /// <summary>
    /// A element the mod brings itself - meant for a 3D viewport (a model view cannot be described as rows). KydlCore adds it, applies its text size to everything in it in the
    /// same call, and builds it again only when the page State changes (so the view keeps its rotation while the State stays). Not for text: describe text with the rows.
    /// </summary>
    public KySection Hosted(Func<UnityEngine.UIElements.VisualElement> build)
    {
        if (CoreLog.On) ModLog.Verbose($"KySection.Hosted(build={CoreLog.Shown(build)})");
        _rows.Add(new KyHostedRow(build));
        { var returnedAt46 = this; if (CoreLog.On) ModLog.Verbose($"KySection.Hosted: returns {CoreLog.Shown(returnedAt46)} (line 46)"); return returnedAt46; }
    }

    /// <summary>A line of explaining text.</summary>
    public KySection Note(string text)
    {
        if (CoreLog.Due("KySection.Note", 2d)) ModLog.Every("KySection.Note", 2d, $"KySection.Note(text={CoreLog.Shown(text)})");
        _rows.Add(new KyNoteRow(text));
        { var returnedAt54 = this; if (CoreLog.Due("KySection.Note:54", 2d)) ModLog.Every("KySection.Note:54", 2d, $"KySection.Note: returns {CoreLog.Shown(returnedAt54)} (line 54)"); return returnedAt54; }
    }

    /// <summary>A label and a drop-down of choices; `chosen` gets the id of the picked one (the page is built again right after).</summary>
    public KySection Choice(string label, IList<KyChoice> choices, string selected, Action<string> chosen)
    {
        if (CoreLog.On) ModLog.Verbose($"KySection.Choice(label={CoreLog.Shown(label)}, choices={CoreLog.Shown(choices)}, selected={CoreLog.Shown(selected)}, chosen={CoreLog.Shown(chosen)})");
        _rows.Add(new KyChoiceRow(label, choices, selected, chosen));
        { var returnedAt62 = this; if (CoreLog.On) ModLog.Verbose($"KySection.Choice: returns {CoreLog.Shown(returnedAt62)} (line 62)"); return returnedAt62; }
    }

    /// <summary>A table: a header line and one line per row, each cell a text, a button or a drop-down (see <see cref="KyCell"/>). Texts may change between builds without the State changing (they are updated in place); the rows and cell kinds may not.</summary>
    public KySection Table(IList<string> headers, IList<IList<KyCell>> rows)
    {
        if (CoreLog.On) ModLog.Verbose($"KySection.Table(headers={CoreLog.Shown(headers)}, rows={CoreLog.Shown(rows)})");
        _rows.Add(new KyTableRow(headers, rows));
        { var returnedAt70 = this; if (CoreLog.On) ModLog.Verbose($"KySection.Table: returns {CoreLog.Shown(returnedAt70)} (line 70)"); return returnedAt70; }
    }

    /// <summary>A switch with a label; `changed` gets the new state. The page is NOT rebuilt by the change (the switch shows it); put what the switch changes into the page State.</summary>
    public KySection Toggle(string label, bool on, Action<bool> changed)
    {
        if (CoreLog.Due("KySection.Toggle", 2d)) ModLog.Every("KySection.Toggle", 2d, $"KySection.Toggle(label={CoreLog.Shown(label)}, on={CoreLog.Shown(on)}, changed={CoreLog.Shown(changed)})");
        _rows.Add(new KyToggleRow(label, on, changed));
        { var returnedAt78 = this; if (CoreLog.Due("KySection.Toggle:78", 2d)) ModLog.Every("KySection.Toggle:78", 2d, $"KySection.Toggle: returns {CoreLog.Shown(returnedAt78)} (line 78)"); return returnedAt78; }
    }

    /// <summary>A slider with a label; `format` is the .NET number format of the shown value (e.g. "0.0"). `changed` is called while it is dragged; the page is NOT rebuilt by it.</summary>
    public KySection Slider(string label, float min, float max, float value, string format, Action<float> changed)
    {
        if (CoreLog.Due("KySection.Slider", 2d)) ModLog.Every("KySection.Slider", 2d, $"KySection.Slider(label={CoreLog.Shown(label)}, min={CoreLog.Shown(min)}, max={CoreLog.Shown(max)}, value={CoreLog.Shown(value)}, format={CoreLog.Shown(format)}, changed={CoreLog.Shown(changed)})");
        _rows.Add(new KySliderRow(label, min, max, value, format, changed));
        { var returnedAt86 = this; if (CoreLog.Due("KySection.Slider:86", 2d)) ModLog.Every("KySection.Slider:86", 2d, $"KySection.Slider: returns {CoreLog.Shown(returnedAt86)} (line 86)"); return returnedAt86; }
    }

    /// <summary>A text box with a label; `changed` gets the new text. The page is NOT rebuilt by the change.</summary>
    public KySection TextBox(string label, string value, int maxLength, Action<string> changed)
    {
        if (CoreLog.On) ModLog.Verbose($"KySection.TextBox(label={CoreLog.Shown(label)}, value={CoreLog.Shown(value)}, maxLength={CoreLog.Shown(maxLength)}, changed={CoreLog.Shown(changed)})");
        _rows.Add(new KyTextRow(label, value, maxLength, changed));
        { var returnedAt94 = this; if (CoreLog.On) ModLog.Verbose($"KySection.TextBox: returns {CoreLog.Shown(returnedAt94)} (line 94)"); return returnedAt94; }
    }

    /// <summary>A list of text lines (one bullet each); `title` is a small caption above it ("" = none).</summary>
    public KySection List(string title, IList<string> items)
    {
        if (CoreLog.On) ModLog.Verbose($"KySection.List(title={CoreLog.Shown(title)}, items={CoreLog.Shown(items)})");
        _rows.Add(new KyListRow(title, items));
        { var returnedAt102 = this; if (CoreLog.On) ModLog.Verbose($"KySection.List: returns {CoreLog.Shown(returnedAt102)} (line 102)"); return returnedAt102; }
    }

    /// <summary>A button with a label.</summary>
    public KySection Button(string label, Action clicked)
    {
        if (CoreLog.Due("KySection.Button", 2d)) ModLog.Every("KySection.Button", 2d, $"KySection.Button(label={CoreLog.Shown(label)}, clicked={CoreLog.Shown(clicked)})");
        _rows.Add(new KyButtonRow(label, clicked));
        { var returnedAt110 = this; if (CoreLog.Due("KySection.Button:110", 2d)) ModLog.Every("KySection.Button:110", 2d, $"KySection.Button: returns {CoreLog.Shown(returnedAt110)} (line 110)"); return returnedAt110; }
    }
}
