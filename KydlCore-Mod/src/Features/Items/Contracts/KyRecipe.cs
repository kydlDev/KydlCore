using System;
using UnityEngine;

namespace KydlCore.Features.Items.Contracts;

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
