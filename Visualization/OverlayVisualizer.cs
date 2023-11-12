using HarmonyLib;
using Jotunn.Utils;
using UnityEngine;

namespace TerrainTools.Visualization
{
    // Helper classes for OverlayVisualizerImpls, intented to abstract away unecessary low level complexity.
    public abstract class OverlayVisualizer : MonoBehaviour
    {
        protected Overlay primary;
        protected Overlay secondary;
        protected Overlay tertiary;
        protected HoverInfo hoverInfo;

        private void Update()
        {
            if (primary == null)
            {
                var primaryTransform = transform.Find("_GhostOnly");
                var secondaryTransform = Instantiate(primaryTransform, transform);
                var tetriaryTransform = Instantiate(secondaryTransform, transform);
                primary = new Overlay(primaryTransform);
                secondary = new Overlay(secondaryTransform);
                tertiary = new Overlay(tetriaryTransform);
                hoverInfo = new HoverInfo(secondaryTransform);
                tertiary.StartColor = new Color(255, 255, 255);

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
            overlay.LocalPosition = new Vector3(0, 0.05f, 0);
        }

        protected void VisualizeRecoloringBounds(Overlay overlay)
        {
            overlay.StartSize = 4.0f;
            overlay.psr.material.mainTexture = IconCache.Box;
            overlay.LocalPosition = new Vector3(0.5f, 0.05f, 0.5f);
        }

        protected void VisualizeIconInsideTerraformingBounds(Overlay overlay, Texture iconTexture)
        {
            overlay.StartSize = 2.5f;
            overlay.psr.material.mainTexture = iconTexture;
            overlay.LocalPosition = new Vector3(0, 0.05f, 0);
        }

        protected void VisualizeIconInsideRecoloringBounds(Overlay overlay, Texture iconTexture)
        {
            overlay.StartSize = 3.0f;
            overlay.psr.material.mainTexture = iconTexture;
            overlay.Position = transform.position + new Vector3(0.5f, 0.05f, 0.5f);
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
            if (hoverInfo.Enabled)
            {
                hoverInfo.RotateToPlayer();
                hoverInfo.Text = $"x: {secondary.Position.x:0}, y: {secondary.Position.z:0}, h: {secondary.Position.y - 0.05f:0.00000}";
            }
        }
    }

    public abstract class SecondaryEnabledPrimaryDisabled : HoverInfoEnabled
    {
        protected override void OnRefresh()
        {
            base.OnRefresh();
            primary.Enabled = false;
            secondary.Enabled = true;
        }
    }

    public abstract class SecondaryEnabledPrimaryEnabled : HoverInfoEnabled
    {
        protected override void OnRefresh()
        {
            base.OnRefresh();
            primary.Enabled = true;
            secondary.Enabled = true;
        }
    }

    public abstract class SecondaryAndPrimaryEnabledAlways : OverlayVisualizer
    {
        protected override void OnRefresh()
        {
            primary.Enabled = true;
            secondary.Enabled = true;
        }
    }

    [HarmonyPatch(typeof(Piece), "SetInvalidPlacementHeightlight")]
    public static class OverlayVisualizationRedshiftHeighlightBlocker
    {
        private static bool Prefix(bool enabled, Piece __instance)
        {
            return __instance.GetComponentInChildren<OverlayVisualizer>() == null;
        }
    }
}