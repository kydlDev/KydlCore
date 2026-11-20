using System;
using System.Collections.Generic;

namespace KydlCore.Core;

/// <summary>
/// The mod's list of features (MIGRATION_DECISIONS A4). <see cref="Refresh"/> compares each feature's gate with its running state and
/// starts / stops the ones that differ; call it once after Bootstrap and on every config change (ConfigReloader / SettingChanged).
/// A feature that throws in Start / Stop is reported as Failed and stays in its old state; the other features are not affected.
/// Game-free: the trace sink is ModLog.Verbose in game, null in tests.
/// </summary>
public sealed class FeatureRegistry
{
    readonly List<IFeature> _features = new();
    readonly HashSet<string> _running = new(StringComparer.OrdinalIgnoreCase);
    readonly Action<string> _trace;
    const bool BrokenGateIsOn = false;
    const bool ThrownMeansFinished = false;

    public FeatureRegistry(Action<string> trace = null) { _trace = trace; }

    public int Count => _features.Count;
    public IReadOnlyList<IFeature> All => _features;

    public bool IsRunning(string id) { return id != null && _running.Contains(id); }

    /// Adds a feature (not started yet). Expected outcome: Success with the feature count; Rejected for null or a duplicate id.
    public OperationResponse<int> Add(IFeature feature)
    {
        if (CoreLog.On) CoreLog.Every("FeatureRegistry.Add", 2d, $"FeatureRegistry.Add(feature={feature})");
        if (feature == null) { var returnedAt31 = OperationResponse<int>.Rejected("null feature"); if (CoreLog.On) CoreLog.Every("FeatureRegistry.Add:31", 2d, $"FeatureRegistry.Add: returns {returnedAt31} (line 31)"); return returnedAt31; }
        if (string.IsNullOrEmpty(feature.Id)) { var returnedAt32 = OperationResponse<int>.Rejected("feature without an id"); if (CoreLog.On) CoreLog.Every("FeatureRegistry.Add:32", 2d, $"FeatureRegistry.Add: returns {returnedAt32} (line 32)"); return returnedAt32; }
        foreach (var f in _features)
            if (string.Equals(f.Id, feature.Id, StringComparison.OrdinalIgnoreCase)) { var returnedAt34 = OperationResponse<int>.Rejected("duplicate feature id '" + feature.Id + "'"); if (CoreLog.On) CoreLog.Every("FeatureRegistry.Add:34", 2d, $"FeatureRegistry.Add: returns {returnedAt34} (line 34)"); return returnedAt34; }
        _features.Add(feature);
        Trace("FeatureRegistry: added '" + feature.Id + "' (" + _features.Count + " features)");
        { var returnedAt37 = OperationResponse<int>.Success(_features.Count); if (CoreLog.On) CoreLog.Every("FeatureRegistry.Add:37", 2d, $"FeatureRegistry.Add: returns {returnedAt37} (line 37)"); return returnedAt37; }
    }

    /// Starts / stops features whose gate changed. Expected outcome: Success with the number of features changed (0 = nothing to do),
    /// Failed when at least one Start / Stop threw or reported it did not finish (the reason names them).
    public OperationResponse<int> Refresh()
    {
        if (CoreLog.On) CoreLog.Every("FeatureRegistry.Refresh", 2d, $"FeatureRegistry.Refresh()");
        int changed = 0;
        string failed = null;
        foreach (var f in _features)
        {
            bool want = SafeGate(f);
            bool running = _running.Contains(f.Id);
            if (want == running) continue;
            bool ok = want ? Run(f, true) : Run(f, false);
            if (!ok) { failed = failed == null ? f.Id : failed + ", " + f.Id; continue; }
            if (want) _running.Add(f.Id); else _running.Remove(f.Id);
            changed++;
            Trace("FeatureRegistry: '" + f.Id + "' " + (want ? "started" : "stopped"));
        }
        if (failed != null) { var returnedAt58 = OperationResponse<int>.Failed("could not switch: " + failed); if (CoreLog.On) CoreLog.Every("FeatureRegistry.Refresh:58", 2d, $"FeatureRegistry.Refresh: returns {returnedAt58} (line 58)"); return returnedAt58; }
        { var returnedAt59 = OperationResponse<int>.Success(changed, changed == 0 ? "no change" : changed + " feature(s) switched"); if (CoreLog.On) CoreLog.Every("FeatureRegistry.Refresh:59", 2d, $"FeatureRegistry.Refresh: returns {returnedAt59} (line 59)"); return returnedAt59; }
    }

    /// Stops every running feature (mod unload). Expected outcome: Success with the number stopped; Failed when one could not stop.
    public OperationResponse<int> StopAll()
    {
        if (CoreLog.On) CoreLog.Verbose($"FeatureRegistry.StopAll()");
        int stopped = 0;
        string failed = null;
        foreach (var f in _features)
        {
            if (!_running.Contains(f.Id)) continue;
            if (!Run(f, false)) { failed = failed == null ? f.Id : failed + ", " + f.Id; continue; }
            _running.Remove(f.Id);
            stopped++;
        }
        if (failed != null) { var returnedAt75 = OperationResponse<int>.Failed("could not stop: " + failed); if (CoreLog.On) CoreLog.Verbose($"FeatureRegistry.StopAll: returns {returnedAt75} (line 75)"); return returnedAt75; }
        { var returnedAt76 = OperationResponse<int>.Success(stopped); if (CoreLog.On) CoreLog.Verbose($"FeatureRegistry.StopAll: returns {returnedAt76} (line 76)"); return returnedAt76; }
    }

    bool SafeGate(IFeature f)
    {
        if (CoreLog.On) CoreLog.Every("FeatureRegistry.SafeGate", 2d, $"FeatureRegistry.SafeGate(f={f})");
        bool on;
        try { on = f.IsEnabled(); }
        catch (Exception e)       // last resort: a broken gate counts as "off", never takes the other features down
        {
            Trace("FeatureRegistry: gate of '" + f.Id + "' threw " + e.GetType().Name + ": " + e.Message + " - treated as off");
            on = BrokenGateIsOn;
        }
        { var returnedAt89 = on; if (CoreLog.On) CoreLog.Every("FeatureRegistry.SafeGate:89", 2d, $"FeatureRegistry.SafeGate: returns {returnedAt89} (line 89)"); return returnedAt89; }
    }

    bool Run(IFeature f, bool start)
    {
        if (CoreLog.On) CoreLog.Every("FeatureRegistry.Run", 2d, $"FeatureRegistry.Run(f={f}, start={start})");
        bool finished;
        try
        {
            finished = start ? f.Start() : f.Stop();
            if (!finished) Trace("FeatureRegistry: '" + f.Id + "' " + (start ? "Start" : "Stop") + " did not finish");
        }
        catch (Exception e)       // last resort: report the feature, keep the registry consistent
        {
            Trace("FeatureRegistry: '" + f.Id + "' " + (start ? "Start" : "Stop") + " threw " + e.GetType().Name + ": " + e.Message);
            finished = ThrownMeansFinished;
        }
        { var returnedAt106 = finished; if (CoreLog.On) CoreLog.Every("FeatureRegistry.Run:106", 2d, $"FeatureRegistry.Run: returns {returnedAt106} (line 106)"); return returnedAt106; }
    }

    void Trace(string line) { if (_trace != null) _trace(line); }
}
