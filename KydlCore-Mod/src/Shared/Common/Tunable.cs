using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using KydlCore.Core;

namespace KydlCore.Common;

/// <summary>
/// SHARED. A DEV-ONLY setting: NOT in the player's config file. It has a fixed value chosen by the user; the code reads it like a config entry
/// (`.Value`, `SettingChanged`). The user's own tools mod KydlTools (<workspace>\KydlTools, never published, F7 menu) finds EVERY
/// static Tunable field of every loaded com.kydldev.* mod by reflection, gives it a tab and a KydlTools config entry, and pushes changes in.
/// Nothing is saved by the mod itself: without KydlTools every start uses the fixed value.
/// Rule (user, 2026-10-21): the player config only holds what a player should change; everything else is a Tunable.
/// KydlTools matches the type by its name ("Tunable`1") and the property names below: do not rename them.
/// </summary>
public sealed class Tunable<T>
{
    readonly T _fixed;
    readonly ModContext _ctx;       // the mod that declared this setting: its debug lines carry that mod's name
    T _value;

    public string Name { get; }
    public string Section { get; }
    public string Description { get; }
    public T Default => _fixed;

    /// Set from another mod's config (an add-on sets its base mod's value): KydlTools leaves it alone.
    public bool External { get; private set; }

    /// Optional allowed range (both set or both unset; T must be IComparable). KydlTools shows it as an AcceptableValueRange
    /// (slider, out-of-range values refused); Value clamps into it. Null = no range (all older mods).
    public object Min { get; }
    public object Max { get; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Tunable(string section, string name, T value, string description = "")
        : this(ModContexts.For(Assembly.GetCallingAssembly()), section, name, value, description) { }

    Tunable(ModContext ctx, string section, string name, T value, string description)
    {
        _ctx = ctx;
        Section = section; Name = name; Description = description ?? "";
        _fixed = value; _value = value;
    }

    /// With an allowed range [min, max] (added 2026-10-22 for WhipItUp; additive: the 4-argument constructor is unchanged).
    [MethodImpl(MethodImplOptions.NoInlining)]
    public Tunable(string section, string name, T value, T min, T max, string description = "") : this(ModContexts.For(Assembly.GetCallingAssembly()), section, name, value, description)
    {
        if (CoreLog.On) ModLog.Verbose($"Tunable.Tunable(section={CoreLog.Shown(section)}, name={CoreLog.Shown(name)}, value={CoreLog.Shown(value)}, min={CoreLog.Shown(min)}, max={CoreLog.Shown(max)}, description={CoreLog.Shown(description)})");
        // Stryker disable once Equality : swapping equal values changes nothing
        if (Comparer<T>.Default.Compare(min, max) > 0) { var t = min; min = max; max = t; }
        Min = min; Max = max;
        _fixed = Clamp(value); _value = _fixed;
    }

    T Clamp(T v)
    {
        if (CoreLog.Due("Tunable.Clamp", 2d)) ModLog.Every("Tunable.Clamp", 2d, $"Tunable.Clamp(v={CoreLog.Shown(v)})");
        // Stryker disable once Logical : Min and Max are always both set or both unset (constructors)
        if (Min == null || Max == null) { var returnedAt62 = v; if (CoreLog.Due("Tunable.Clamp:62", 2d)) ModLog.Every("Tunable.Clamp:62", 2d, $"Tunable.Clamp: returns {CoreLog.Shown(returnedAt62)} (line 62)"); return returnedAt62; }
        var c = Comparer<T>.Default;
        // Stryker disable Equality : at v == Min / Max returning the bound IS returning v
        if (c.Compare(v, (T)Min) < 0) { var returnedAt65 = (T)Min; if (CoreLog.Due("Tunable.Clamp:65", 2d)) ModLog.Every("Tunable.Clamp:65", 2d, $"Tunable.Clamp: returns {CoreLog.Shown(returnedAt65)} (line 65)"); return returnedAt65; }
        if (c.Compare(v, (T)Max) > 0) { var returnedAt66 = (T)Max; if (CoreLog.Due("Tunable.Clamp:66", 2d)) ModLog.Every("Tunable.Clamp:66", 2d, $"Tunable.Clamp: returns {CoreLog.Shown(returnedAt66)} (line 66)"); return returnedAt66; }
        // Stryker restore Equality
        { var returnedAt68 = v; if (CoreLog.Due("Tunable.Clamp:68", 2d)) ModLog.Every("Tunable.Clamp:68", 2d, $"Tunable.Clamp: returns {CoreLog.Shown(returnedAt68)} (line 68)"); return returnedAt68; }
    }

    /// Marks the value as set from another mod's config (External). Returns this Tunable.
    public Tunable<T> SetFromConfigOf(string mod)
    {
        External = true;
        ModLog.Verbose(_ctx, "Tunable: " + Section + "." + Name + " is set from the config of " + mod);
        return this;
    }

    public T Value
    {
        get => _value;
        set
        {
            var clamped = Clamp(value);
            // Stryker disable once all : only decides whether a debug line is written (the logger itself is tested in ModLogTests)
            if (!Equals(clamped, value)) ModLog.Verbose(_ctx, "Tunable: " + Section + "." + Name + " = " + value + " is outside " + Min + ".." + Max + ", using " + clamped);
            value = clamped;
            if (Equals(_value, value)) return;
            var was = _value;
            _value = value;
            ModLog.Verbose(_ctx, "Tunable: " + Section + "." + Name + " = " + value + " (was " + was + ", fixed value " + _fixed + ")");
            SettingChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// Back to the fixed value. Expected outcome: the value in use equals the fixed value.
    public bool Reset() { Value = _fixed; return Equals(_value, _fixed); }

    public event EventHandler SettingChanged;

    public override string ToString() { return Section + "." + Name + " = " + _value; }
}
