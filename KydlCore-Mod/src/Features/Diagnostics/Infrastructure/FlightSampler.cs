using System;
using System.Globalization;
using System.Text;
using HarmonyLib;
using UnityEngine;
using KydlCore.Core;
using KydlCore.Common;

namespace KydlCore.Features.Diagnostics.Infrastructure;

/// <summary>
/// The game side of the flight recorder: reads what a sample needs from the running game (main thread only). Everything here is generic
/// (any creature or object); a mod adds its own facts through the function it passes to FlightRecorder.Track.
///  * Environment: frame rate, the game's simulation area (near / total zones - a lower graphics setting shrank it on 2026-11-05 and far
///    trolls froze), the Unity quality level, the local player's position.
///  * An object: position, health, the AI's target, whether it is alerted, who owns it on the network, whether its zone is loaded, and the
///    distance to the local player.
/// </summary>
internal static class FlightSampler
{
    // MonsterAI keeps its target private (STT reads it the same way, TrollGuard)
    static readonly AccessTools.FieldRef<MonsterAI, Character> TargetOf = AccessTools.FieldRefAccess<MonsterAI, Character>("m_targetCreature");

    /// <summary>One environment line: "fps 78 | sim near 2 total 4 | quality 3 High | player (-21, 51, 69)".</summary>
    internal static string Environment(double fps, int tracked)
    {
        if (CoreLog.Due("FlightSampler.Environment", 2d)) ModLog.Every("FlightSampler.Environment", 2d, $"FlightSampler.Environment(fps={CoreLog.Shown(fps)}, tracked={CoreLog.Shown(tracked)})");
        var sb = new StringBuilder();
        sb.Append("fps ").Append(fps.ToString("0", CultureInfo.InvariantCulture));
        sb.Append(" | sim ").Append(Simulation());
        int q = QualitySettings.GetQualityLevel();
        string[] names = QualitySettings.names;
        sb.Append(" | quality ").Append(q).Append(q >= 0 && q < names.Length ? $" {names[q]}" : "");
        Player p = Player.m_localPlayer;
        sb.Append(" | player ").Append(p != null ? Pos(p.transform.position) : "none");
        sb.Append(" | tracked ").Append(tracked);
        { var returnedAt37 = sb.ToString(); if (CoreLog.Due("FlightSampler.Environment:37", 2d)) ModLog.Every("FlightSampler.Environment:37", 2d, $"FlightSampler.Environment: returns {CoreLog.Shown(returnedAt37)} (line 37)"); return returnedAt37; }
    }

    /// <summary>The game's simulation area in zones ("near 2 total 4"), or "-" before the network is up.</summary>
    internal static string Simulation()
    {
        if (CoreLog.Due("FlightSampler.Simulation", 2d)) ModLog.Every("FlightSampler.Simulation", 2d, $"FlightSampler.Simulation()");
        if (ZNet.instance == null)
        {
            { if (CoreLog.Due("FlightSampler.Simulation:46", 2d)) ModLog.Every("FlightSampler.Simulation:46", 2d, "FlightSampler.Simulation: returns \"-\" (line 46)"); return "-"; }
        }

        SimulationDistance sim = ZNet.instance.GetSyncedSimulationDistance();
        { var returnedAt50 = $"near {sim.NearSimulationDistance} total {sim.TotalSimulationDistance}{(sim.IsClassic ? " classic" : "")}"; if (CoreLog.Due("FlightSampler.Simulation:50", 2d)) ModLog.Every("FlightSampler.Simulation:50", 2d, $"FlightSampler.Simulation: returns {CoreLog.Shown(returnedAt50)} (line 50)"); return returnedAt50; }
    }

    /// <summary>The generic facts of a loaded object: health, AI target, alerted, network owner, zone loaded, distance to the player.</summary>
    internal static string Facts(Character character, MonsterAI ai, ZNetView view, Vector3 position)
    {
        if (CoreLog.Due("FlightSampler.Facts", 2d)) ModLog.Every("FlightSampler.Facts", 2d, $"FlightSampler.Facts(character={CoreLog.Shown(character)}, ai={CoreLog.Shown(ai)}, view={CoreLog.Shown(view)}, position={CoreLog.Shown(position)})");
        var sb = new StringBuilder();
        if (character != null)
        {
            sb.Append("hp ").Append(character.GetHealth().ToString("0", CultureInfo.InvariantCulture)).Append('/')
              .Append(character.GetMaxHealth().ToString("0", CultureInfo.InvariantCulture));
            if (character.IsDead())
            {
                sb.Append(" DEAD");
            }
        }

        if (ai != null)
        {
            Character target = TargetOf(ai);
            Sep(sb).Append("target ").Append(target != null ? target.name : "none");
            if (ai.IsAlerted())
            {
                sb.Append(", alerted");
            }
        }

        if (view != null && view.IsValid())
        {
            Sep(sb).Append("owner ").Append(view.IsOwner() ? "me" : view.GetZDO().GetOwner().ToString(CultureInfo.InvariantCulture));
        }

        { var returnedAt83 = AppendWhere(sb, position).ToString(); if (CoreLog.Due("FlightSampler.Facts:83", 2d)) ModLog.Every("FlightSampler.Facts:83", 2d, $"FlightSampler.Facts: returns {CoreLog.Shown(returnedAt83)} (line 83)"); return returnedAt83; }
    }

    /// <summary>The facts of an object that is not loaded: only its saved data (ZDO) is left.</summary>
    internal static string UnloadedFacts(ZDO zdo, Vector3 position)
    {
        if (CoreLog.Due("FlightSampler.UnloadedFacts", 2d)) ModLog.Every("FlightSampler.UnloadedFacts", 2d, $"FlightSampler.UnloadedFacts(zdo={CoreLog.Shown(zdo)}, position={CoreLog.Shown(position)})");
        var sb = new StringBuilder("NOT LOADED (saved data only)");
        sb.Append(", owner ").Append(zdo.GetOwner() == 0L ? "nobody" : zdo.GetOwner().ToString(CultureInfo.InvariantCulture));
        { var returnedAt92 = AppendWhere(sb, position).ToString(); if (CoreLog.Due("FlightSampler.UnloadedFacts:92", 2d)) ModLog.Every("FlightSampler.UnloadedFacts:92", 2d, $"FlightSampler.UnloadedFacts: returns {CoreLog.Shown(returnedAt92)} (line 92)"); return returnedAt92; }
    }

    static StringBuilder AppendWhere(StringBuilder sb, Vector3 position)
    {
        if (CoreLog.Due("FlightSampler.AppendWhere", 2d)) ModLog.Every("FlightSampler.AppendWhere", 2d, $"FlightSampler.AppendWhere(sb={CoreLog.Shown(sb)}, position={CoreLog.Shown(position)})");
        if (ZoneSystem.instance != null && !ZoneSystem.instance.IsZoneLoaded(position))
        {
            Sep(sb).Append("zone NOT loaded");
        }

        Player p = Player.m_localPlayer;
        if (p != null)
        {
            Sep(sb).Append(Vector3.Distance(p.transform.position, position).ToString("0", CultureInfo.InvariantCulture)).Append(" m from the player");
        }

        { var returnedAt109 = sb; if (CoreLog.Due("FlightSampler.AppendWhere:109", 2d)) ModLog.Every("FlightSampler.AppendWhere:109", 2d, $"FlightSampler.AppendWhere: returns {CoreLog.Shown(returnedAt109)} (line 109)"); return returnedAt109; }
    }

    static StringBuilder Sep(StringBuilder sb) => sb.Length > 0 ? sb.Append(", ") : sb;

    static string Pos(Vector3 v) =>
        $"({v.x.ToString("0", CultureInfo.InvariantCulture)}, {v.y.ToString("0", CultureInfo.InvariantCulture)}, {v.z.ToString("0", CultureInfo.InvariantCulture)})";

    /// <summary>A mod's own facts (its function may throw: that is written into the sample, never thrown on).</summary>
    internal static string ModFacts(Func<string> facts)
    {
        if (CoreLog.Due("FlightSampler.ModFacts", 2d)) ModLog.Every("FlightSampler.ModFacts", 2d, $"FlightSampler.ModFacts(facts={CoreLog.Shown(facts)})");
        if (facts == null)
        {
            { if (CoreLog.Due("FlightSampler.ModFacts:123", 2d)) ModLog.Every("FlightSampler.ModFacts:123", 2d, "FlightSampler.ModFacts: returns \"\" (line 123)"); return ""; }
        }

        try
        {
            string s = facts();
            { var returnedAt129 = string.IsNullOrEmpty(s) ? "" : $" | {s}"; if (CoreLog.Due("FlightSampler.ModFacts:129", 2d)) ModLog.Every("FlightSampler.ModFacts:129", 2d, $"FlightSampler.ModFacts: returns {CoreLog.Shown(returnedAt129)} (line 129)"); return returnedAt129; }
        }
        catch (NullReferenceException e)
        {
            { var returnedAt133 = $" | (the mod's facts failed: {e.Message})"; if (CoreLog.Due("FlightSampler.ModFacts:133", 2d)) ModLog.Every("FlightSampler.ModFacts:133", 2d, $"FlightSampler.ModFacts: returns {CoreLog.Shown(returnedAt133)} (line 133)"); return returnedAt133; }
        }
        catch (Exception e)          // last resort: foreign code
        {
            { var returnedAt137 = $" | (the mod's facts failed: {e.GetType().Name}: {e.Message})"; if (CoreLog.Due("FlightSampler.ModFacts:137", 2d)) ModLog.Every("FlightSampler.ModFacts:137", 2d, $"FlightSampler.ModFacts: returns {CoreLog.Shown(returnedAt137)} (line 137)"); return returnedAt137; }
        }
    }
}
