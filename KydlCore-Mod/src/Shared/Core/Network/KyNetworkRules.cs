using System.Collections.Generic;
using System.Reflection;

namespace KydlCore.Core;

/// <summary>The pure decisions of KyNetwork (unit-tested): which level needs the mod on every player, which config tag means admin-only.</summary>
public static class KyNetworkRules
{
    const string AdminOnlyField = "IsAdminOnly";

    /// <summary>EveryoneMustHave = the server refuses a player without the mod. Expected outcome: true only for that level (value 1).</summary>
    public static bool ModRequired(int level)
    {
        bool required = level == 1;
        if (CoreLog.On) CoreLog.Verbose($"KyNetworkRules.ModRequired: level={level} -> required={required}");
        return required;
    }

    /// <summary>Same for the enum of KydlCore.Common (an enum is passed as its number, so this file stays free of the game side).</summary>
    public static bool ModRequired(System.Enum level)
    {
        int value = level == null ? 0 : System.Convert.ToInt32(level, System.Globalization.CultureInfo.InvariantCulture);
        bool required = ModRequired(value);
        if (CoreLog.On) CoreLog.Verbose($"KyNetworkRules.ModRequired: {level} ({value}) -> required={required}");
        return required;
    }

    /// <summary>True when one of the tags has a field or property IsAdminOnly = true (any class named ConfigurationManagerAttributes: KydlCore's, Jotunn's or a mod's own).</summary>
    public static bool IsAdminOnly(IEnumerable<object> tags)
    {
        bool admin = false;
        if (tags != null)
            foreach (object tag in tags)
                if (tag != null && ReadBool(tag, AdminOnlyField) == true) { admin = true; break; }
        if (CoreLog.On) CoreLog.Verbose($"KyNetworkRules.IsAdminOnly: tags={(tags == null ? "<none>" : CoreLog.Shown(tags))} -> admin-only={admin}");
        return admin;
    }

    static bool? ReadBool(object tag, string name)
    {
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        FieldInfo f = tag.GetType().GetField(name, Any);
        object v = f != null ? f.GetValue(tag) : tag.GetType().GetProperty(name, Any)?.GetValue(tag, null);
        bool? read = v is bool b ? b : (bool?)null;
        if (CoreLog.On) CoreLog.Verbose($"KyNetworkRules.ReadBool: {tag.GetType().Name}.{name} = {(read.HasValue ? read.Value.ToString() : "<none>")}");
        return read;
    }
}
