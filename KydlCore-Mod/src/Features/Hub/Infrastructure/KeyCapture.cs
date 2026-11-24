using System;
using System.Collections.Generic;
using KydlCore.Common;
using UnityEngine;
using KydlCore.Core;

namespace KydlCore.Features.Hub.Infrastructure;

/// <summary>
/// The GAME side of the press-to-rebind button (D-075): while a capture is on, it reads the keyboard every frame (Plugin.Update, before the hub key
/// and Escape are looked at) and hands what was pressed to <see cref="KeyCaptureRules"/>. While it runs, and in the frame it ends, the hub key and
/// Escape do nothing else (<see cref="Blocks"/>), so pressing the hub's own key to rebind it does not close the hub and Escape only cancels.
/// </summary>
internal static class KeyCapture
{
    static readonly KeyCode[] AllKeys = (KeyCode[])Enum.GetValues(typeof(KeyCode));
    static readonly List<KeyCode> Pressed = new List<KeyCode>(8);
    static readonly List<KeyCode> Held = new List<KeyCode>(6);
    static readonly KeyCode[] Modifiers = { KeyCode.LeftShift, KeyCode.RightShift, KeyCode.LeftControl, KeyCode.RightControl, KeyCode.LeftAlt, KeyCode.RightAlt };

    static Action<KeyCode, List<KeyCode>> _done;
    static Action _cancelled;
    static bool _withModifiers;
    static int _startFrame = -1, _endFrame = -1;

    /// <summary>A capture is waiting for a key.</summary>
    internal static bool Active { get { return _done != null; } }

    /// <summary>True while a capture runs and in the frame it ended: the hub key and Escape must not act on that press.</summary>
    internal static bool Blocks { get { return Active || _endFrame == Time.frameCount; } }

    /// <summary>
    /// Starts waiting for a key (an earlier capture is cancelled). `withModifiers` = a shortcut: held Shift / Ctrl / Alt join the key.
    /// `done` gets the key and the modifiers, `cancelled` runs when Escape or <see cref="Cancel"/> ends it.
    /// </summary>
    internal static void Begin(bool withModifiers, Action<KeyCode, List<KeyCode>> done, Action cancelled)
    {
        Cancel("another capture started");
        _withModifiers = withModifiers;
        _done = done;
        _cancelled = cancelled;
        _startFrame = Time.frameCount;
        ModLog.Verbose($"KeyCapture: waiting for a {(withModifiers ? "shortcut" : "key")}");
    }

    /// <summary>Ends a running capture without a result (the page closed or was redrawn). Expected outcome: not active afterwards.</summary>
    internal static bool Cancel(string why)
    {
        if (!Active) { if (CoreLog.Due("KeyCapture.Cancel:49", 2d)) ModLog.Every("KeyCapture.Cancel:49", 2d, "KeyCapture.Cancel: returns true (line 49)"); return true; }
        var cancelled = _cancelled;
        Clear();
        ModLog.Verbose($"KeyCapture: cancelled ({why})");
        if (cancelled != null) cancelled();
        { var returnedAt54 = !Active; if (CoreLog.Due("KeyCapture.Cancel:54", 2d)) ModLog.Every("KeyCapture.Cancel:54", 2d, $"KeyCapture.Cancel: returns {CoreLog.Shown(returnedAt54)} (line 54)"); return returnedAt54; }
    }

    /// <summary>Every frame (Plugin.Update, first). Expected outcome: true when this frame ended the capture.</summary>
    internal static bool Tick()
    {
        if (!Active || Time.frameCount == _startFrame) { if (CoreLog.Due("KeyCapture.Tick:60", 2d)) ModLog.Every("KeyCapture.Tick:60", 2d, "KeyCapture.Tick: returns false (line 60)"); return false; }     // the click that started it is not the key
        var pressed = Pressed;         // reused every frame of a capture (rule 38: per-frame paths allocate nothing); Decide only reads it
        pressed.Clear();
        foreach (KeyCode key in AllKeys)
            if (key != KeyCode.None && Input.GetKeyDown(key) && !pressed.Contains(key)) pressed.Add(key);
        if (pressed.Count == 0) { if (CoreLog.Due("KeyCapture.Tick:65", 2d)) ModLog.Every("KeyCapture.Tick:65", 2d, "KeyCapture.Tick: returns false (line 65)"); return false; }
        var held = Held;
        held.Clear();
        foreach (KeyCode m in Modifiers) if (Input.GetKey(m)) held.Add(m);

        KeyCode main;
        List<KeyCode> mods;
        CaptureStep step = KeyCaptureRules.Decide(pressed, held, _withModifiers, out main, out mods);
        if (step == CaptureStep.Waiting) { if (CoreLog.Due("KeyCapture.Tick:73", 2d)) ModLog.Every("KeyCapture.Tick:73", 2d, "KeyCapture.Tick: returns false (line 73)"); return false; }
        var done = _done;
        var cancelled = _cancelled;
        Clear();
        if (step == CaptureStep.Cancelled)
        {
            ModLog.Verbose("KeyCapture: Escape - the setting stays");
            if (cancelled != null) cancelled();
        }
        else
        {
            ModLog.Verbose($"KeyCapture: {KeyCaptureRules.ShortcutText(main, mods)}");
            if (done != null) done(main, mods);
        }
        { if (CoreLog.Due("KeyCapture.Tick:87", 2d)) ModLog.Every("KeyCapture.Tick:87", 2d, "KeyCapture.Tick: returns true (line 87)"); return true; }
    }

    static void Clear()
    {
        if (CoreLog.Due("KeyCapture.Clear", 2d)) ModLog.Every("KeyCapture.Clear", 2d, $"KeyCapture.Clear()");
        _done = null;
        _cancelled = null;
        _endFrame = Time.frameCount;
    }
}
