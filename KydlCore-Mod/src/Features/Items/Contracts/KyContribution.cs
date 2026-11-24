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

/// <summary>Recipe information of a contribution.</summary>
public sealed class KyRecipe
{
    /// <summary>Crafting station prefab name ("piece_workbench"); "" = crafted by hand (items) / no station (pieces).</summary>
    public string CraftingStation = "";
    /// <summary>The station level the recipe needs (1 = any level).</summary>
    public int MinStationLevel = 1;
    /// <summary>How many one craft makes (items).</summary>
    public int Amount = 1;
    /// <summary>The ingredients (may be empty).</summary>
    public KyResource[] Resources = new KyResource[0];
}

/// <summary>One resource of a recipe.</summary>
public sealed class KyResource
{
    /// <summary>The ingredient's item prefab name ("Wood").</summary>
    public string Item = "";
    /// <summary>How many one craft takes.</summary>
    public int Amount = 1;
    /// <summary>How many more each item quality level takes (items with upgrades).</summary>
    public int AmountPerLevel;
    /// <summary>True = deconstructing the piece gives the ingredient back.</summary>
    public bool Recover = true;
    /// <summary>An empty resource; set the fields.</summary>
    public KyResource() { }
    /// <summary>A resource of <paramref name="amount"/> x <paramref name="item"/> (a null item becomes "").</summary>
    public KyResource(string item, int amount) { Item = item ?? ""; Amount = amount; }
}

/// <summary>The answer of <see cref="KyItems.Contribute"/>: accepted (with the normalised id) or rejected with the reason.</summary>
public sealed class KyResult
{
    /// <summary>True when the contribution was taken into the registry.</summary>
    public bool Accepted { get; private set; }
    /// <summary>The normalised id ("&lt;modid&gt;:&lt;item&gt;").</summary>
    public string Id { get; private set; }
    /// <summary>Why it was rejected, or a note on an accepted one ("" when there is none).</summary>
    public string Reason { get; private set; }
    KyResult() { }
    internal static KyResult Ok(string id, string reason = "") { return new KyResult { Accepted = true, Id = id ?? "", Reason = reason ?? "" }; }
    internal static KyResult Rejected(string id, string reason) { return new KyResult { Accepted = false, Id = id ?? "", Reason = reason ?? "" }; }
    /// <summary>"accepted &lt;id&gt;" / "rejected &lt;id&gt;: &lt;reason&gt;".</summary>
    public override string ToString() { return (Accepted ? "accepted " : "rejected ") + Id + (string.IsNullOrEmpty(Reason) ? "" : ": " + Reason); }
}
