namespace KydlCore.Core;

/// <summary>SHARED pure rules of the feature switches (D-105: was Functions.IsOn in 4+ mods; unit-tested).</summary>
public static class FeatureRules
{
    /// <summary>A functionality is on only when [General] Enable Mod and its own [General] Enable {category} are both on (rule 11). Expected outcome: both switches.</summary>
    public static bool IsOn(bool enableMod, bool own)
    {
        bool on = enableMod && own;
        if (CoreLog.On) CoreLog.Verbose($"FeatureRules.IsOn: enableMod={enableMod}, own={own} -> {on}");
        return on;
    }
}
