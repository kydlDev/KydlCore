namespace KydlCore.Common;

/// <summary>How strictly a KydlDev mod checks the other side of a connection (KydlCore 1.2.0, replaces Jotunn's NetworkCompatibility, D-104).</summary>
public enum KyNetworkLevel
{
    /// <summary>The mod works on one side only: a player joins a server without it, but when both sides have it the versions must be equal
    /// (the old Jotunn level VersionCheckOnly + Patch).</summary>
    ClientOnly = 0,

    /// <summary>Every player on a server needs the mod in exactly the server's version (the old EveryoneMustHaveMod + Patch).</summary>
    EveryoneMustHave = 1,
}
