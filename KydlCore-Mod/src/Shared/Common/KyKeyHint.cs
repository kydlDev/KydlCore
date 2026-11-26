using BepInEx.Configuration;
using UnityEngine;

namespace KydlCore.Common;

/// <summary>One line of a <see cref="KyKeyHints"/> panel: a key and what it does (KydlCore 1.2.0, replaces Jotunn's ButtonConfig in key hints, D-104).</summary>
public sealed class KyKeyHint
{
    /// <summary>A game button ("Attack", "SecondaryAttack", "Block", "Use" ...): its bound key is shown. Ignored when <see cref="Key"/> is set.</summary>
    public string Button = "";
    /// <summary>A mod's own key setting: its current key is shown (follows the setting live).</summary>
    public ConfigEntry<KeyCode> Key;
    /// <summary>What the key does, as shown next to it (English, rule 15).</summary>
    public string Hint = "";
}
