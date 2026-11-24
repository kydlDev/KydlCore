using System;
using System.Collections.Generic;
using KydlCore.Core;
using KydlCore.Common;

namespace KydlCore.Features.Hub.Contracts;

/// <summary>One row of a <see cref="KySection"/>.</summary>
public abstract class KyRow
{
    /// <summary>The label (the text for a note).</summary>
    public string Label { get; private set; }

    internal KyRow(string label) { Label = label ?? ""; }
}
