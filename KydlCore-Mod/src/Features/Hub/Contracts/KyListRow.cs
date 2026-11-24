using System;
using System.Collections.Generic;
using KydlCore.Core;
using KydlCore.Common;

namespace KydlCore.Features.Hub.Contracts;

/// <summary>A list of lines (made by <see cref="KySection.List"/>).</summary>
public sealed class KyListRow : KyRow
{
    /// <summary>The lines.</summary>
    public IList<string> Items { get; private set; }
    internal KyListRow(string title, IList<string> items) : base(title) { Items = items ?? new List<string>(); }
}
