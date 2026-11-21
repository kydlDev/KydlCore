using System;
using System.Collections.Generic;
using KydlCore.Core;
using KydlCore.Common;

namespace KydlCore.Features.Hub.Contracts;

/// <summary>
/// T-098 (user 2026-10-31: "mods inform KydlCore about what they want displayed, and Core displays it based on its settings"): what a mod wants a hub tab to SHOW, as plain data.
/// A mod builds a page (sections of rows) and hands it to <see cref="KyHub.AddPage"/>; KydlCore builds the elements with its own theme and its own text size. A page has no
/// font, colour or size at all - the mod cannot set one. The page builder is called again every few seconds; a page whose <see cref="State"/> text equals the one on screen
/// is NOT rebuilt (no flicker, an open drop-down stays open), a different State replaces the content.
/// </summary>
public sealed class KyPage
{
    /// <summary>A text that changes exactly when something SHOWN changes (e.g. the values joined). Empty = rebuilt on every refresh.</summary>
    public string State = "";

    /// <summary>The sections, top to bottom.</summary>
    public List<KySection> Sections { get; private set; }

    /// <summary>A new, empty page.</summary>
    public KyPage() { Sections = new List<KySection>(); }

    /// <summary>Adds a section with a heading (empty title = no heading) and returns it to add rows to.</summary>
    public KySection Section(string title)
    {
        if (CoreLog.On) ModLog.Verbose($"KyPage.Section(title={CoreLog.Shown(title)})");
        var s = new KySection(title);
        Sections.Add(s);
        { var returnedAt31 = s; if (CoreLog.On) ModLog.Verbose($"KyPage.Section: returns {CoreLog.Shown(returnedAt31)} (line 31)"); return returnedAt31; }
    }
}

/// <summary>A block of rows under one heading.</summary>
public sealed class KySection
{
    /// <summary>The heading ("" = none).</summary>
    public string Title { get; private set; }
    /// <summary>The rows, top to bottom.</summary>
    public List<KyRow> Rows { get; private set; }

    /// <summary>Made by <see cref="KyPage.Section"/>.</summary>
    internal KySection(string title) { Title = title ?? ""; Rows = new List<KyRow>(); }

    /// <summary>A label and its value; where the value changes with quality, maxValue is shown in a second column.</summary>
    public KySection Value(string label, string value, string maxValue = "")
    {
        if (CoreLog.On) ModLog.Verbose($"KySection.Value(label={CoreLog.Shown(label)}, value={CoreLog.Shown(value)}, maxValue={CoreLog.Shown(maxValue)})");
        Rows.Add(new KyValueRow(label, value, maxValue));
        { var returnedAt51 = this; if (CoreLog.On) ModLog.Verbose($"KySection.Value: returns {CoreLog.Shown(returnedAt51)} (line 51)"); return returnedAt51; }
    }

    /// <summary>A small header over the value columns of the rows below (e.g. "Quality 1" | "Quality 4"). Use it before rows that have a maxValue.</summary>
    public KySection Columns(string value, string maxValue)
    {
        if (CoreLog.On) ModLog.Verbose($"KySection.Columns(value={CoreLog.Shown(value)}, maxValue={CoreLog.Shown(maxValue)})");
        Rows.Add(new KyColumnsRow(value, maxValue));
        { var returnedAt59 = this; if (CoreLog.On) ModLog.Verbose($"KySection.Columns: returns {CoreLog.Shown(returnedAt59)} (line 59)"); return returnedAt59; }
    }

    /// <summary>
    /// A element the mod brings itself - meant for a 3D viewport (a model view cannot be described as rows). KydlCore adds it, applies its text size to everything in it in the
    /// same call, and builds it again only when the page State changes (so the view keeps its rotation while the State stays). Not for text: describe text with the rows.
    /// </summary>
    public KySection Hosted(Func<UnityEngine.UIElements.VisualElement> build)
    {
        if (CoreLog.On) ModLog.Verbose($"KySection.Hosted(build={CoreLog.Shown(build)})");
        Rows.Add(new KyHostedRow(build));
        { var returnedAt70 = this; if (CoreLog.On) ModLog.Verbose($"KySection.Hosted: returns {CoreLog.Shown(returnedAt70)} (line 70)"); return returnedAt70; }
    }

    /// <summary>A line of explaining text.</summary>
    public KySection Note(string text)
    {
        if (CoreLog.Due("KySection.Note", 2d)) ModLog.Every("KySection.Note", 2d, $"KySection.Note(text={CoreLog.Shown(text)})");
        Rows.Add(new KyNoteRow(text));
        { var returnedAt78 = this; if (CoreLog.Due("KySection.Note:78", 2d)) ModLog.Every("KySection.Note:78", 2d, $"KySection.Note: returns {CoreLog.Shown(returnedAt78)} (line 78)"); return returnedAt78; }
    }

    /// <summary>A label and a drop-down of choices; `chosen` gets the id of the picked one (the page is built again right after).</summary>
    public KySection Choice(string label, IList<KyChoice> choices, string selected, Action<string> chosen)
    {
        if (CoreLog.On) ModLog.Verbose($"KySection.Choice(label={CoreLog.Shown(label)}, choices={CoreLog.Shown(choices)}, selected={CoreLog.Shown(selected)}, chosen={CoreLog.Shown(chosen)})");
        Rows.Add(new KyChoiceRow(label, choices, selected, chosen));
        { var returnedAt86 = this; if (CoreLog.On) ModLog.Verbose($"KySection.Choice: returns {CoreLog.Shown(returnedAt86)} (line 86)"); return returnedAt86; }
    }

    /// <summary>A table: a header line and one line per row, each cell a text, a button or a drop-down (see <see cref="KyCell"/>). Texts may change between builds without the State changing (they are updated in place); the rows and cell kinds may not.</summary>
    public KySection Table(IList<string> headers, IList<IList<KyCell>> rows)
    {
        if (CoreLog.On) ModLog.Verbose($"KySection.Table(headers={CoreLog.Shown(headers)}, rows={CoreLog.Shown(rows)})");
        Rows.Add(new KyTableRow(headers, rows));
        { var returnedAt94 = this; if (CoreLog.On) ModLog.Verbose($"KySection.Table: returns {CoreLog.Shown(returnedAt94)} (line 94)"); return returnedAt94; }
    }

    /// <summary>A switch with a label; `changed` gets the new state. The page is NOT rebuilt by the change (the switch shows it); put what the switch changes into the page State.</summary>
    public KySection Toggle(string label, bool on, Action<bool> changed)
    {
        if (CoreLog.Due("KySection.Toggle", 2d)) ModLog.Every("KySection.Toggle", 2d, $"KySection.Toggle(label={CoreLog.Shown(label)}, on={CoreLog.Shown(on)}, changed={CoreLog.Shown(changed)})");
        Rows.Add(new KyToggleRow(label, on, changed));
        { var returnedAt102 = this; if (CoreLog.Due("KySection.Toggle:102", 2d)) ModLog.Every("KySection.Toggle:102", 2d, $"KySection.Toggle: returns {CoreLog.Shown(returnedAt102)} (line 102)"); return returnedAt102; }
    }

    /// <summary>A slider with a label; `format` is the .NET number format of the shown value (e.g. "0.0"). `changed` is called while it is dragged; the page is NOT rebuilt by it.</summary>
    public KySection Slider(string label, float min, float max, float value, string format, Action<float> changed)
    {
        if (CoreLog.Due("KySection.Slider", 2d)) ModLog.Every("KySection.Slider", 2d, $"KySection.Slider(label={CoreLog.Shown(label)}, min={CoreLog.Shown(min)}, max={CoreLog.Shown(max)}, value={CoreLog.Shown(value)}, format={CoreLog.Shown(format)}, changed={CoreLog.Shown(changed)})");
        Rows.Add(new KySliderRow(label, min, max, value, format, changed));
        { var returnedAt110 = this; if (CoreLog.Due("KySection.Slider:110", 2d)) ModLog.Every("KySection.Slider:110", 2d, $"KySection.Slider: returns {CoreLog.Shown(returnedAt110)} (line 110)"); return returnedAt110; }
    }

    /// <summary>A text box with a label; `changed` gets the new text. The page is NOT rebuilt by the change.</summary>
    public KySection TextBox(string label, string value, int maxLength, Action<string> changed)
    {
        if (CoreLog.On) ModLog.Verbose($"KySection.TextBox(label={CoreLog.Shown(label)}, value={CoreLog.Shown(value)}, maxLength={CoreLog.Shown(maxLength)}, changed={CoreLog.Shown(changed)})");
        Rows.Add(new KyTextRow(label, value, maxLength, changed));
        { var returnedAt118 = this; if (CoreLog.On) ModLog.Verbose($"KySection.TextBox: returns {CoreLog.Shown(returnedAt118)} (line 118)"); return returnedAt118; }
    }

    /// <summary>A list of text lines (one bullet each); `title` is a small caption above it ("" = none).</summary>
    public KySection List(string title, IList<string> items)
    {
        if (CoreLog.On) ModLog.Verbose($"KySection.List(title={CoreLog.Shown(title)}, items={CoreLog.Shown(items)})");
        Rows.Add(new KyListRow(title, items));
        { var returnedAt126 = this; if (CoreLog.On) ModLog.Verbose($"KySection.List: returns {CoreLog.Shown(returnedAt126)} (line 126)"); return returnedAt126; }
    }

    /// <summary>A button with a label.</summary>
    public KySection Button(string label, Action clicked)
    {
        if (CoreLog.Due("KySection.Button", 2d)) ModLog.Every("KySection.Button", 2d, $"KySection.Button(label={CoreLog.Shown(label)}, clicked={CoreLog.Shown(clicked)})");
        Rows.Add(new KyButtonRow(label, clicked));
        { var returnedAt134 = this; if (CoreLog.Due("KySection.Button:134", 2d)) ModLog.Every("KySection.Button:134", 2d, $"KySection.Button: returns {CoreLog.Shown(returnedAt134)} (line 134)"); return returnedAt134; }
    }
}

/// <summary>One row of a <see cref="KySection"/>.</summary>
public abstract class KyRow
{
    /// <summary>The label (the text for a note).</summary>
    public string Label { get; private set; }

    internal KyRow(string label) { Label = label ?? ""; }
}

/// <summary>Label and value (made by <see cref="KySection.Value"/>).</summary>
public sealed class KyValueRow : KyRow
{
    /// <summary>The value.</summary>
    public string Value { get; private set; }
    /// <summary>The value at max quality ("" = the value does not change with quality).</summary>
    public string MaxValue { get; private set; }
    internal KyValueRow(string label, string value, string maxValue) : base(label) { Value = value ?? ""; MaxValue = maxValue ?? ""; }
}

/// <summary>Explaining text (made by <see cref="KySection.Note"/>).</summary>
public sealed class KyNoteRow : KyRow
{
    internal KyNoteRow(string text) : base(text) { }
}

/// <summary>One entry of a drop-down.</summary>
public sealed class KyChoice
{
    /// <summary>What the mod identifies it by.</summary>
    public string Id { get; private set; }
    /// <summary>What the player reads.</summary>
    public string Text { get; private set; }
    /// <summary>Makes a choice.</summary>
    public KyChoice(string id, string text) { Id = id ?? ""; Text = text ?? ""; }
}

/// <summary>A drop-down (made by <see cref="KySection.Choice"/>).</summary>
public sealed class KyChoiceRow : KyRow
{
    /// <summary>The entries.</summary>
    public IList<KyChoice> Choices { get; private set; }
    /// <summary>The id shown as picked.</summary>
    public string Selected { get; private set; }
    /// <summary>Called with the id of the picked entry.</summary>
    public Action<string> Chosen { get; private set; }
    internal KyChoiceRow(string label, IList<KyChoice> choices, string selected, Action<string> chosen) : base(label)
    {
        if (CoreLog.On) ModLog.Verbose($"KyChoiceRow.KyChoiceRow(label={CoreLog.Shown(label)}, choices={CoreLog.Shown(choices)}, selected={CoreLog.Shown(selected)})");
        Choices = choices ?? new List<KyChoice>(); Selected = selected ?? ""; Chosen = chosen;
    }
}

/// <summary>A button (made by <see cref="KySection.Button"/>).</summary>
public sealed class KyButtonRow : KyRow
{
    /// <summary>Called when it is pressed.</summary>
    public Action Clicked { get; private set; }
    internal KyButtonRow(string label, Action clicked) : base(label) { Clicked = clicked; }
}

/// <summary>One cell of a table row. Made with <see cref="Text"/>, <see cref="Button"/> or <see cref="Drop"/>.</summary>
public sealed class KyCell
{
    internal enum Kinds { Text, Button, Drop }
    internal Kinds Kind;
    /// <summary>True when the text may hold rich-text tags (colours, emoji) - e.g. a player-styled name.</summary>
    public bool IsRich { get; private set; }
    /// <summary>The text (or the button label).</summary>
    public string Text { get; private set; }
    /// <summary>Called when the button is pressed (after the confirmation, if there is one).</summary>
    public Action Clicked { get; private set; }
    /// <summary>The question asked in the cell before a button acts ("" = acts at once).</summary>
    public string ConfirmText { get; private set; }
    /// <summary>The label of the confirming button.</summary>
    public string YesText { get; private set; }
    /// <summary>The label of the button that takes it back.</summary>
    public string NoText { get; private set; }
    /// <summary>The drop-down entries.</summary>
    public IList<KyChoice> Choices { get; private set; }
    /// <summary>The id shown as picked in a drop-down.</summary>
    public string Selected { get; private set; }
    /// <summary>Called with the picked id.</summary>
    public Action<string> Chosen { get; private set; }

    KyCell() { Text = ""; ConfirmText = ""; YesText = ""; NoText = ""; Selected = ""; }

    /// <summary>A text cell.</summary>
    public static KyCell Label(string text) { return new KyCell { Kind = Kinds.Text, Text = text ?? "" }; }

    /// <summary>A text cell that shows rich-text tags (a player-styled name).</summary>
    public static KyCell Rich(string text) { return new KyCell { Kind = Kinds.Text, Text = text ?? "", IsRich = true }; }

    /// <summary>A button cell.</summary>
    public static KyCell Button(string label, Action clicked) { return new KyCell { Kind = Kinds.Button, Text = label ?? "", Clicked = clicked }; }

    /// <summary>A drop-down cell.</summary>
    public static KyCell Drop(IList<KyChoice> choices, string selected, Action<string> chosen)
    {
        if (CoreLog.On) ModLog.Verbose($"KyCell.Drop(choices={CoreLog.Shown(choices)}, selected={CoreLog.Shown(selected)}, chosen={CoreLog.Shown(chosen)})");
        { var returnedAt237 = new KyCell { Kind = Kinds.Drop, Choices = choices ?? new List<KyChoice>(), Selected = selected ?? "", Chosen = chosen }; if (CoreLog.On) ModLog.Verbose($"KyCell.Drop: returns {CoreLog.Shown(returnedAt237)} (line 237)"); return returnedAt237; }
    }

    /// <summary>Makes this button ask `question` with Yes / No buttons in its place before it acts. Returns the cell.</summary>
    public KyCell Confirm(string question, string yes, string no)
    {
        if (CoreLog.On) ModLog.Verbose($"KyCell.Confirm(question={CoreLog.Shown(question)}, yes={CoreLog.Shown(yes)}, no={CoreLog.Shown(no)})");
        ConfirmText = question ?? ""; YesText = yes ?? ""; NoText = no ?? "";
        { var returnedAt245 = this; if (CoreLog.On) ModLog.Verbose($"KyCell.Confirm: returns {CoreLog.Shown(returnedAt245)} (line 245)"); return returnedAt245; }
    }
}

/// <summary>A table (made by <see cref="KySection.Table"/>).</summary>
public sealed class KyTableRow : KyRow
{
    /// <summary>The header texts.</summary>
    public IList<string> Headers { get; private set; }
    /// <summary>The rows of cells.</summary>
    public IList<IList<KyCell>> Cells { get; private set; }
    internal KyTableRow(IList<string> headers, IList<IList<KyCell>> cells) : base("")
    {
        if (CoreLog.On) ModLog.Verbose($"KyTableRow.KyTableRow(headers={CoreLog.Shown(headers)})");
        Headers = headers ?? new List<string>(); Cells = cells ?? new List<IList<KyCell>>();
    }
}

/// <summary>The header of the value columns (made by <see cref="KySection.Columns"/>).</summary>
public sealed class KyColumnsRow : KyRow
{
    /// <summary>The header over the value column.</summary>
    public string Value { get; private set; }
    /// <summary>The header over the max-quality column.</summary>
    public string MaxValue { get; private set; }
    internal KyColumnsRow(string value, string maxValue) : base("") { Value = value ?? ""; MaxValue = maxValue ?? ""; }
}

/// <summary>An element the mod builds (made by <see cref="KySection.Hosted"/>).</summary>
public sealed class KyHostedRow : KyRow
{
    /// <summary>Builds the element.</summary>
    public Func<UnityEngine.UIElements.VisualElement> Build { get; private set; }
    internal KyHostedRow(Func<UnityEngine.UIElements.VisualElement> build) : base("") { Build = build; }
}

/// <summary>A switch (made by <see cref="KySection.Toggle"/>).</summary>
public sealed class KyToggleRow : KyRow
{
    /// <summary>Shown as on.</summary>
    public bool On { get; private set; }
    /// <summary>Called with the new state.</summary>
    public Action<bool> Changed { get; private set; }
    internal KyToggleRow(string label, bool on, Action<bool> changed) : base(label) { On = on; Changed = changed; }
}

/// <summary>A slider (made by <see cref="KySection.Slider"/>).</summary>
public sealed class KySliderRow : KyRow
{
    /// <summary>Lowest value.</summary>
    public float Min { get; private set; }
    /// <summary>Highest value.</summary>
    public float Max { get; private set; }
    /// <summary>The value shown.</summary>
    public float Value { get; private set; }
    /// <summary>The .NET number format of the shown value.</summary>
    public string Format { get; private set; }
    /// <summary>Called with the new value.</summary>
    public Action<float> Changed { get; private set; }
    internal KySliderRow(string label, float min, float max, float value, string format, Action<float> changed) : base(label)
    {
        if (CoreLog.Due("KySliderRow.KySliderRow", 2d)) ModLog.Every("KySliderRow.KySliderRow", 2d, $"KySliderRow.KySliderRow(label={CoreLog.Shown(label)}, min={CoreLog.Shown(min)}, max={CoreLog.Shown(max)}, value={CoreLog.Shown(value)}, format={CoreLog.Shown(format)})");
        Min = min; Max = max < min ? min : max; Value = value; Format = string.IsNullOrEmpty(format) ? "0.##" : format; Changed = changed;
    }
}

/// <summary>A text box (made by <see cref="KySection.TextBox"/>).</summary>
public sealed class KyTextRow : KyRow
{
    /// <summary>The text shown.</summary>
    public string Value { get; private set; }
    /// <summary>Longest text the box takes.</summary>
    public int MaxLength { get; private set; }
    /// <summary>Called with the new text.</summary>
    public Action<string> Changed { get; private set; }
    internal KyTextRow(string label, string value, int maxLength, Action<string> changed) : base(label) { Value = value ?? ""; MaxLength = maxLength < 1 ? 64 : maxLength; Changed = changed; }
}

/// <summary>A list of lines (made by <see cref="KySection.List"/>).</summary>
public sealed class KyListRow : KyRow
{
    /// <summary>The lines.</summary>
    public IList<string> Items { get; private set; }
    internal KyListRow(string title, IList<string> items) : base(title) { Items = items ?? new List<string>(); }
}
