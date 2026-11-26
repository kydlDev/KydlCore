using System.Collections.Generic;
using System.Reflection;

namespace KydlCore.Common;

/// <summary>ZNetScene.m_namedPrefabs is private (assembly_valheim, decompiled 2026-11-26): read once by reflection for KyPrefabs.</summary>
internal static class KyPrefabsScene
{
    static readonly FieldInfo NamedField = typeof(ZNetScene).GetField("m_namedPrefabs", BindingFlags.Instance | BindingFlags.NonPublic);

    /// <summary>The scene's name-hash map. Expected outcome: the map, null when the field is gone (game update).</summary>
    internal static Dictionary<int, UnityEngine.GameObject> Named(ZNetScene scene)
    {
        var map = scene == null || NamedField == null ? null : NamedField.GetValue(scene) as Dictionary<int, UnityEngine.GameObject>;
        if (map == null) ModLog.Verbose($"KyPrefabsScene.Named: no map (scene={scene != null}, field={NamedField != null})");
        return map;
    }
}
