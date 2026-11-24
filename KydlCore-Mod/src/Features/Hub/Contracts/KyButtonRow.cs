using System;
using System.Collections.Generic;
using KydlCore.Core;
using KydlCore.Common;

namespace KydlCore.Features.Hub.Contracts;

/// <summary>A button (made by <see cref="KySection.Button"/>).</summary>
public sealed class KyButtonRow : KyRow
{
    /// <summary>Called when it is pressed.</summary>
    public Action Clicked { get; private set; }
    internal KyButtonRow(string label, Action clicked) : base(label) { Clicked = clicked; }
}
