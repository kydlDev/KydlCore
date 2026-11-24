using System;
using System.Collections.Generic;
using KydlCore.Core;
using KydlCore.Common;

namespace KydlCore.Features.Hub.Contracts;

/// <summary>A slider (made by <see cref="KySection.Slider"/>).</summary>
public sealed class KySliderRow : KyRow
{
    /// <summary>Lowest value.</summary>
    public float Min { get; private set; }
    /// <summary>Highest value.</summary>
    public float Max { get; private set; }
    /// <summary>The value shown.</summary>
    public float Value { get; private set; }
    /// <summary>The .NET number format of the shown value.</summary>
    public string Format { get; private set; }
    /// <summary>Called with the new value.</summary>
    public Action<float> Changed { get; private set; }
    internal KySliderRow(string label, float min, float max, float value, string format, Action<float> changed) : base(label)
    {
        if (CoreLog.Due("KySliderRow.KySliderRow", 2d)) ModLog.Every("KySliderRow.KySliderRow", 2d, $"KySliderRow.KySliderRow(label={CoreLog.Shown(label)}, min={CoreLog.Shown(min)}, max={CoreLog.Shown(max)}, value={CoreLog.Shown(value)}, format={CoreLog.Shown(format)})");
        Min = min; Max = max < min ? min : max; Value = value; Format = string.IsNullOrEmpty(format) ? "0.##" : format; Changed = changed;
    }
}
