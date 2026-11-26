using System;
using System.Collections.Generic;
using System.Linq;
using KydlCore.Common;
using KydlCore.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KydlCore.Integrations.Valheim;

/// <summary>
/// Draws the KyKeyHints panels (ported in part from Jotunn 2.30.2 KeyHintManager, D-104): the game's build-hints panel is cloned, its keys removed
/// and one "Place"-style row made per hint. Keyboard / mouse only (no gamepad icons). Game code: tested in game (GunPowdered keyHintTexts hook).
/// </summary>
internal static class KeyHintPanels
{
    static readonly Dictionary<string, GameObject> Panels = new();
    static GameObject _baseKey;
    static KeyHints _owner;

    /// <summary>KeyHints.Start: the row template is taken from the build hints. Expected outcome: true when the template exists.</summary>
    internal static bool Start(KeyHints hints)
    {
        _owner = hints;
        Panels.Clear();
        Transform keys = hints.m_buildHints.GetComponent<UIInputHint>()?.m_mouseKeyboardHint?.transform;
        GameObject place = keys == null ? null : keys.Find("Place")?.gameObject;
        if (place != null && _baseKey == null)
        {
            _baseKey = UnityEngine.Object.Instantiate(place, KyPrefabs.Holder, false);
            _baseKey.name = "KydlCore_KeyHintRow";
        }
        ModLog.Verbose($"KeyHintPanels.Start: row template {(_baseKey != null ? "ready" : "NOT found (build hints have no 'Place' row)")}");
        return _baseKey != null;
    }

    /// <summary>KeyHints.UpdateHints prefix: true = let the game draw its own hints (no panel for the item in hand).</summary>
    internal static bool Update(KeyHints hints)
    {
        string item = ItemInHand(hints);
        KyKeyHints.Panel panel = item == null ? null : KyKeyHints.For(item);
        if (panel == null || _baseKey == null || _owner != hints)
        {
            foreach (GameObject go in Panels.Values) if (go != null && go.activeSelf) go.SetActive(false);
            if (CoreLog.Due("KeyHintPanels.Update", 5d)) ModLog.Every("KeyHintPanels.Update", 5d, $"KeyHintPanels.Update: item {item ?? "<none>"} - game hints (panel={panel != null}, template={_baseKey != null})");
            return true;
        }
        if (!Panels.TryGetValue(item, out GameObject shown) || shown == null || panel.Dirty) { if (shown != null) UnityEngine.Object.Destroy(shown); shown = Build(hints, item, panel); Panels[item] = shown; }
        if (!shown.activeSelf)
        {
            hints.m_buildHints.SetActive(false);
            hints.m_combatHints.SetActive(false);
            hints.m_inventoryHints.SetActive(false);
            hints.m_inventoryWithContainerHints.SetActive(false);
            hints.m_fishingHints.SetActive(false);
            foreach (GameObject go in Panels.Values) if (go != null && go != shown) go.SetActive(false);
            shown.SetActive(true);
            ModLog.Verbose($"KeyHintPanels: panel for '{item}' shown ({panel.Hints.Length} key(s))");
        }
        if (CoreLog.Due("KeyHintPanels.Update:own", 5d)) ModLog.Every("KeyHintPanels.Update:own", 5d, $"KeyHintPanels.Update: own panel for '{item}' - game hints skipped");
        return false;
    }

    static string ItemInHand(KeyHints hints)
    {
        Player p = Player.m_localPlayer;
        bool blocked = !HintsEnabled(hints) || p == null || p.IsDead() || Game.IsPaused() || (Chat.instance != null && Chat.instance.IsChatDialogWindowVisible()) || InventoryGui.IsVisible();
        ItemDrop.ItemData data = blocked ? null : p.GetCurrentWeapon();
        string name = data == null || data.m_dropPrefab == null ? null : data.m_dropPrefab.name;
        if (CoreLog.Due("KeyHintPanels.ItemInHand", 5d)) ModLog.Every("KeyHintPanels.ItemInHand", 5d, $"KeyHintPanels.ItemInHand: blocked={blocked} -> {name ?? "<none>"}");
        return name;
    }

    static readonly HarmonyLib.AccessTools.FieldRef<KeyHints, bool> HintsEnabled = HarmonyLib.AccessTools.FieldRefAccess<KeyHints, bool>("m_keyHintsEnabled");

    static GameObject Build(KeyHints hints, string item, KyKeyHints.Panel panel)
    {
        GameObject root = UnityEngine.Object.Instantiate(hints.m_buildHints, hints.transform, false);
        root.name = $"KydlCore_KeyHints_{item}";
        UIInputHint input = root.GetComponent<UIInputHint>();
        Transform keys = input?.m_mouseKeyboardHint?.transform;
        Transform pad = input?.m_gamepadHint?.transform;
        foreach (Transform t in new[] { keys, pad }.Where(x => x != null)) foreach (Transform child in t) UnityEngine.Object.Destroy(child.gameObject);
        foreach (KyKeyHint hint in panel.Hints)
        {
            GameObject row = UnityEngine.Object.Instantiate(_baseKey, keys, false);
            row.name = hint.Key != null ? hint.Key.Definition.Key : hint.Button;
            SetText(row.transform.Find("key_bkg/Key"), KeyText(hint));
            SetText(row.transform.Find("Text"), hint.Hint);
            row.SetActive(true);
        }
        root.SetActive(false);
        panel.Dirty = false;
        ModLog.Verbose($"KeyHintPanels.Build: panel for '{item}' with {panel.Hints.Length} row(s): {string.Join(", ", panel.Hints.Select(h => $"{KeyText(h)}={h.Hint}"))}");
        return root;
    }

    static string KeyText(KyKeyHint hint)
    {
        string text = hint.Key != null ? hint.Key.Value.ToString() : ZInput.instance == null ? hint.Button : ZInput.instance.GetBoundKeyString(hint.Button, true);
        if (string.IsNullOrEmpty(text)) text = hint.Button;
        if (text.StartsWith("$", StringComparison.Ordinal) && Localization.instance != null) text = Localization.instance.Localize(text);
        if (CoreLog.On) ModLog.Verbose($"KeyHintPanels.KeyText: button '{hint.Button}', key setting={(hint.Key != null)} -> '{text}'");
        return text;
    }

    static bool SetText(Transform t, string text)
    {
        if (t == null) { ModLog.Verbose($"KeyHintPanels.SetText: no child for '{text}' (the build-hint row changed in a game update?)"); return false; }
        TMP_Text tmp = t.GetComponent<TMP_Text>();
        if (tmp != null) tmp.text = text;
        Text ui = tmp == null ? t.GetComponent<Text>() : null;
        if (ui != null) ui.text = text;
        bool set = tmp != null || ui != null;
        if (CoreLog.On) ModLog.Verbose($"KeyHintPanels.SetText: '{t.name}' = '{text}' (set={set})");
        return set;
    }
}
