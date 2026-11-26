using BepInEx.Configuration;

namespace KydlCore.Common;

/// <summary>
/// SHARED (KydlCore 1.2.0, D-105: was a copy in 15 mods). Descriptions that carry the F1 configuration manager's order tag (rule 36): <see cref="First"/> =
/// top of its category ([General] Enable Mod), <see cref="Last"/> = bottom. A HIGHER Order is shown HIGHER. The KY hub (F6) lists entries in bind order, so
/// bind them in the same order. Uses KydlCore's <see cref="ConfigurationManagerAttributes"/> (mods drop their own copy of that class).
/// </summary>
public static class ConfigOrder
{
    /// <summary>The Order value of the first entry of a category.</summary>
    public const int FirstOrder = 1000;
    /// <summary>The Order value of the last entry of a category.</summary>
    public const int LastOrder = -1000;

    /// <summary>A description shown at the top of its category (optional value range).</summary>
    public static ConfigDescription First(string description, AcceptableValueBase range = null)
    {
        var d = new ConfigDescription(description, range, new ConfigurationManagerAttributes { Order = FirstOrder });
        ModLog.Verbose($"ConfigOrder.First: order {FirstOrder}, range={(range != null)}");
        return d;
    }

    /// <summary>A description shown at the bottom of its category (optional value range).</summary>
    public static ConfigDescription Last(string description, AcceptableValueBase range = null)
    {
        var d = new ConfigDescription(description, range, new ConfigurationManagerAttributes { Order = LastOrder });
        ModLog.Verbose($"ConfigOrder.Last: order {LastOrder}, range={(range != null)}");
        return d;
    }
}
