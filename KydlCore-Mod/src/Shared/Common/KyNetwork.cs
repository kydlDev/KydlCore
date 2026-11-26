using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using KydlCore.Core;
using KydlCore.ServerSync;

namespace KydlCore.Common;

/// <summary>
/// SHARED (KydlCore 1.2.0, D-104: no Jotunn). The network side of a KydlDev mod through KydlCore.ServerSync: the version check when a player
/// joins a server (replaces Jotunn's [NetworkCompatibility]) and the settings the server decides (replaces Jotunn's IsAdminOnly sync).
/// Call <see cref="Register"/> once in Awake (before the settings it shares are bound into it), then <see cref="Share{T}"/> or
/// <see cref="ShareAdminOnly"/> for the settings a non-admin player may not change.
/// </summary>
public static class KyNetwork
{
    static readonly Dictionary<string, ConfigSync> Syncs = new();

    /// <summary>
    /// The ServerSync instance of a mod: the same one for every call with the same guid. Expected outcome: a sync whose CurrentVersion is
    /// <paramref name="version"/> and whose rule follows <paramref name="level"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ConfigSync Register(string guid, string displayName, string version, KyNetworkLevel level)
    {
        var ctx = ModContexts.For(Assembly.GetCallingAssembly());
        string key = guid ?? "";
        lock (Syncs)
        {
            if (Syncs.TryGetValue(key, out ConfigSync known))
            {
                ModLog.Verbose(ctx, $"KyNetwork.Register: {key} already registered (version {known.CurrentVersion}, required {known.ModRequired}) - returns that sync");
                return known;
            }
            var sync = new ConfigSync(key)
            {
                DisplayName = displayName,
                CurrentVersion = version,
                MinimumRequiredVersion = version,        // both sides demand the other's own version: equal versions only (old Patch strictness)
                ModRequired = KyNetworkRules.ModRequired(level),
                AnnounceVersion = true,                  // a client-only mod still tells the server its version, so a mismatch is caught
                IsLocked = true,                         // shared settings: only an admin (or the server) changes them
            };
            Syncs[key] = sync;
            ModLog.Info(ctx, $"KyNetwork: {displayName} {version} registered - level={level}, required on every player={sync.ModRequired}");
            return sync;
        }
    }

    /// <summary>The setting is sent by the server and locked for players who are not admins. Expected outcome: the same entry, now synced.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ConfigEntry<T> Share<T>(ConfigSync sync, ConfigEntry<T> entry)
    {
        var ctx = ModContexts.For(Assembly.GetCallingAssembly());
        if (sync == null || entry == null)
        {
            ModLog.Warn(ctx, $"KyNetwork.Share: nothing shared - sync={(sync == null ? "<none>" : sync.Name)}, entry={(entry == null ? "<none>" : entry.Definition.ToString())}");
            return entry;
        }
        sync.AddConfigEntry(entry);
        ModLog.Verbose(ctx, $"KyNetwork.Share: [{entry.Definition.Section}] {entry.Definition.Key} is decided by the server (mod {sync.Name})");
        return entry;
    }

    /// <summary>
    /// Shares every bound entry of <paramref name="config"/> whose ConfigurationManagerAttributes tag says IsAdminOnly = true (what Jotunn's
    /// SynchronizationManager did by itself). Call after the last Bind. Expected outcome: the number of entries shared.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int ShareAdminOnly(ConfigSync sync, ConfigFile config)
    {
        var ctx = ModContexts.For(Assembly.GetCallingAssembly());
        int shared = 0, seen = 0;
        if (sync == null || config == null)
        {
            ModLog.Warn(ctx, $"KyNetwork.ShareAdminOnly: nothing shared - sync={(sync == null ? "<none>" : sync.Name)}, config={(config == null ? "<none>" : config.ConfigFilePath)}");
            return shared;
        }
        foreach (ConfigEntryBase entry in new List<ConfigEntryBase>(((IDictionary<ConfigDefinition, ConfigEntryBase>)config).Values))
        {
            seen++;
            if (!KyNetworkRules.IsAdminOnly(entry.Description == null ? null : entry.Description.Tags)) continue;
            AddUntyped(sync, entry);
            shared++;
            ModLog.Verbose(ctx, $"KyNetwork.ShareAdminOnly: [{entry.Definition.Section}] {entry.Definition.Key} is admin-only - decided by the server");
        }
        ModLog.Verbose(ctx, $"KyNetwork.ShareAdminOnly: {shared} of {seen} entr{(seen == 1 ? "y" : "ies")} of {sync.Name} shared");
        return shared;
    }

    /// <summary>ConfigSync.AddConfigEntry is generic: called through reflection with the entry's own setting type.</summary>
    static bool AddUntyped(ConfigSync sync, ConfigEntryBase entry)
    {
        MethodInfo generic = typeof(ConfigSync).GetMethod(nameof(ConfigSync.AddConfigEntry));
        object result = generic.MakeGenericMethod(entry.SettingType).Invoke(sync, new object[] { entry });
        if (CoreLog.On) ModLog.Verbose($"KyNetwork.AddUntyped: [{entry.Definition.Section}] {entry.Definition.Key} as {entry.SettingType.Name} - added={result != null}");
        return result != null;
    }
}
