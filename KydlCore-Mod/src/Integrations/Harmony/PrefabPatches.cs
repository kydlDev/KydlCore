using HarmonyLib;
using KydlCore.Common;

namespace KydlCore.Integrations.Harmony;

/// <summary>ZNetScene wakes up when a world loads: every prefab a mod added (KyPrefabs.Add) goes into it (Jotunn PrefabManager's job before 1.2.0, D-104).</summary>
[HarmonyPatch(typeof(ZNetScene), "Awake")]
internal static class ZNetSceneAwakePrefabs
{
    [HarmonyPriority(Priority.High)]
    static void Postfix(ZNetScene __instance)
    {
        int put = KyPrefabs.RegisterAll(__instance);
        ModLog.Verbose($"ZNetSceneAwakePrefabs: {put} added prefab(s) registered, scene prefabs={__instance.m_prefabs.Count}");
    }
}

/// <summary>The main menu copies the game's ObjectDB (FejdStartup): vanilla prefabs can be cloned from now on (Jotunn's OnVanillaPrefabsAvailable).</summary>
[HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
internal static class ObjectDBCopyVanilla
{
    [HarmonyPriority(Priority.Last)]
    static void Prefix(ObjectDB other)
    {
        bool available = KyPrefabs.MarkVanillaAvailable();
        ModLog.Verbose($"ObjectDBCopyVanilla: menu ObjectDB copied (items={(other == null ? 0 : other.m_items.Count)}) - vanilla available={available}");
    }
}
