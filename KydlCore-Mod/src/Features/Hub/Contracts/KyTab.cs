using System;
using System.Collections.Generic;
using KydlCore.Common;
using KydlCore.Features.Hub.Infrastructure;
using KydlCore.Presentation.Hub.Pages;
using KydlCore.Ui;
using UnityEngine;
using UnityEngine.UIElements;
using KydlCore.Core;

namespace KydlCore.Features.Hub.Contracts;

/// <summary>A row-2 tab of the KY hub, made by <see cref="KyHub.AddTab"/>.</summary>
public sealed class KyTab
{
    /// <summary>The plugin GUID of the mod that owns the tab.</summary>
    public string ModGuid { get; internal set; }
    /// <summary>The tab id, unique inside the mod.</summary>
    public string Id { get; internal set; }
    /// <summary>The shown name.</summary>
    public string Title { get; internal set; }
    /// <summary>The place in row 2 (smaller first; the same order = the order of registering).</summary>
    public int Order { get; internal set; }

    /// <summary>Builds the page again when this tab is the one on screen (the mod's data changed).</summary>
    // Stryker disable once Block : the body only calls the game-side window (nothing to see while no hub is open)
    public void Refresh()
    {
        ModLog.Verbose("KyHub: Refresh " + ModGuid + " / " + Id);
        // Stryker disable once all : only does something while the hub window is open (game)
        HubHost.Refresh(ModGuid, Id);
    }
}
