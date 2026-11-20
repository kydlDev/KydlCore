using System.Collections.Generic;
using UnityEngine;
using KydlCore.Core;
using KydlCore.Common;

namespace KydlCore.Features.Hub.Infrastructure;

/// <summary>What a press-to-rebind capture does with one frame of key presses.</summary>
internal enum CaptureStep
{
    /// <summary>Nothing usable was pressed: keep waiting.</summary>
    Waiting,
    /// <summary>Escape: leave the setting as it was.</summary>
    Cancelled,
    /// <summary>A key was chosen (and, for a shortcut, the modifiers held with it).</summary>
    Done
}

/// <summary>
/// Pure rules of the press-to-rebind button of the auto Settings page (user 2026-10-30, D-075): which key a frame of presses means, which of the
/// held Shift / Ctrl / Alt keys join a shortcut, and how a key or shortcut is written on the button. Only Unity's plain KeyCode enum, no engine
/// calls, no logging (like ConfigControlMap): the page and <c>KeyCapture</c> read the keyboard and write the entry.
/// </summary>
internal static class KeyCaptureRules
{
    /// <summary>Shift, Control and Alt (left or right) are the modifiers of a shortcut.</summary>
    public static bool IsModifier(KeyCode key)
    {
        if (CoreLog.On) ModLog.Every("KeyCaptureRules.IsModifier", 2d, $"KeyCaptureRules.IsModifier(key={key})");
        return key == KeyCode.LeftShift || key == KeyCode.RightShift || key == KeyCode.LeftControl || key == KeyCode.RightControl ||
               key == KeyCode.LeftAlt || key == KeyCode.RightAlt;
    }

    /// <summary>
    /// Decides one frame. `pressed` = the keys that went down in this frame, `held` = the modifier keys down right now. Escape cancels. The left
    /// mouse button is the click on the button itself and never counts. For a shortcut (`withModifiers`) a modifier alone is not a choice, the
    /// capture waits for the key it modifies and takes the held modifiers with it; for a single key every key counts, a modifier too.
    /// </summary>
    public static CaptureStep Decide(IList<KeyCode> pressed, IList<KeyCode> held, bool withModifiers, out KeyCode main, out List<KeyCode> modifiers)
    {
        if (CoreLog.On) ModLog.Every("KeyCaptureRules.Decide", 2d, $"KeyCaptureRules.Decide(pressed={pressed}, held={held}, withModifiers={withModifiers})");
        main = KeyCode.None;
        modifiers = new List<KeyCode>();
        if (pressed == null) { var returnedAt44 = CaptureStep.Waiting; if (CoreLog.On) ModLog.Every("KeyCaptureRules.Decide:44", 2d, $"KeyCaptureRules.Decide: returns {returnedAt44} (line 44)"); return returnedAt44; }
        if (pressed.Contains(KeyCode.Escape)) { var returnedAt45 = CaptureStep.Cancelled; if (CoreLog.On) ModLog.Every("KeyCaptureRules.Decide:45", 2d, $"KeyCaptureRules.Decide: returns {returnedAt45} (line 45)"); return returnedAt45; }
        foreach (KeyCode key in pressed)
        {
            if (key == KeyCode.None || key == KeyCode.Mouse0) continue;
            if (withModifiers && IsModifier(key)) continue;
            main = key;
            if (withModifiers && held != null)
                foreach (KeyCode m in held)
                    if (m != key && IsModifier(m) && !modifiers.Contains(m)) modifiers.Add(m);
            { var returnedAt54 = CaptureStep.Done; if (CoreLog.On) ModLog.Every("KeyCaptureRules.Decide:54", 2d, $"KeyCaptureRules.Decide: returns {returnedAt54} (line 54)"); return returnedAt54; }
        }
        { var returnedAt56 = CaptureStep.Waiting; if (CoreLog.On) ModLog.Every("KeyCaptureRules.Decide:56", 2d, $"KeyCaptureRules.Decide: returns {returnedAt56} (line 56)"); return returnedAt56; }
    }

    /// <summary>A key as the button shows it: the digit row as 0..9, nothing as "(none)", every other key by its name.</summary>
    public static string KeyName(KeyCode key)
    {
        if (CoreLog.On) ModLog.Every("KeyCaptureRules.KeyName", 2d, $"KeyCaptureRules.KeyName(key={key})");
        if (key == KeyCode.None) { if (CoreLog.On) ModLog.Every("KeyCaptureRules.KeyName:63", 2d, "KeyCaptureRules.KeyName: returns \"(none)\" (line 63)"); return "(none)"; }
        if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9) { var returnedAt64 = ((int)key - (int)KeyCode.Alpha0).ToString(); if (CoreLog.On) ModLog.Every("KeyCaptureRules.KeyName:64", 2d, $"KeyCaptureRules.KeyName: returns {returnedAt64} (line 64)"); return returnedAt64; }
        { var returnedAt65 = key.ToString(); if (CoreLog.On) ModLog.Every("KeyCaptureRules.KeyName:65", 2d, $"KeyCaptureRules.KeyName: returns {returnedAt65} (line 65)"); return returnedAt65; }
    }

    /// <summary>A shortcut as the button shows it: the modifiers, then the key, joined with " + " ("LeftControl + K").</summary>
    public static string ShortcutText(KeyCode main, IList<KeyCode> modifiers)
    {
        if (CoreLog.On) ModLog.Every("KeyCaptureRules.ShortcutText", 2d, $"KeyCaptureRules.ShortcutText(main={main}, modifiers={modifiers})");
        if (main == KeyCode.None) { if (CoreLog.On) ModLog.Every("KeyCaptureRules.ShortcutText:72", 2d, "KeyCaptureRules.ShortcutText: returns \"(none)\" (line 72)"); return "(none)"; }
        var parts = new List<string>();
        if (modifiers != null) foreach (KeyCode m in modifiers) parts.Add(KeyName(m));
        parts.Add(KeyName(main));
        { var returnedAt76 = string.Join(" + ", parts.ToArray()); if (CoreLog.On) ModLog.Every("KeyCaptureRules.ShortcutText:76", 2d, $"KeyCaptureRules.ShortcutText: returns {returnedAt76} (line 76)"); return returnedAt76; }
    }
}
