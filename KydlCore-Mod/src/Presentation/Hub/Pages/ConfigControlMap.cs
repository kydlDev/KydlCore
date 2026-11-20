using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using KydlCore.Core;
using KydlCore.Common;

namespace KydlCore.Presentation.Hub.Pages;

/// <summary>The control the auto "Settings" page shows for one config entry.</summary>
internal enum ControlKind
{
    /// <summary>A type the page cannot edit: shown as read-only text.</summary>
    Unsupported,
    /// <summary>bool: a pill switch.</summary>
    Toggle,
    /// <summary>int / float / double with an AcceptableValueRange: slider + number box.</summary>
    Slider,
    /// <summary>A number without a range (or a long): a text box that only accepts numbers.</summary>
    Number,
    /// <summary>string: a text box.</summary>
    Text,
    /// <summary>An enum (not KeyCode) or a string with an AcceptableValueList: a drop-down.</summary>
    Dropdown,
    /// <summary>UnityEngine.Color: the colour editor.</summary>
    Color,
    /// <summary>UnityEngine.KeyCode: a press-to-rebind button (one key).</summary>
    Key,
    /// <summary>BepInEx KeyboardShortcut: a press-to-rebind button (a key with the Shift / Ctrl / Alt held with it).</summary>
    Shortcut
}

/// <summary>
/// Pure rules of the auto "Settings" page (PLAN T-033, D-023): which control a config entry gets, how number text is read and shown, and how the
/// BepInEx tags "Browsable = false" / "IsAdminOnly = true" are found. No Unity and no BepInEx types, no logging (like WindowStack): the page
/// (ConfigPage) does the drawing and the writing through <c>ConfigEntryBase.BoxedValue</c>. Tags are read by member NAME, because the mods and
/// Jotunn each ship their own <c>ConfigurationManagerAttributes</c> class (Jotunn's is internal; checked in Jotunn.dll 2026-10-30).
/// </summary>
internal static class ConfigControlMap
{
    const string BrowsableName = "Browsable", AdminOnlyName = "IsAdminOnly", ReadOnlyName = "ReadOnly", ColorTypeName = "UnityEngine.Color";
    const string KeyCodeTypeName = "UnityEngine.KeyCode", ShortcutTypeName = "BepInEx.Configuration.KeyboardShortcut";

    /// <summary>The control for a value type. `hasRange` = the entry has an AcceptableValueRange, `hasList` = an AcceptableValueList.</summary>
    public static ControlKind Classify(Type valueType, bool hasRange, bool hasList)
    {
        if (CoreLog.On) ModLog.Every("ConfigControlMap.Classify", 2d, $"ConfigControlMap.Classify(valueType={valueType}, hasRange={hasRange}, hasList={hasList})");
        if (valueType == null) { var returnedAt48 = ControlKind.Unsupported; if (CoreLog.On) ModLog.Every("ConfigControlMap.Classify:48", 2d, $"ConfigControlMap.Classify: returns {returnedAt48} (line 48)"); return returnedAt48; }
        if (valueType == typeof(bool)) { var returnedAt49 = ControlKind.Toggle; if (CoreLog.On) ModLog.Every("ConfigControlMap.Classify:49", 2d, $"ConfigControlMap.Classify: returns {returnedAt49} (line 49)"); return returnedAt49; }
        if (valueType.FullName == KeyCodeTypeName) { var returnedAt50 = ControlKind.Key; if (CoreLog.On) ModLog.Every("ConfigControlMap.Classify:50", 2d, $"ConfigControlMap.Classify: returns {returnedAt50} (line 50)"); return returnedAt50; }            // before the enums: a key is pressed, not picked from ~300 names
        if (valueType.FullName == ShortcutTypeName) { var returnedAt51 = ControlKind.Shortcut; if (CoreLog.On) ModLog.Every("ConfigControlMap.Classify:51", 2d, $"ConfigControlMap.Classify: returns {returnedAt51} (line 51)"); return returnedAt51; }
        if (valueType.IsEnum) { var returnedAt52 = ControlKind.Dropdown; if (CoreLog.On) ModLog.Every("ConfigControlMap.Classify:52", 2d, $"ConfigControlMap.Classify: returns {returnedAt52} (line 52)"); return returnedAt52; }
        if (valueType == typeof(string)) { var returnedAt53 = hasList ? ControlKind.Dropdown : ControlKind.Text; if (CoreLog.On) ModLog.Every("ConfigControlMap.Classify:53", 2d, $"ConfigControlMap.Classify: returns {returnedAt53} (line 53)"); return returnedAt53; }
        if (valueType == typeof(long)) { var returnedAt54 = ControlKind.Number; if (CoreLog.On) ModLog.Every("ConfigControlMap.Classify:54", 2d, $"ConfigControlMap.Classify: returns {returnedAt54} (line 54)"); return returnedAt54; }
        if (valueType == typeof(int) || valueType == typeof(float) || valueType == typeof(double)) { var returnedAt55 = hasRange ? ControlKind.Slider : ControlKind.Number; if (CoreLog.On) ModLog.Every("ConfigControlMap.Classify:55", 2d, $"ConfigControlMap.Classify: returns {returnedAt55} (line 55)"); return returnedAt55; }
        if (valueType.FullName == ColorTypeName) { var returnedAt56 = ControlKind.Color; if (CoreLog.On) ModLog.Every("ConfigControlMap.Classify:56", 2d, $"ConfigControlMap.Classify: returns {returnedAt56} (line 56)"); return returnedAt56; }
        { var returnedAt57 = ControlKind.Unsupported; if (CoreLog.On) ModLog.Every("ConfigControlMap.Classify:57", 2d, $"ConfigControlMap.Classify: returns {returnedAt57} (line 57)"); return returnedAt57; }
    }

    /// <summary>True for the number types of the page: int, long, float, double.</summary>
    public static bool IsNumber(Type valueType)
    {
        if (CoreLog.On) ModLog.Every("ConfigControlMap.IsNumber", 2d, $"ConfigControlMap.IsNumber(valueType={valueType})");
        { var returnedAt64 = valueType == typeof(int) || valueType == typeof(long) || valueType == typeof(float) || valueType == typeof(double); if (CoreLog.On) ModLog.Every("ConfigControlMap.IsNumber:64", 2d, $"ConfigControlMap.IsNumber: returns {returnedAt64} (line 64)"); return returnedAt64; }
    }

    /// <summary>The text of a number as the page shows it: whole numbers plain, fractions with at most 2 decimals, always with a dot.</summary>
    public static string Format(Type valueType, object value)
    {
        if (CoreLog.On) ModLog.Every("ConfigControlMap.Format", 2d, $"ConfigControlMap.Format(valueType={valueType}, value={value})");
        if (value == null || !IsNumber(valueType)) { var returnedAt71 = value == null ? "" : value.ToString(); if (CoreLog.On) ModLog.Every("ConfigControlMap.Format:71", 2d, $"ConfigControlMap.Format: returns {returnedAt71} (line 71)"); return returnedAt71; }
        if (IsWhole(valueType)) { var returnedAt72 = Convert.ToString(value, CultureInfo.InvariantCulture); if (CoreLog.On) ModLog.Every("ConfigControlMap.Format:72", 2d, $"ConfigControlMap.Format: returns {returnedAt72} (line 72)"); return returnedAt72; }       // not through double: a long keeps every digit
        { var returnedAt73 = Convert.ToDouble(value, CultureInfo.InvariantCulture).ToString("0.##", CultureInfo.InvariantCulture); if (CoreLog.On) ModLog.Every("ConfigControlMap.Format:73", 2d, $"ConfigControlMap.Format: returns {returnedAt73} (line 73)"); return returnedAt73; }
    }

    /// <summary>The number format for a slider of this type ("0" for whole numbers, else "0.##").</summary>
    public static string SliderFormat(Type valueType)
    {
        if (CoreLog.On) ModLog.Every("ConfigControlMap.SliderFormat", 2d, $"ConfigControlMap.SliderFormat(valueType={valueType})");
        { var returnedAt80 = IsWhole(valueType) ? "0" : "0.##"; if (CoreLog.On) ModLog.Every("ConfigControlMap.SliderFormat:80", 2d, $"ConfigControlMap.SliderFormat: returns {returnedAt80} (line 80)"); return returnedAt80; }
    }

    /// <summary>
    /// Reads the text a player typed as a number of `valueType` (a dot or a comma as decimal sign, spaces around ignored). Null when it is not a
    /// finite number of that type; a whole-number type rejects a fraction and a value outside its range.
    /// </summary>
    public static object Parse(Type valueType, string text)
    {
        if (CoreLog.On) ModLog.Every("ConfigControlMap.Parse", 2d, $"ConfigControlMap.Parse(valueType={valueType}, text={text})");
        if (!IsNumber(valueType) || text == null) { if (CoreLog.On) ModLog.Every("ConfigControlMap.Parse:90", 2d, "ConfigControlMap.Parse: returns null (line 90)"); return null; }
        string s = text.Trim().Replace(',', '.');
        if (IsWhole(valueType)) { var returnedAt92 = ParseWhole(valueType, s); if (CoreLog.On) ModLog.Every("ConfigControlMap.Parse:92", 2d, $"ConfigControlMap.Parse: returns {returnedAt92} (line 92)"); return returnedAt92; }
        double d;
        if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out d) || double.IsNaN(d) || double.IsInfinity(d)) { if (CoreLog.On) ModLog.Every("ConfigControlMap.Parse:94", 2d, "ConfigControlMap.Parse: returns null (line 94)"); return null; }
        if (valueType == typeof(float)) { var returnedAt95 = Math.Abs(d) > float.MaxValue ? null : (object)(float)d; if (CoreLog.On) ModLog.Every("ConfigControlMap.Parse:95", 2d, $"ConfigControlMap.Parse: returns {returnedAt95} (line 95)"); return returnedAt95; }
        { var returnedAt96 = d; if (CoreLog.On) ModLog.Every("ConfigControlMap.Parse:96", 2d, $"ConfigControlMap.Parse: returns {returnedAt96} (line 96)"); return returnedAt96; }
    }

    /// <summary>`value` limited to min..max (the entry's range) and turned into `valueType`; whole-number types round to the nearest integer.</summary>
    public static object Clamp(Type valueType, double value, double min, double max)
    {
        if (CoreLog.On) ModLog.Every("ConfigControlMap.Clamp", 2d, $"ConfigControlMap.Clamp(valueType={valueType}, value={value}, min={min}, max={max})");
        double d = Math.Max(min, Math.Min(max, value));
        if (valueType == typeof(int)) { var returnedAt104 = (int)Math.Round(d); if (CoreLog.On) ModLog.Every("ConfigControlMap.Clamp:104", 2d, $"ConfigControlMap.Clamp: returns {returnedAt104} (line 104)"); return returnedAt104; }
        if (valueType == typeof(long)) { var returnedAt105 = (long)Math.Round(d); if (CoreLog.On) ModLog.Every("ConfigControlMap.Clamp:105", 2d, $"ConfigControlMap.Clamp: returns {returnedAt105} (line 105)"); return returnedAt105; }
        if (valueType == typeof(float)) { var returnedAt106 = (float)d; if (CoreLog.On) ModLog.Every("ConfigControlMap.Clamp:106", 2d, $"ConfigControlMap.Clamp: returns {returnedAt106} (line 106)"); return returnedAt106; }
        { var returnedAt107 = d; if (CoreLog.On) ModLog.Every("ConfigControlMap.Clamp:107", 2d, $"ConfigControlMap.Clamp: returns {returnedAt107} (line 107)"); return returnedAt107; }
    }

    /// <summary>True when a tag says Browsable = false: the entry is not shown on the page.</summary>
    public static bool IsHidden(IEnumerable<object> tags)
    {
        if (CoreLog.On) ModLog.Every("ConfigControlMap.IsHidden", 2d, $"ConfigControlMap.IsHidden(tags={tags})");
        { var returnedAt114 = Flag(tags, BrowsableName) == false; if (CoreLog.On) ModLog.Every("ConfigControlMap.IsHidden:114", 2d, $"ConfigControlMap.IsHidden: returns {returnedAt114} (line 114)"); return returnedAt114; }
    }

    /// <summary>True when a tag says IsAdminOnly = true: only an admin (or the host) may change the entry.</summary>
    public static bool IsAdminOnly(IEnumerable<object> tags)
    {
        if (CoreLog.On) ModLog.Every("ConfigControlMap.IsAdminOnly", 2d, $"ConfigControlMap.IsAdminOnly(tags={tags})");
        { var returnedAt121 = Flag(tags, AdminOnlyName) == true; if (CoreLog.On) ModLog.Every("ConfigControlMap.IsAdminOnly:121", 2d, $"ConfigControlMap.IsAdminOnly: returns {returnedAt121} (line 121)"); return returnedAt121; }
    }

    /// <summary>
    /// True when a tag says ReadOnly = true. ServerSync (the library most server-synced mods carry) sets and keeps this flag itself: true on a
    /// client that may not change the server's values, false for an admin, the host and single player. So a synced entry is editable on the page
    /// exactly when ServerSync lets this player change it.
    /// </summary>
    public static bool IsReadOnly(IEnumerable<object> tags)
    {
        if (CoreLog.On) ModLog.Every("ConfigControlMap.IsReadOnly", 2d, $"ConfigControlMap.IsReadOnly(tags={tags})");
        { var returnedAt132 = Flag(tags, ReadOnlyName) == true; if (CoreLog.On) ModLog.Every("ConfigControlMap.IsReadOnly:132", 2d, $"ConfigControlMap.IsReadOnly: returns {returnedAt132} (line 132)"); return returnedAt132; }
    }

    /// <summary>The first tag member called `name` (public or not, field or property) of type bool / bool?; null when no tag has one or it is null.</summary>
    static bool? Flag(IEnumerable<object> tags, string name)
    {
        if (CoreLog.On) ModLog.Every("ConfigControlMap.Flag", 2d, $"ConfigControlMap.Flag(tags={tags}, name={name})");
        if (tags == null) { if (CoreLog.On) ModLog.Every("ConfigControlMap.Flag:139", 2d, "ConfigControlMap.Flag: returns null (line 139)"); return null; }
        foreach (object tag in tags)
        {
            bool? v = tag == null ? null : MemberFlag(tag, name);
            if (v.HasValue) { var returnedAt143 = v; if (CoreLog.On) ModLog.Every("ConfigControlMap.Flag:143", 2d, $"ConfigControlMap.Flag: returns {returnedAt143} (line 143)"); return returnedAt143; }
        }
        { if (CoreLog.On) ModLog.Every("ConfigControlMap.Flag:145", 2d, "ConfigControlMap.Flag: returns null (line 145)"); return null; }
    }

    static bool? MemberFlag(object tag, string name)
    {
        if (CoreLog.On) ModLog.Every("ConfigControlMap.MemberFlag", 2d, $"ConfigControlMap.MemberFlag(tag={tag}, name={name})");
        const BindingFlags all = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type t = tag.GetType();
        FieldInfo f = t.GetField(name, all);
        if (f != null) { var returnedAt154 = f.GetValue(tag) as bool?; if (CoreLog.On) ModLog.Every("ConfigControlMap.MemberFlag:154", 2d, $"ConfigControlMap.MemberFlag: returns {returnedAt154} (line 154)"); return returnedAt154; }
        PropertyInfo p = t.GetProperty(name, all);
        { var returnedAt156 = p != null && p.GetIndexParameters().Length == 0 ? p.GetValue(tag, null) as bool? : null; if (CoreLog.On) ModLog.Every("ConfigControlMap.MemberFlag:156", 2d, $"ConfigControlMap.MemberFlag: returns {returnedAt156} (line 156)"); return returnedAt156; }
    }

    static bool IsWhole(Type valueType)
    {
        if (CoreLog.On) ModLog.Every("ConfigControlMap.IsWhole", 2d, $"ConfigControlMap.IsWhole(valueType={valueType})");
        { var returnedAt162 = valueType == typeof(int) || valueType == typeof(long); if (CoreLog.On) ModLog.Every("ConfigControlMap.IsWhole:162", 2d, $"ConfigControlMap.IsWhole: returns {returnedAt162} (line 162)"); return returnedAt162; }
    }

    static object ParseWhole(Type valueType, string s)
    {
        if (CoreLog.On) ModLog.Every("ConfigControlMap.ParseWhole", 2d, $"ConfigControlMap.ParseWhole(valueType={valueType}, s={s})");
        long n;
        if (!long.TryParse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out n)) { if (CoreLog.On) ModLog.Every("ConfigControlMap.ParseWhole:169", 2d, "ConfigControlMap.ParseWhole: returns null (line 169)"); return null; }
        if (valueType == typeof(long)) { var returnedAt170 = n; if (CoreLog.On) ModLog.Every("ConfigControlMap.ParseWhole:170", 2d, $"ConfigControlMap.ParseWhole: returns {returnedAt170} (line 170)"); return returnedAt170; }
        { var returnedAt171 = n < int.MinValue || n > int.MaxValue ? null : (object)(int)n; if (CoreLog.On) ModLog.Every("ConfigControlMap.ParseWhole:171", 2d, $"ConfigControlMap.ParseWhole: returns {returnedAt171} (line 171)"); return returnedAt171; }
    }
}
