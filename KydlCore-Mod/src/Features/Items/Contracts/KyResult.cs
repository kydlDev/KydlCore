using System;
using UnityEngine;

namespace KydlCore.Features.Items.Contracts;

/// <summary>The answer of <see cref="KyItems.Contribute"/>: accepted (with the normalised id) or rejected with the reason.</summary>
public sealed class KyResult
{
    /// <summary>True when the contribution was taken into the registry.</summary>
    public bool Accepted { get; private set; }
    /// <summary>The normalised id ("&lt;modid&gt;:&lt;item&gt;").</summary>
    public string Id { get; private set; }
    /// <summary>Why it was rejected, or a note on an accepted one ("" when there is none).</summary>
    public string Reason { get; private set; }
    KyResult() { }
    internal static KyResult Ok(string id, string reason = "") { return new KyResult { Accepted = true, Id = id ?? "", Reason = reason ?? "" }; }
    internal static KyResult Rejected(string id, string reason) { return new KyResult { Accepted = false, Id = id ?? "", Reason = reason ?? "" }; }
    /// <summary>"accepted &lt;id&gt;" / "rejected &lt;id&gt;: &lt;reason&gt;".</summary>
    public override string ToString() { return (Accepted ? "accepted " : "rejected ") + Id + (string.IsNullOrEmpty(Reason) ? "" : $": {Reason}"); }
}
