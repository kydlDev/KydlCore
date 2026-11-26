using UnityEngine;

namespace KydlCore.Common;

/// <summary>How <see cref="KyIcons.Render(KyIconRequest)"/> draws a prefab (KydlCore 1.2.0, replaces Jotunn's RenderManager.RenderRequest, D-104).</summary>
public sealed class KyIconRequest
{
    /// <summary>The prefab to draw (only its meshes are drawn).</summary>
    public GameObject Target;
    /// <summary>Picture width in pixels.</summary>
    public int Width = 128;
    /// <summary>Picture height in pixels.</summary>
    public int Height = 128;
    /// <summary>Rotation of the prefab in front of the camera (<see cref="KyIcons.Isometric"/> = Jotunn's isometric view).</summary>
    public Quaternion Rotation = Quaternion.identity;
    /// <summary>Camera field of view in degrees (small = almost no perspective).</summary>
    public float FieldOfView = 0.5f;
    /// <summary>Camera distance factor (above 1 = more empty border).</summary>
    public float DistanceMultiplier = 1f;

    /// <summary>A request for <paramref name="target"/> with the defaults.</summary>
    public KyIconRequest(GameObject target) { Target = target; }
}
