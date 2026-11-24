using System.Collections.Generic;
using UnityEngine;
using KydlCore.Core;
using KydlCore.Common;

namespace KydlCore.Features.Items.Contracts;

/// <summary>
/// What a <see cref="KyEntry"/> is: an inventory item, a build piece, or a material (user, 2026-11-18): an ingredient (saltpeter, gunpowder, water) or a
/// gatherable / world deposit (bat guano deposit, sulfurous mud). Materials get their own "Materials" tab in the item viewer. Appended last so the
/// numbers of Item and Piece never change.
/// </summary>
public enum KyKind { Item, Piece, Material }

/// <summary>
/// One of the user's prefabs in the <see cref="KyItems"/> registry (DESIGN 3 / 6): where it comes from (mod GUID and name), what it is and how it is
/// named. Read only for consumers; made by the registry.
/// </summary>
public sealed class KyEntry
{
    /// <summary>The namespaced KY-Items id ("stevethetroll:slippers") of an explicit contribution; "" for a prefab found the legacy way.</summary>
    public string Id = "";
    /// <summary>The prefab name (the key: one entry per prefab name, case ignored).</summary>
    public string Prefab;
    /// <summary>The prefab itself (null in unit tests).</summary>
    public GameObject PrefabObject;
    /// <summary>The GUID of the mod that registered it (e.g. "com.kydldev.DrainTarPit"); "" when unknown.</summary>
    public string ModGuid = "";
    /// <summary>The mod's name, or "build category KY-Items" for a piece found only by its category.</summary>
    public string ModName = "";
    /// <summary>What it is: an item, a piece or a material (shown on the Materials tab).</summary>
    public KyKind Kind;
    /// <summary>The localisation key / name from the game data ("$item_x" or a plain name); <see cref="DisplayName"/> is the shown text.</summary>
    public string NameToken = "";
    Sprite _icon;
    /// <summary>
    /// The icon, read from the prefab EVERY time it is asked: a mod may swap it after registering (DrainTarPit puts a placeholder first and renders the
    /// real icons when a world is loaded), so a copy taken at registration would be the placeholder for good. Falls back to the icon seen at registration.
    /// </summary>
    public Sprite Icon
    {
        get { Sprite live = LiveIcon(); return live != null ? live : _icon; }
        set { _icon = value; }
    }

    Sprite LiveIcon()
    {
        if (CoreLog.On) ModLog.Verbose($"KyEntry.LiveIcon()");
        if (PrefabObject == null) { if (CoreLog.On) ModLog.Verbose("KyEntry.LiveIcon: returns null (line 48)"); return null; }
        ItemDrop drop = PrefabObject.GetComponent<ItemDrop>();
        if (drop != null)
        {
            Sprite[] icons = drop.m_itemData.m_shared.m_icons;
            { var returnedAt53 = icons != null && icons.Length > 0 ? icons[0] : null; if (CoreLog.On) ModLog.Verbose($"KyEntry.LiveIcon: returns {CoreLog.Shown(returnedAt53)} (line 53)"); return returnedAt53; }
        }
        Piece piece = PrefabObject.GetComponent<Piece>();
        if (piece != null) { var returnedAt56 = piece.m_icon; if (CoreLog.On) ModLog.Verbose($"KyEntry.LiveIcon: returns {CoreLog.Shown(returnedAt56)} (line 56)"); return returnedAt56; }
        { var returnedAt57 = DepositIcon(PrefabObject); if (CoreLog.On) ModLog.Verbose($"KyEntry.LiveIcon: returns {CoreLog.Shown(returnedAt57)} (line 57)"); return returnedAt57; }
    }

    /// <summary>A deposit (Pickable) has no icon of its own: it shows the icon of what it yields. Null when there is none.</summary>
    internal static Sprite DepositIcon(GameObject go)
    {
        if (CoreLog.Due("KyEntry.DepositIcon", 2d)) ModLog.Every("KyEntry.DepositIcon", 2d, $"KyEntry.DepositIcon(go={CoreLog.Shown(go)})");
        Pickable pick = go != null ? go.GetComponent<Pickable>() : null;
        ItemDrop yields = pick != null && pick.m_itemPrefab != null ? pick.m_itemPrefab.GetComponent<ItemDrop>() : null;
        Sprite[] icons = yields != null ? yields.m_itemData.m_shared.m_icons : null;
        { var returnedAt67 = icons != null && icons.Length > 0 ? icons[0] : null; if (CoreLog.Due("KyEntry.DepositIcon:67", 2d)) ModLog.Every("KyEntry.DepositIcon:67", 2d, $"KyEntry.DepositIcon: returns {CoreLog.Shown(returnedAt67)} (line 67)"); return returnedAt67; }
    }
    /// <summary>Free tags the registering mod gave (may be empty).</summary>
    public IReadOnlyList<string> Tags = new string[0];
    /// <summary>How the registry found it: "Contribute" (the spec API), "Register", "Jotunn" / "category" (legacy, until every mod contributes).</summary>
    public string Source = "";

    /// <summary>The name shown to the player: the token localised when the game's text is ready, else the token or the prefab name.</summary>
    public string DisplayName
    {
        get
        {
            string token = string.IsNullOrEmpty(NameToken) ? Prefab : NameToken;
            return Localization.instance != null ? Localization.instance.Localize(token) : token;
        }
    }
}
