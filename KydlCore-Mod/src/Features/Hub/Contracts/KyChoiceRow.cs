using System;
using System.Collections.Generic;
using KydlCore.Core;
using KydlCore.Common;

namespace KydlCore.Features.Hub.Contracts;

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
