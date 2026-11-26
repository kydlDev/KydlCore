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

/// <summary>A world's ObjectDB woke up: KY-Items contributions (items, recipes, pieces) go in (ValheimItems; Jotunn's ItemManager / PieceManager before 1.2.0).</summary>
[HarmonyPatch(typeof(ObjectDB), "Awake")]
internal static class ObjectDBAwakeItems
{
    [HarmonyPriority(Priority.Low)]
    static void Postfix(ObjectDB __instance)
    {
        int applied = KydlCore.Integrations.Valheim.ValheimItems.OnObjectDB(__instance, true);
        ModLog.Verbose($"ObjectDBAwakeItems: {applied} contribution(s) in ObjectDB ({__instance.m_items.Count} items, {__instance.m_recipes.Count} recipes)");
    }
}

/// <summary>The menu's ObjectDB copied the game's: contributed ITEMS go in (character preview, item lookups); pieces need the world.</summary>
[HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
internal static class ObjectDBCopyItems
{
    [HarmonyPriority(Priority.Low)]
    static void Postfix(ObjectDB __instance)
    {
        int applied = KydlCore.Integrations.Valheim.ValheimItems.OnObjectDB(__instance, false);
        ModLog.Verbose($"ObjectDBCopyItems: {applied} item contribution(s) in the menu ObjectDB ({__instance.m_items.Count} items)");
    }
}
