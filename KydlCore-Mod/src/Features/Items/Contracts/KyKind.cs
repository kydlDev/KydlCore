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
