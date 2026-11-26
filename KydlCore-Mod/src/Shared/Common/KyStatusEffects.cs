using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using KydlCore.Core;

namespace KydlCore.Common;

/// <summary>
/// SHARED (KydlCore 1.2.0, replaces Jotunn's CustomStatusEffect + ItemManager.AddStatusEffect, D-104). A mod hands its StatusEffect (a ScriptableObject,
/// usually ScriptableObject.CreateInstance or a clone) to <see cref="Add"/> once; KydlCore puts it into every ObjectDB that wakes up or copies the menu one
/// (Integrations/Harmony/PrefabPatches), so ObjectDB.GetStatusEffect(name hash) finds it in the menu and in every world.
/// </summary>
public static class KyStatusEffects
{
    static readonly List<StatusEffect> Added = new();

    /// <summary>Keeps the effect for every ObjectDB (and puts it into the current one at once). Expected outcome: true when it is in the list (false for null or a second effect with the same name).</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Add(StatusEffect effect)
    {
        var ctx = ModContexts.For(Assembly.GetCallingAssembly());
        if (effect == null) { ModLog.Warn(ctx, "KyStatusEffects.Add: null effect ignored"); return false; }
        StatusEffect same = Added.Find(e => e != null && e.name == effect.name);
        if (same != null && same != effect) { ModLog.Warn(ctx, $"KyStatusEffects.Add: '{effect.name}' refused - another effect with this name was added before"); return false; }
        if (same == null) Added.Add(effect);
        int now = ObjectDB.instance != null ? PutInto(ObjectDB.instance) : 0;
        ModLog.Verbose(ctx, $"KyStatusEffects: '{effect.name}' ({effect.m_name}) added - total {Added.Count}, put into the current ObjectDB now={now}");
        return Added.Contains(effect);
    }

    /// <summary>Called by the ObjectDB patches: every added effect that the ObjectDB does not have yet goes in. Expected outcome: the number put in.</summary>
    internal static int PutInto(ObjectDB db)
    {
        int put = 0;
        if (db == null) { ModLog.Verbose("KyStatusEffects.PutInto: no ObjectDB - nothing put in"); return put; }
        foreach (StatusEffect effect in Added)
        {
            if (effect == null || db.m_StatusEffects.Exists(e => e != null && e.name == effect.name)) continue;
            db.m_StatusEffects.Add(effect);
            put++;
        }
        if (CoreLog.On || put > 0) ModLog.Verbose($"KyStatusEffects.PutInto: {put} of {Added.Count} effect(s) put into ObjectDB '{db.name}' (now {db.m_StatusEffects.Count})");
        return put;
    }
}
