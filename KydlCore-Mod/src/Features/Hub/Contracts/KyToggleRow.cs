using System;
using System.Collections.Generic;
using KydlCore.Core;
using KydlCore.Common;

namespace KydlCore.Features.Hub.Contracts;

/// <summary>A switch (made by <see cref="KySection.Toggle"/>).</summary>
public sealed class KyToggleRow : KyRow
{
    /// <summary>Shown as on.</summary>
    public bool On { get; private set; }
    /// <summary>Called with the new state.</summary>
    public Action<bool> Changed { get; private set; }
    internal KyToggleRow(string label, bool on, Action<bool> changed) : base(label) { On = on; Changed = changed; }
}
