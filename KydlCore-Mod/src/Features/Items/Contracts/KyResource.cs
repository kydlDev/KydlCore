using System;
using UnityEngine;

namespace KydlCore.Features.Items.Contracts;

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
