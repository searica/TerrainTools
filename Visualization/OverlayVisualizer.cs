using HarmonyLib;
using UnityEngine;

namespace TerrainTools.Visualization;

// Helper classes for OverlayVisualizerImpls, intended to abstract away necessary low level complexity.
public abstract class OverlayVisualizer : MonoBehaviour
{
    protected Overlay primary;
    protected Overlay secondary;
    protected Overlay tertiary;
    protected HoverInfo hoverInfo;

    internal static readonly Vector3 VerticalOffset = new(0, 0.075f, 0);

    private void Update()
    {
        if (primary == null)
        {
            Transform primaryTransform = transform.Find("_GhostOnly");
            Transform secondaryTransform = Instantiate(primaryTransform, transform);
            Transform tetriaryTransform = Instantiate(secondaryTransform, transform);
            primary = new Overlay(primaryTransform);
            secondary = new Overlay(secondaryTransform);
            tertiary = new Overlay(tetriaryTransform);
            hoverInfo = new HoverInfo(secondaryTransform);
            tertiary.StartColor = Color.white;

            primary.Enabled = false;
            secondary.Enabled = false;
            tertiary.Enabled = false;
            Initialize();
        }

        OnRefresh();
    }

    protected abstract void Initialize();

    protected abstract void OnRefresh();

    protected void SpeedUp(Overlay overlay)
    {
        var animationCurve = new AnimationCurve();
        animationCurve.AddKey(0.0f, 0.0f);
        animationCurve.AddKey(0.5f, 1.0f);
        var minMaxCurve = new ParticleSystem.MinMaxCurve(1.0f, animationCurve);

        overlay.StartLifetime = 2.0f;
        overlay.SizeOverLifetime = minMaxCurve;
    }

    protected void Freeze(Overlay overlay)
    {
        overlay.StartSpeed = 0;
        overlay.SizeOverLifetimeEnabled = false;
    }

    protected void VisualizeTerraformingBounds(Overlay overlay)
    {
        overlay.StartSize = 3.0f;
        overlay.psr.material.mainTexture = IconCache.Box;
        overlay.LocalPosition = VerticalOffset;
    }

    protected void VisualizeIconInsideTerraformingBounds(Overlay overlay, Texture iconTexture)
    {
        overlay.StartSize = 2.5f;
        overlay.psr.material.mainTexture = iconTexture;
        overlay.LocalPosition = VerticalOffset;
    }

    protected void VisualizeRecoloringBounds(Overlay overlay)
    {
        overlay.StartSize = 3.0f;
        overlay.psr.material.mainTexture = IconCache.Box;
        overlay.LocalPosition = VerticalOffset;
    }

    protected void SnapToPaintGrid(Overlay overlay)
    {
        Heightmap heightmap = Heightmap.FindHeightmap(transform.position);
        if (!heightmap)
        {
            overlay.LocalPosition = VerticalOffset;
            return;
        }

        heightmap.WorldToVertexMask(transform.position, out int xPos, out int yPos);
        Vector3 first = heightmap.VertexMaskToWorld(xPos, yPos);
        Vector3 last = heightmap.VertexMaskToWorld(xPos + 2, yPos + 2);
        overlay.Position = new Vector3(
            (first.x + last.x) * 0.5f,
            transform.position.y + VerticalOffset.y,
            (first.z + last.z) * 0.5f
        );
    }

    protected void VisualizeIconInsideRecoloringBounds(Overlay overlay, Texture iconTexture)
    {
        overlay.StartSize = 2.5f;
        overlay.psr.material.mainTexture = iconTexture;
        overlay.Position = transform.position + VerticalOffset;
    }
}

public abstract class HoverInfoEnabled : OverlayVisualizer
{
    protected override void Initialize()
    {
        hoverInfo.Color = secondary.StartColor;
    }

    protected override void OnRefresh()
    {
        hoverInfo.Enabled = TerrainTools.Instance.IsHoverInforEnabled;
        if (hoverInfo.Enabled)
        {
            hoverInfo.RotateToPlayer();
            Vector3 pos = secondary.Position - VerticalOffset;
            hoverInfo.Text = $"x: {pos.x:0}, y: {pos.y:0.000}, z: {pos.z:0}";
        }
    }
}

/// <summary>
///     Blocks check for invalid placement height if custom overlay
/// </summary>
[HarmonyPatch(typeof(Piece), "SetInvalidPlacementHeightlight")]
public static class OverlayVisualizationRedshiftHeigthlightBlocker
{
    private static bool Prefix(Piece __instance)
    {
        return !__instance.GetComponentInChildren<OverlayVisualizer>();
    }
}
