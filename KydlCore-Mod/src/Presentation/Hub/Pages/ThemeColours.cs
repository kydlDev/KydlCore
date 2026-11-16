using KydlCore.Ui;
using UnityEngine;

namespace KydlCore.Presentation.Hub.Pages;

/// <summary>
/// KydlCore's own text colours (user 2026-11-16: the hub shows every page in KydlCore's colours; a colour a mod set itself is replaced, see
/// ConfigPage.EnforceText). A colour counts as KydlCore's when it is one of the kit's palette, whatever its alpha.
/// </summary>
internal static class ThemeColours
{
    const float Tolerance = 0.004f;      // one step of an 8-bit colour channel

    static readonly Color[] Own =
    {
        UiKit.Text, UiKit.TextDim, UiKit.Gold, UiKit.GoldDim, UiKit.NameColor, UiKit.ValueColor, UiKit.TabColor, UiKit.OnDark, UiKit.Good, UiKit.Bad,
    };

    /// <summary>Is this one of KydlCore's text colours? Expected outcome: true for every palette colour (alpha ignored), false for any other.</summary>
    internal static bool IsOwn(Color colour)
    {
        foreach (Color own in Own)
            if (Mathf.Abs(own.r - colour.r) <= Tolerance && Mathf.Abs(own.g - colour.g) <= Tolerance && Mathf.Abs(own.b - colour.b) <= Tolerance) return true;
        return false;
    }
}
