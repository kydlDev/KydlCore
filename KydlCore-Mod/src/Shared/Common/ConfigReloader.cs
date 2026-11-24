using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using BepInEx.Configuration;
using UnityEngine;
using KydlCore.Core;

namespace KydlCore.Common;

/// <summary>
/// SHARED. Reloads the config when the .cfg file is saved on disk. The watcher only sets a flag (it runs on another thread); Tick (from
/// Plugin.Update) reloads on the main thread 0.5 s later, because editors save in several writes. Dispose from Plugin.OnDestroy.
/// </summary>
public sealed class ConfigReloader : IDisposable
{
    readonly ConfigFile _config;
    readonly ModContext _ctx;
    FileSystemWatcher _watcher;
    volatile bool _changed;
    float _reloadAt;
    int _retries;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ConfigReloader(ConfigFile config)
    {
        _ctx = ModContexts.For(Assembly.GetCallingAssembly());
        _config = config;
        // PROJECT_RULES 17 (hot reload) needs proof in the log: every changed player setting is written with its new value, whether it
        // came from the .cfg file, Configuration Manager or code (RearViewCamera T-G009: FOV / mirror changes left no trace).
        _config.SettingChanged += (s, e) =>
        {
            var entry = e != null ? e.ChangedSetting : null;
            if (entry == null) { if (CoreLog.On) ModLog.Verbose("ConfigReloader.ConfigReloader: returns (line 112)"); return; }
            ModLog.Info(_ctx, $"Config: [{entry.Definition.Section}] {entry.Definition.Key} = {entry.BoxedValue} (applied live)");
        };
        try
        {
            var path = config.ConfigFilePath;
            _watcher = new FileSystemWatcher(Path.GetDirectoryName(path), Path.GetFileName(path))
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            };
            _watcher.Changed += (s, e) => _changed = true;
            _watcher.Created += (s, e) => _changed = true;
            _watcher.Renamed += (s, e) => _changed = true;     // some editors save to a temp file and rename it over the config
            _watcher.EnableRaisingEvents = true;
            ModLog.Verbose(_ctx, $"ConfigReloader: watching {path}");
        }
        catch (ArgumentException e) { ModLog.Warn(_ctx, $"Config file watcher not started (bad path; edits to the .cfg need a restart): {e.Message}"); }
        catch (IOException e) { ModLog.Warn(_ctx, $"Config file watcher not started (edits to the .cfg need a restart): {e.Message}"); }
    }

    public bool Watching => _watcher != null && _watcher.EnableRaisingEvents;

    /// Every frame. Expected outcome: true when the config was reloaded in this call.
    public bool Tick()
    {
        if (_changed) { _changed = false; _reloadAt = Time.realtimeSinceStartup + 0.5f; _retries = 0; }
        if (_reloadAt <= 0f || Time.realtimeSinceStartup < _reloadAt) { if (CoreLog.Due("ConfigReloader.Tick:138", 2d)) ModLog.Every("ConfigReloader.Tick:138", 2d, "ConfigReloader.Tick: returns false (line 138)"); return false; }
        _reloadAt = 0f;
        try
        {
            if (!File.Exists(_config.ConfigFilePath)) { ModLog.Verbose(_ctx, "ConfigReloader: the file is gone, nothing reloaded"); return false; }
            _config.Reload();                      // raises SettingChanged for every entry whose value differs
            ModLog.Info(_ctx, "Config file changed on disk - reloaded.");
            return true;
        }
        catch (IOException e)
        {
            if (++_retries <= 3) { _reloadAt = Time.realtimeSinceStartup + 1f; ModLog.Verbose(_ctx, $"ConfigReloader: file locked, retry {_retries}: {e.Message}"); }
            else ModLog.Warn(_ctx, $"Config reload failed: {e.Message}");
            return false;
        }
    }

    public void Dispose()
    {
        if (_watcher == null) { if (CoreLog.Due("ConfigReloader.Dispose:157", 2d)) ModLog.Every("ConfigReloader.Dispose:157", 2d, "ConfigReloader.Dispose: returns (line 157)"); return; }
        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();
        _watcher = null;
        ModLog.Verbose(_ctx, "ConfigReloader: stopped");
    }
}
