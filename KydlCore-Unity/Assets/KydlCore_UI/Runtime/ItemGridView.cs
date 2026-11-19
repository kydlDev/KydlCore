using System.Collections.Generic;
using KydlCore.Ui;
using UnityEngine;
using UnityEngine.UIElements;

namespace KydlCore.Ui;

/// <summary>One card of an <see cref="ItemGridView"/>: a big icon, the name under it and a smaller line (the prefab name) under that.</summary>
internal sealed class ItemCard
{
    internal string Title = "";
    internal string Subtitle = "";
    internal Sprite Icon;
}

/// <summary>
/// The grid way of showing items (user 2026-11-19, KydlCore D-096): <see cref="PerRow"/> square icons side by side fill the width, each with its
/// name and, below it, its prefab name. The icon keeps its own shape inside the square (contain). Unity-side view: previewed in MenuPreview.
/// </summary>
internal static class ItemGridView
{
    /// <summary>Icons per row.</summary>
    internal const int PerRow = 4;
    const float Gap = 8f;

    /// Builds the grid. Expected outcome: one cell per card, <see cref="PerRow"/> per row (an empty element for none).
    internal static VisualElement Build(IList<ItemCard> cards)
    {
        var grid = new VisualElement { name = "kydl-item-grid" };
        grid.style.flexDirection = FlexDirection.Row;
        grid.style.flexWrap = Wrap.Wrap;
        grid.style.flexShrink = 0;
        int n = 0;
        if (cards != null) foreach (var c in cards) { grid.Add(Cell(c)); n++; }
        UiLog.Verbose("ItemGridView: " + n + " card(s), " + PerRow + " per row");
        return grid;
    }

    static VisualElement Cell(ItemCard card)
    {
        var cell = new VisualElement { name = "kydl-item-card" };
        cell.style.width = Length.Percent(100f / PerRow);
        cell.style.flexShrink = 0;
        cell.style.paddingLeft = Gap / 2; cell.style.paddingRight = Gap / 2; cell.style.paddingTop = Gap / 2; cell.style.paddingBottom = Gap;
        cell.style.alignItems = Align.Stretch;

        var frame = new VisualElement { name = "kydl-item-icon-frame", pickingMode = PickingMode.Ignore };
        frame.style.backgroundColor = UiKit.PanelLight;
        UiKit.Borders(frame, 1f, UiKit.Border);
        UiKit.Radius(frame, 4f);
        // square: the height follows the width the layout gave the cell
        frame.RegisterCallback<GeometryChangedEvent>(e =>
        {
            float w = e.newRect.width;
            if (w > 0f && Mathf.Abs(frame.resolvedStyle.height - w) > 0.5f) frame.style.height = w;
        });
        var icon = new VisualElement { name = "kydl-item-icon", pickingMode = PickingMode.Ignore };
        icon.style.flexGrow = 1;
        icon.style.marginLeft = 6; icon.style.marginRight = 6; icon.style.marginTop = 6; icon.style.marginBottom = 6;
        icon.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
        if (card.Icon != null) icon.style.backgroundImage = new StyleBackground(card.Icon);
        frame.Add(icon);
        cell.Add(frame);

        var title = UiKit.TextLabel(card.Title ?? "", TextRole.Name);
        title.style.unityTextAlign = TextAnchor.UpperCenter;
        title.style.whiteSpace = WhiteSpace.Normal;
        title.style.marginTop = 4;
        cell.Add(title);
        var sub = UiKit.TextLabel(card.Subtitle ?? "", TextRole.SmallNote);
        sub.style.unityTextAlign = TextAnchor.UpperCenter;
        sub.style.whiteSpace = WhiteSpace.Normal;
        cell.Add(sub);
        return cell;
    }
}
