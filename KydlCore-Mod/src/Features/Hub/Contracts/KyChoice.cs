using System;
using System.Collections.Generic;
using KydlCore.Core;
using KydlCore.Common;

namespace KydlCore.Features.Hub.Contracts;

/// <summary>One entry of a drop-down.</summary>
public sealed class KyChoice
{
    /// <summary>What the mod identifies it by.</summary>
    public string Id { get; private set; }
    /// <summary>What the player reads.</summary>
    public string Text { get; private set; }
    /// <summary>Makes a choice.</summary>
    public KyChoice(string id, string text) { Id = id ?? ""; Text = text ?? ""; }
}
