using System;
using System.Collections.Generic;
using KydlCore.Core;
using KydlCore.Common;

namespace KydlCore.Features.Hub.Contracts;

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
