using System;
using System.Collections.Generic;
using KydlCore.Core;
using KydlCore.Common;

namespace KydlCore.Features.Hub.Contracts;

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
