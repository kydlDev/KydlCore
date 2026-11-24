namespace KydlCore.Core;

/// <summary>Game time for pure code (Domain / Application / Execution must not read UnityEngine.Time). In game: an adapter over Time.time / ZNet time; in tests: <see cref="ManualClock"/>.</summary>
public interface IClock
{
    /// Seconds since the game started (Time.time in game).
    float Now { get; }
}
