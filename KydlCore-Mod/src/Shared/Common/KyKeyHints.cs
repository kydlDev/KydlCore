using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using KydlCore.Core;

namespace KydlCore.Common;

/// <summary>
/// SHARED (KydlCore 1.2.0, replaces Jotunn's KeyHintManager / KeyHintConfig, D-104). While the player holds the item (prefab name), the game's key
/// hints (bottom right) show the mod's rows instead (Integrations/Valheim/KeyHintPanels). Keyboard / mouse rows only.
/// </summary>
public static class KyKeyHints
{
    internal sealed class Panel
    {
        internal KyKeyHint[] Hints;
        internal bool Dirty;
    }

    static readonly Dictionary<string, Panel> Panels = new();

    /// <summary>Shows <paramref name="hints"/> while <paramref name="itemPrefab"/> is held. Expected outcome: true when the panel is kept (false for no item / no rows, or an item that has one already).</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Add(string itemPrefab, params KyKeyHint[] hints)
    {
        var ctx = ModContexts.For(Assembly.GetCallingAssembly());
        KyKeyHint[] rows = (hints ?? new KyKeyHint[0]).Where(h => h != null).ToArray();
        if (string.IsNullOrEmpty(itemPrefab) || rows.Length == 0 || Panels.ContainsKey(itemPrefab))
        {
            ModLog.Warn(ctx, $"KyKeyHints.Add: not kept - item='{itemPrefab}', rows={rows.Length}, already has a panel={(itemPrefab != null && Panels.ContainsKey(itemPrefab))}");
            return false;
        }
        var panel = new Panel { Hints = rows, Dirty = true };
        foreach (KyKeyHint h in rows) if (h.Key != null) h.Key.SettingChanged += (_, _) => panel.Dirty = true;     // a new key shows at once (rule 17)
        Panels[itemPrefab] = panel;
        ModLog.Verbose(ctx, $"KyKeyHints: panel for '{itemPrefab}' kept - {rows.Length} row(s), panels {Panels.Count}");
        return Panels.ContainsKey(itemPrefab);
    }

    /// <summary>The panel for an item prefab name (null when none).</summary>
    internal static Panel For(string itemPrefab)
    {
        Panels.TryGetValue(itemPrefab ?? "", out Panel panel);
        if (CoreLog.Due("KyKeyHints.For", 5d)) ModLog.Every("KyKeyHints.For", 5d, $"KyKeyHints.For: '{itemPrefab}' -> {(panel == null ? "<none>" : $"{panel.Hints.Length} row(s)")}");
        return panel;
    }

    /// <summary>A key binding was saved: every panel is drawn again (the key texts may have changed). Expected outcome: the number of panels marked.</summary>
    internal static int MarkAllDirty()
    {
        foreach (Panel p in Panels.Values) p.Dirty = true;
        ModLog.Verbose($"KyKeyHints.MarkAllDirty: {Panels.Count} panel(s) redrawn on next show");
        return Panels.Count;
    }
}
