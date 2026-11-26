using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using KydlCore.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KydlCore.Common;

/// <summary>
/// SHARED (KydlCore 1.2.0, replaces Jotunn's PrefabManager, D-104). Clones and new prefabs live under one inactive holder object (so their
/// Awake does not run while they are templates), <see cref="Add"/> puts a prefab into the game's network scene (ZNetScene) - at once when the
/// world is loaded, else when ZNetScene wakes up (Integrations/Harmony/PrefabPatches) - and <see cref="Get"/> finds a prefab by name in the mods'
/// own list, the network scene, ObjectDB and finally every loaded asset. Items, recipes and pieces are added through KyItems, not here.
/// </summary>
public static class KyPrefabs
{
    static readonly Dictionary<int, GameObject> Added = new();
    static readonly Dictionary<Type, Dictionary<string, Object>> AssetCache = new();
    static GameObject _holder;

    /// <summary>Raised every time the game copies its ObjectDB in the main menu: from then on vanilla prefabs can be cloned (Jotunn's OnVanillaPrefabsAvailable).</summary>
    public static event Action VanillaPrefabsAvailable;

    /// <summary>Raised after ZNetScene woke up and every added prefab is in it (Jotunn's OnPrefabsRegistered).</summary>
    public static event Action PrefabsRegistered;

    /// <summary>True once the game's ObjectDB has been copied in the main menu at least once.</summary>
    public static bool VanillaAvailable { get; private set; }

    /// <summary>The inactive object every template lives under (made on first use, kept across scene loads).</summary>
    public static Transform Holder
    {
        get
        {
            if (_holder == null)
            {
                _holder = new GameObject("KydlCore_Prefabs");
                _holder.SetActive(false);
                Object.DontDestroyOnLoad(_holder);
                ModLog.Verbose($"KyPrefabs: template holder '{_holder.name}' made (inactive, kept across scenes)");
            }
            return _holder.transform;
        }
    }

    /// <summary>A copy of the prefab named <paramref name="baseName"/> (found with <see cref="Get"/>) under the holder, called <paramref name="name"/>. Expected outcome: the clone, or null when the base is not found.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static GameObject Clone(string name, string baseName)
    {
        var ctx = ModContexts.For(Assembly.GetCallingAssembly());
        GameObject source = Find(baseName);
        if (source == null) { ModLog.Warn(ctx, $"KyPrefabs.Clone: '{name}' not made - base prefab '{baseName}' not found (vanilla available={VanillaAvailable})"); return null; }
        return CloneOf(ctx, name, source);
    }

    /// <summary>A copy of <paramref name="source"/> under the holder, called <paramref name="name"/>. Expected outcome: the clone (null for a null source).</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static GameObject Clone(string name, GameObject source)
    {
        var ctx = ModContexts.For(Assembly.GetCallingAssembly());
        if (source == null) { ModLog.Warn(ctx, $"KyPrefabs.Clone: '{name}' not made - the source prefab is null"); return null; }
        return CloneOf(ctx, name, source);
    }

    static GameObject CloneOf(ModContext ctx, string name, GameObject source)
    {
        GameObject clone = Object.Instantiate(source, Holder, false);
        clone.name = name;
        ModLog.Verbose(ctx, $"KyPrefabs: '{name}' cloned from '{source.name}' (children={clone.transform.childCount}, netview={clone.GetComponent<ZNetView>() != null})");
        return clone;
    }

    /// <summary>A new, empty prefab under the holder (with a ZNetView unless <paramref name="addZNetView"/> is false). Expected outcome: the new object.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static GameObject CreateEmpty(string name, bool addZNetView = true)
    {
        var ctx = ModContexts.For(Assembly.GetCallingAssembly());
        var go = new GameObject(name);
        go.transform.SetParent(Holder, false);
        if (addZNetView) go.AddComponent<ZNetView>();
        ModLog.Verbose(ctx, $"KyPrefabs: empty prefab '{name}' made (netview={addZNetView})");
        return go;
    }

    /// <summary>
    /// Puts the prefab into the game's network scene (now when ZNetScene exists, and again every time it wakes up). Expected outcome: true when
    /// it is in the list of added prefabs (false for null or for a second, different prefab with the same name).
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Add(GameObject prefab)
    {
        var ctx = ModContexts.For(Assembly.GetCallingAssembly());
        if (prefab == null) { ModLog.Warn(ctx, "KyPrefabs.Add: null prefab ignored"); return false; }
        int hash = prefab.name.GetStableHashCode();
        if (Added.TryGetValue(hash, out GameObject known) && known != prefab)
        {
            ModLog.Warn(ctx, $"KyPrefabs.Add: '{prefab.name}' refused - another prefab with this name was added before (hash {hash})");
            return false;
        }
        Added[hash] = prefab;
        bool now = ZNetScene.instance != null && Register(ZNetScene.instance, prefab);
        ModLog.Verbose(ctx, $"KyPrefabs: '{prefab.name}' added (hash {hash}, in the network scene now={now}, total added={Added.Count})");
        return Added.ContainsKey(hash);
    }

    /// <summary>
    /// A prefab by name: the mods' added prefabs, then the network scene, then ObjectDB, then every loaded GameObject asset. Expected outcome:
    /// the prefab, or null when none has that name.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static GameObject Get(string name)
    {
        var ctx = ModContexts.For(Assembly.GetCallingAssembly());
        GameObject found = Find(name);
        if (found == null) ModLog.Verbose(ctx, $"KyPrefabs.Get: '{name}' not found (added={Added.Count}, znetscene={ZNetScene.instance != null}, objectdb={ObjectDB.instance != null})");
        return found;
    }

    /// <summary>Any loaded asset of type <typeparamref name="T"/> by name (materials, shaders, sprites ...), cached per type (Jotunn's PrefabManager.Cache). Expected outcome: the asset or null.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T Asset<T>(string name) where T : Object
    {
        var ctx = ModContexts.For(Assembly.GetCallingAssembly());
        T found = AssetOf(typeof(T), name) as T;
        ModLog.Verbose(ctx, $"KyPrefabs.Asset<{typeof(T).Name}>: '{name}' {(found != null ? "found" : "not found")}");
        return found;
    }

    static GameObject Find(string name)
    {
        if (string.IsNullOrEmpty(name)) { ModLog.Verbose($"KyPrefabs.Find: no name given ('{name}') - returns <none>"); return null; }
        int hash = name.GetStableHashCode();
        if (Added.TryGetValue(hash, out GameObject own) && own != null) { if (CoreLog.On) ModLog.Verbose($"KyPrefabs.Find: '{name}' -> added by a mod (hash {hash})"); return own; }
        GameObject scene = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(hash) : null;
        if (scene != null) { if (CoreLog.On) ModLog.Verbose($"KyPrefabs.Find: '{name}' -> network scene prefab '{scene.name}'"); return scene; }
        GameObject item = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(hash) : null;
        GameObject found = item != null ? item : AssetOf(typeof(GameObject), name) as GameObject;
        if (CoreLog.On) ModLog.Verbose($"KyPrefabs.Find: '{name}' -> {(found == null ? "<none>" : found.name)} (scene={scene != null}, objectdb item={item != null})");
        return found;
    }

    static Object AssetOf(Type type, string name)
    {
        if (!AssetCache.TryGetValue(type, out Dictionary<string, Object> map) || (map.TryGetValue(name ?? "", out Object stale) && stale == null))
        {
            map = new Dictionary<string, Object>();
            foreach (Object o in Resources.FindObjectsOfTypeAll(type))
                if (o != null && !map.ContainsKey(o.name)) map[o.name] = o;
            AssetCache[type] = map;
            ModLog.Verbose($"KyPrefabs: asset cache of {type.Name} built - {map.Count} name(s)");
        }
        map.TryGetValue(name ?? "", out Object hit);
        if (CoreLog.On) ModLog.Verbose($"KyPrefabs.AssetOf: {type.Name} '{name}' -> {(hit == null ? "<none>" : hit.name)}");
        return hit;
    }

    /// <summary>Called by the ZNetScene.Awake patch: every added prefab goes into the new scene, then <see cref="PrefabsRegistered"/>. Expected outcome: the number put in.</summary>
    internal static int RegisterAll(ZNetScene scene)
    {
        int put = 0;
        foreach (GameObject prefab in new List<GameObject>(Added.Values))
            if (prefab != null && Register(scene, prefab)) put++;
        ModLog.Verbose($"KyPrefabs: {put} of {Added.Count} added prefab(s) put into the network scene");
        int failed = Raise(PrefabsRegistered, nameof(PrefabsRegistered));
        ModLog.Verbose($"KyPrefabs.RegisterAll: returns {put} (handlers failed={failed})");
        return put;
    }

    /// <summary>Called by the ObjectDB.CopyOtherDB patch (main menu): vanilla prefabs are loaded. Expected outcome: true (the flag is set).</summary>
    internal static bool MarkVanillaAvailable()
    {
        VanillaAvailable = true;
        AssetCache.Clear();          // a new scene brings new assets: rebuild the name maps on the next lookup
        Raise(VanillaPrefabsAvailable, nameof(VanillaPrefabsAvailable));
        ModLog.Verbose($"KyPrefabs: vanilla prefabs available (listeners={(VanillaPrefabsAvailable == null ? 0 : VanillaPrefabsAvailable.GetInvocationList().Length)})");
        return VanillaAvailable;
    }

    static bool Register(ZNetScene scene, GameObject prefab)
    {
        int hash = prefab.name.GetStableHashCode();
        Dictionary<int, GameObject> named = KyPrefabsScene.Named(scene);
        if (named == null) { ModLog.Warn($"KyPrefabs: ZNetScene.m_namedPrefabs not readable - '{prefab.name}' not registered"); return false; }
        if (named.TryGetValue(hash, out GameObject there))
        {
            bool same = there == prefab;
            if (!same) ModLog.Warn($"KyPrefabs: '{prefab.name}' not registered - the network scene already has another prefab with this name");
            return same;
        }
        if (prefab.GetComponent<ZNetView>() != null) scene.m_prefabs.Add(prefab); else scene.m_nonNetViewPrefabs.Add(prefab);
        named[hash] = prefab;
        if (CoreLog.On) ModLog.Verbose($"KyPrefabs.Register: '{prefab.name}' in the network scene (hash {hash}, prefabs={scene.m_prefabs.Count})");
        return named.ContainsKey(hash);
    }

    static int Raise(Action handlers, string what)
    {
        int failed = 0;
        if (handlers == null) { if (CoreLog.On) ModLog.Verbose($"KyPrefabs.Raise: {what} has no listeners - returns {failed}"); return failed; }
        foreach (Action h in handlers.GetInvocationList())
        {
            try { h(); }
            catch (Exception e)       // a mod's handler: KydlCore keeps calling the others and names the mod's method
            {
                failed++;
                ModLog.Warn($"KyPrefabs: a {what} handler ({h.Method.DeclaringType?.FullName}.{h.Method.Name}) threw {e.GetType().FullName}: {e.Message} - the other handlers still run");
            }
        }
        ModLog.Verbose($"KyPrefabs.Raise: {what} done - failed handlers={failed}");
        return failed;
    }
}
