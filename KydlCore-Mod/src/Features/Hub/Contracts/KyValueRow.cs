using System;
using System.Collections.Generic;
using KydlCore.Core;
using KydlCore.Common;

namespace KydlCore.Features.Hub.Contracts;

/// <summary>Label and value (made by <see cref="KySection.Value"/>).</summary>
public sealed class KyValueRow : KyRow
{
    /// <summary>The value.</summary>
    public string Value { get; private set; }
    /// <summary>The value at max quality ("" = the value does not change with quality).</summary>
    public string MaxValue { get; private set; }
    internal KyValueRow(string label, string value, string maxValue) : base(label) { Value = value ?? ""; MaxValue = maxValue ?? ""; }
}
