using System;
using KydlCore.Common;
using KydlCore.Features.Hub.Domain;
using KydlCore.Features.Hub.Infrastructure;
using UnityEngine.UIElements;
using KydlCore.Core;

namespace KydlCore.Features.Hub.Contracts;

/// <summary>
/// Windows OUTSIDE the hub (DESIGN 3, D-019, PLAN T-038): a window tied to something in the world, like ChestDecor's chest editor, opened by the
/// interaction (never by a mod hotkey of its own). Everything else is a tab in <see cref="KyHub"/>. Core owns what must exist once: the UI root,
/// the window order, the input lock (held while ANY Core window is open) and Escape (closes only the front window). The mod builds the window
/// content with the shared kit (<c>KydlCore.Ui.UiKit.Window</c> ...), passes it to <see cref="Open"/> and cleans up in `onClosed`.
/// </summary>
public static class KyUi
{
    /// <summary>The one UI root of the game's KydlDev UI (null while the UI is not available yet).</summary>
    public static VisualElement Root { get { return global::KydlCore.Ui.UiKit.Root; } }

    /// <summary>True while any Core window (the hub included) is open: the game takes no input then.</summary>
    public static bool AnyOpen { get { return KyWindows.Stack.AnyOpen; } }

    /// <summary>
    /// Puts `window` on the UI root, in front, and holds the input lock. A window with the same GUID and id that is open already is closed first
    /// (its `onClosed` runs). `onClosed` runs once when the window closes by any way (Escape, <see cref="Close"/>, <see cref="KyWindow.Close"/>);
    /// the window element is already removed then. Returns null (and logs why) for a blank GUID or id, a null window or when the UI is not available yet.
    /// </summary>
    public static KyWindow Open(string modGuid, string id, VisualElement window, Action onClosed = null)
    {
        if (CoreLog.Due("KyUi.Open", 2d)) ModLog.Every("KyUi.Open", 2d, $"KyUi.Open(modGuid={CoreLog.Shown(modGuid)}, id={CoreLog.Shown(id)}, window={CoreLog.Shown(window)}, onClosed={CoreLog.Shown(onClosed)})");
        { var returnedAt32 = KyWindows.Add(modGuid, id, window, onClosed); if (CoreLog.Due("KyUi.Open:32", 2d)) ModLog.Every("KyUi.Open:32", 2d, $"KyUi.Open: returns {CoreLog.Shown(returnedAt32)} (line 32)"); return returnedAt32; }
    }

    /// <summary>
    /// The same, but Core runs `build` itself, as the mod (D-086): the shared kit's log lines of every control in the window (buttons, switches,
    /// sliders, drop-downs) then carry the mod's tag and follow the mod's [DebugLog] Enable, like a hub tab. Prefer this overload. A null or
    /// throwing `build` is logged and gives null.
    /// </summary>
    public static KyWindow Open(string modGuid, string id, Func<VisualElement> build, Action onClosed = null)
    {
        if (build == null) { ModLog.Warn($"KyUi.Open: refused (guid '{modGuid}', id '{id}', no window builder)"); return null; }
        VisualElement window;
        try { window = global::KydlCore.Ui.UiLog.Within(modGuid, build); }
        // Stryker disable once all : the only effect of the handler is the warning (the null result is tested)
        catch (Exception e) { ModLog.Warn($"KyUi.Open: the window builder of '{modGuid}' / '{id}' threw {e.GetType().Name}: {e.Message}"); return null; }
        ModLog.Verbose($"KyUi.Open: built '{id}' of '{modGuid}' as that mod (window {(window == null ? "null" : "given")})");
        return KyWindows.Add(modGuid, id, window, onClosed);
    }

    /// <summary>Closes a window of a mod (its `onClosed` runs). False when it was not open.</summary>
    public static bool Close(string modGuid, string id) { return KyWindows.Remove(KyWindowId.Make(modGuid, id), "Close"); }

    /// <summary>Is the window of this mod and id open?</summary>
    public static bool IsOpen(string modGuid, string id)
    {
        if (CoreLog.Due("KyUi.IsOpen", 2d)) ModLog.Every("KyUi.IsOpen", 2d, $"KyUi.IsOpen(modGuid={CoreLog.Shown(modGuid)}, id={CoreLog.Shown(id)})");
        string full = KyWindowId.Make(modGuid, id);
        { var returnedAt59 = full != null && KyWindows.Stack.IsOpen(full); if (CoreLog.Due("KyUi.IsOpen:59", 2d)) ModLog.Every("KyUi.IsOpen:59", 2d, $"KyUi.IsOpen: returns {CoreLog.Shown(returnedAt59)} (line 59)"); return returnedAt59; }
    }
}
