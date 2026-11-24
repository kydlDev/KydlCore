using System;
using KydlCore.Common;
using KydlCore.Features.Hub.Domain;
using KydlCore.Features.Hub.Infrastructure;
using UnityEngine.UIElements;
using KydlCore.Core;

namespace KydlCore.Features.Hub.Contracts;

/// <summary>A window opened with <see cref="KyUi.Open"/>.</summary>
public sealed class KyWindow
{
    internal KyWindow(string modGuid, string id, string fullId, VisualElement element, Action onClosed)
    {
        if (CoreLog.Due("KyWindow.KyWindow", 2d)) ModLog.Every("KyWindow.KyWindow", 2d, $"KyWindow.KyWindow(modGuid={CoreLog.Shown(modGuid)}, id={CoreLog.Shown(id)}, fullId={CoreLog.Shown(fullId)}, element={CoreLog.Shown(element)}, onClosed={CoreLog.Shown(onClosed)})");
        ModGuid = modGuid; Id = id; FullId = fullId; Element = element; OnClosed = onClosed;
    }

    /// <summary>The plugin GUID of the mod that owns the window.</summary>
    public string ModGuid { get; private set; }

    /// <summary>The mod's own id of the window.</summary>
    public string Id { get; private set; }

    internal string FullId { get; private set; }
    internal VisualElement Element { get; private set; }
    internal Action OnClosed { get; private set; }

    /// <summary>Closes the window (its `onClosed` runs). Safe to call twice.</summary>
    public void Close() { KyWindows.Remove(FullId, "KyWindow.Close"); }

    /// <summary>Brings the window in front of the others.</summary>
    public void BringToFront() { KyWindows.Front(FullId); }
}
