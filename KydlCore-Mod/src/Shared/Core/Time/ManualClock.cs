namespace KydlCore.Core;

/// <summary>A clock tests move by hand.</summary>
public sealed class ManualClock : IClock
{
    public float Now { get; private set; }
    public ManualClock(float start = 0f) { Now = start; }
    /// Moves the clock. Expected outcome: Now grew by `seconds` (negative values are ignored).
    public bool Advance(float seconds)
    {
        if (CoreLog.On) CoreLog.Verbose($"ManualClock.Advance(seconds={CoreLog.Shown(seconds)})");
        float before = Now;
        if (seconds > 0f) Now += seconds;
        { var returnedAt21 = Now >= before; if (CoreLog.On) CoreLog.Verbose($"ManualClock.Advance: returns {CoreLog.Shown(returnedAt21)} (line 21)"); return returnedAt21; }
    }
}
