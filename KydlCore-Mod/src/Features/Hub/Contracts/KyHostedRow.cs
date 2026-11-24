using System;
using System.Collections.Generic;
using KydlCore.Core;
using KydlCore.Common;

namespace KydlCore.Features.Hub.Contracts;

/// <summary>An element the mod builds (made by <see cref="KySection.Hosted"/>).</summary>
public sealed class KyHostedRow : KyRow
{
    /// <summary>Builds the element.</summary>
    public Func<UnityEngine.UIElements.VisualElement> Build { get; private set; }
    internal KyHostedRow(Func<UnityEngine.UIElements.VisualElement> build) : base("") { Build = build; }
}
