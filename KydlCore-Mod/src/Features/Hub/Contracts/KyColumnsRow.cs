using System;
using System.Collections.Generic;
using KydlCore.Core;
using KydlCore.Common;

namespace KydlCore.Features.Hub.Contracts;

/// <summary>The header of the value columns (made by <see cref="KySection.Columns"/>).</summary>
public sealed class KyColumnsRow : KyRow
{
    /// <summary>The header over the value column.</summary>
    public string Value { get; private set; }
    /// <summary>The header over the max-quality column.</summary>
    public string MaxValue { get; private set; }
    internal KyColumnsRow(string value, string maxValue) : base("") { Value = value ?? ""; MaxValue = maxValue ?? ""; }
}
