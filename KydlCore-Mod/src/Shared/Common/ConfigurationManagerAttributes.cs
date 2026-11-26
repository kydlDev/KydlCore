using System;
using BepInEx.Configuration;

namespace KydlCore.Common;

/// <summary>
/// SHARED (KydlCore 1.2.0, replaces Jotunn's copy, D-104). Tag for a config entry's description: the BepInEx configuration manager (F1) reads every
/// tag of a class with exactly this name, field by field (so the field names must not change), and KydlCore's hub / KyNetwork read IsAdminOnly.
/// Unset fields (null) keep the manager's default. Usage: <c>new ConfigDescription(text, range, new ConfigurationManagerAttributes { Order = 10 })</c>.
/// </summary>
public sealed class ConfigurationManagerAttributes
{
    /// <summary>Show a number range as a percentage.</summary>
    public bool? ShowRangeAsPercent;
    /// <summary>Draws the setting's value instead of the manager's default control (e.g. the colour drawer with a hex box, rule 12).</summary>
    public Action<ConfigEntryBase> CustomDrawer;
    /// <summary>False hides the entry from the manager and from the KY hub.</summary>
    public bool? Browsable;
    /// <summary>Category name shown instead of the section name.</summary>
    public string Category;
    /// <summary>The value the reset button sets.</summary>
    public object DefaultValue;
    /// <summary>True hides the reset button.</summary>
    public bool? HideDefaultButton;
    /// <summary>True hides the setting's name (the drawer takes the whole width).</summary>
    public bool? HideSettingName;
    /// <summary>Description shown instead of the ConfigDescription text.</summary>
    public string Description;
    /// <summary>Name shown instead of the key.</summary>
    public string DispName;
    /// <summary>Position inside its category: a higher number is shown higher (ConfigOrder.First / Last, rule 36).</summary>
    public int? Order;
    /// <summary>True: shown but not editable.</summary>
    public bool? ReadOnly;
    /// <summary>True: shown only when the manager's "advanced" switch is on.</summary>
    public bool? IsAdvanced;
    /// <summary>Value to text for the manager's text box.</summary>
    public Func<object, string> ObjToStr;
    /// <summary>Text to value for the manager's text box.</summary>
    public Func<string, object> StrToObj;
    /// <summary>True: only an admin (or the host) may change it; the server's value is used (KyNetwork.ShareAdminOnly, KY hub lock).</summary>
    public bool? IsAdminOnly;
}
