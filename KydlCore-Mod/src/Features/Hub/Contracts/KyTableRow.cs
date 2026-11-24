using System;
using System.Collections.Generic;
using KydlCore.Core;
using KydlCore.Common;

namespace KydlCore.Features.Hub.Contracts;

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
