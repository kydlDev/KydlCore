using System;
using System.Collections.Generic;
using KydlCore.Core;
using KydlCore.Common;

namespace KydlCore.Features.Hub.Contracts;

/// <summary>
/// T-098 (user 2026-10-31: "mods inform KydlCore about what they want displayed, and Core displays it based on its settings"): what a mod wants a hub tab to SHOW, as plain data.
/// A mod builds a page (sections of rows) and hands it to <see cref="KyHub.AddPage"/>; KydlCore builds the elements with its own theme and its own text size. A page has no
/// font, colour or size at all - the mod cannot set one. The page builder is called again every few seconds; a page whose <see cref="State"/> text equals the one on screen
/// is NOT rebuilt (no flicker, an open drop-down stays open), a different State replaces the content.
/// </summary>
public sealed class KyPage
{
    /// <summary>A text that changes exactly when something SHOWN changes (e.g. the values joined). Empty = rebuilt on every refresh.</summary>
    public string State = "";

    /// <summary>The sections, top to bottom.</summary>
    public List<KySection> Sections { get; private set; }

    /// <summary>A new, empty page.</summary>
    public KyPage() { Sections = new List<KySection>(); }

    /// <summary>Adds a section with a heading (empty title = no heading) and returns it to add rows to.</summary>
    public KySection Section(string title)
    {
        if (CoreLog.On) ModLog.Verbose($"KyPage.Section(title={CoreLog.Shown(title)})");
        var s = new KySection(title);
        Sections.Add(s);
        { var returnedAt31 = s; if (CoreLog.On) ModLog.Verbose($"KyPage.Section: returns {CoreLog.Shown(returnedAt31)} (line 31)"); return returnedAt31; }
    }
}
