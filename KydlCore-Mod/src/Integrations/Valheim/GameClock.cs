using KydlCore.Core;

namespace KydlCore.Integrations.Valheim;

/// <summary>Game-side clock for pure code: Unity's Time.time (seconds since the game started). Public since 1.2.0 (D-105: was a copy in 7 mods).</summary>
public sealed class GameClock : IClock
{
    /// <summary>Seconds since the game started.</summary>
    public float Now { get { return UnityEngine.Time.time; } }
}
