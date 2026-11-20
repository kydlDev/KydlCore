namespace KydlCore.Core;

/// <summary>What a world object is, for the TestLab cleanup that runs at the start of every test (D-142). TestWorldCleanup decides it from the game object's components.</summary>
public enum CleanupKind
{
    /// Anything the cleanup never touches (the player, terrain, standing vegetation, the lab's own objects).
    Other,
    /// A dropped item on the ground.
    Drop,
    /// A creature that is not the player.
    Creature,
    /// A felled tree trunk or a log.
    Log,
    /// A tree stump.
    Stump,
    /// A built piece.
    Piece
}
