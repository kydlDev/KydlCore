using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using KydlCore.Core;
using UnityEngine;

namespace KydlCore.Common;

/// <summary>
/// SHARED (KydlCore 1.2.0, replaces Jotunn's RenderManager.Render, D-104). Draws a prefab's meshes into a sprite with a transparent background: a
/// copy that holds only the meshes (no scripts, so nothing wakes up) is put on layer 31 in front of a temporary camera and light, rendered once and
/// destroyed. Main thread only. Not ported: particle systems and Jotunn's icon file cache.
/// </summary>
public static class KyIcons
{
    const int Layer = 31;

    /// <summary>Jotunn's isometric icon rotation (23, 51, 25.8 degrees).</summary>
    public static readonly Quaternion Isometric = Quaternion.Euler(23f, 51f, 25.8f);

    /// <summary>The prefab with the default settings. Expected outcome: the sprite, or null when the prefab has no mesh.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Sprite Render(GameObject target) { return Draw(ModContexts.For(Assembly.GetCallingAssembly()), new KyIconRequest(target)); }

    /// <summary>The prefab as the request says. Expected outcome: the sprite, or null when the prefab has no mesh.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Sprite Render(KyIconRequest request) { return Draw(ModContexts.For(Assembly.GetCallingAssembly()), request); }

    static Sprite Draw(ModContext ctx, KyIconRequest request)
    {
        if (request == null || request.Target == null) { ModLog.Warn(ctx, "KyIcons.Render: no target - no icon"); return null; }
        GameObject copy = MeshCopy(request.Target, out Bounds bounds, out int meshes);
        if (meshes == 0) { Object.Destroy(copy); ModLog.Warn(ctx, $"KyIcons.Render: '{request.Target.name}' has no mesh to draw - no icon"); return null; }
        copy.transform.rotation = request.Rotation;
        bounds = WorldBounds(copy);
        copy.transform.position -= bounds.center;
        var cam = new GameObject("KydlCore_IconCamera").AddComponent<Camera>();
        var light = new GameObject("KydlCore_IconLight").AddComponent<Light>();
        Sprite sprite;
        try
        {
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.cullingMask = 1 << Layer;
            cam.farClipPlane = 100000f;
            cam.fieldOfView = request.FieldOfView;
            cam.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            float size = Mathf.Max(bounds.size.x, bounds.size.y) + 0.1f;
            cam.transform.position = new Vector3(0f, 0f, size / Mathf.Tan(request.FieldOfView * Mathf.Deg2Rad) * request.DistanceMultiplier);
            light.type = LightType.Directional;
            light.cullingMask = 1 << Layer;
            light.transform.rotation = Quaternion.Euler(5f, 180f, 5f);
            var target = RenderTexture.GetTemporary(request.Width, request.Height, 24, RenderTextureFormat.ARGB32);
            RenderTexture before = RenderTexture.active;
            cam.targetTexture = target;
            RenderTexture.active = target;
            cam.Render();
            var tex = new Texture2D(request.Width, request.Height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, request.Width, request.Height), 0, 0);
            tex.Apply();
            RenderTexture.active = before;
            cam.targetTexture = null;
            RenderTexture.ReleaseTemporary(target);
            sprite = Sprite.Create(tex, new Rect(0, 0, request.Width, request.Height), Vector2.one / 2f);
            sprite.name = $"{request.Target.name}_icon";
        }
        finally
        {
            Object.Destroy(cam.gameObject);
            Object.Destroy(light.gameObject);
            Object.Destroy(copy);
        }
        ModLog.Verbose(ctx, $"KyIcons: '{request.Target.name}' drawn {request.Width}x{request.Height} - {meshes} mesh(es), size {bounds.size}, fov {request.FieldOfView}");
        return sprite;
    }

    /// A copy of the hierarchy that keeps only transforms, mesh filters and renderers (skinned meshes are baked), on the icon layer.
    static GameObject MeshCopy(GameObject source, out Bounds bounds, out int meshes)
    {
        int count = 0;
        GameObject root = CopyNode(source.transform, null, ref count);
        root.transform.position = new Vector3(0f, 0f, 0f);
        bounds = WorldBounds(root);
        meshes = count;
        if (CoreLog.On) ModLog.Verbose($"KyIcons.MeshCopy: '{source.name}' -> {count} mesh(es)");
        return root;
    }

    static GameObject CopyNode(Transform from, Transform parent, ref int meshes)
    {
        var go = new GameObject(from.name) { layer = Layer };
        go.transform.SetParent(parent, false);
        go.transform.localPosition = parent == null ? Vector3.zero : from.localPosition;
        go.transform.localRotation = parent == null ? Quaternion.identity : from.localRotation;
        go.transform.localScale = from.localScale;
        go.SetActive(from.gameObject.activeSelf || parent == null);
        var filter = from.GetComponent<MeshFilter>();
        var mr = from.GetComponent<MeshRenderer>();
        var skinned = from.GetComponent<SkinnedMeshRenderer>();
        Mesh mesh = filter != null && mr != null && mr.enabled ? filter.sharedMesh : null;
        Material[] mats = mr != null ? mr.sharedMaterials : null;
        if (mesh == null && skinned != null && skinned.enabled && skinned.sharedMesh != null) { mesh = new Mesh(); skinned.BakeMesh(mesh); mats = skinned.sharedMaterials; }
        if (mesh != null)
        {
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = mats;
            meshes++;
        }
        foreach (Transform child in from) CopyNode(child, go.transform, ref meshes);
        if (CoreLog.Due("KyIcons.CopyNode", 5d)) ModLog.Every("KyIcons.CopyNode", 5d, $"KyIcons.CopyNode: '{from.name}' mesh={mesh != null}, meshes so far {meshes}");
        return go;
    }

    static Bounds WorldBounds(GameObject root)
    {
        var list = new List<Renderer>(root.GetComponentsInChildren<Renderer>(false));
        Bounds b = list.Count > 0 ? list[0].bounds : new Bounds(root.transform.position, Vector3.zero);
        foreach (Renderer r in list) b.Encapsulate(r.bounds);
        if (CoreLog.On) ModLog.Verbose($"KyIcons.WorldBounds: {list.Count} renderer(s) - centre {b.center}, size {b.size}");
        return b;
    }
}
