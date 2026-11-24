using System;
using UnityEngine;

namespace KydlCore.Features.Items.Contracts;

/// <summary>
/// What a mod contributes to KY-Items (spec 5, MIGRATION_DECISIONS B13): a namespaced id, the owning mod, a display name, the prefab, recipe information
/// and callbacks. Hand it to <see cref="KyItems.Contribute"/> from the mod's Awake / feature Start. The registry is Jotunn-free; with
/// <see cref="RegisterInGame"/> KydlCore also adds the prefab to the game (items with their recipe, pieces to the hammer in the KY-Items category).
/// </summary>
public sealed class KyContribution
{
    /// <summary>"&lt;modid&gt;:&lt;item&gt;", e.g. "stevethetroll:slippers" (modid = the GUID after "com.kydldev.", lowercase).</summary>
    public string Id = "";
    /// <summary>The owning mod's GUID ("com.kydldev.SteveTheTroll").</summary>
    public string ModGuid = "";
    /// <summary>The shown name or its localisation token ("$item_slippers"); empty = read from the prefab.</summary>
    public string DisplayName = "";
    /// <summary>The prefab (required).</summary>
    public GameObject Prefab;
    /// <summary>What it is: an item, a piece or a material (deposit / ingredient, shown on the hub's Materials tab).</summary>
    public KyKind Kind;
    /// <summary>How it is made (null = not craftable / the mod handles it).</summary>
    public KyRecipe Recipe;
    /// <summary>Free tags (may be empty).</summary>
    public string[] Tags = new string[0];
    /// <summary>True = KydlCore adds the prefab to the game (instead of the mod calling Jotunn itself).</summary>
    public bool RegisterInGame;
    /// <summary>Called once the contribution is accepted (with the prefab), e.g. to attach the mod's behaviour components.</summary>
    public Action<GameObject> OnRegistered;
}
