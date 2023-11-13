using HarmonyLib;
using TerrainTools.Extensions;
using TerrainTools.Visualization;
using UnityEngine;

namespace TerrainTools.Helpers
{
    [HarmonyPatch]
    internal class RadiusModifier
    {
        internal static float lastOriginalRadius;
        internal static float lastModdedRadius;
        internal static float lastTotalDelta;
        private const float MinRadius = 0.5f;

        [HarmonyPatch(typeof(Player))]
        internal class PlayerPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(Player.Update))]
            private static void UpdatePrefix(Player __instance)
            {
                if (
                    __instance == null
                    || !__instance.InPlaceMode()
                    || Hud.IsPieceSelectionVisible()
                )
                {
                    if (lastOriginalRadius != 0)
                    {
                        lastOriginalRadius = 0;
                        lastModdedRadius = 0;
                        lastTotalDelta = 0;
                        SetRadius(__instance, 0);
                    }
                    return;
                }

                if (ShouldModifyRadius())
                {
                    SetRadius(__instance, Input.mouseScrollDelta.y * TerrainTools.ScrollWheelScale);
                }
            }
        }

        internal static bool ShouldModifyRadius()
        {
            return TerrainTools.IsEnableRadiusModifier && Input.GetKey(TerrainTools.ScrollModKey) && Input.mouseScrollDelta.y != 0;
        }

        [HarmonyPatch(typeof(TerrainOp))]
        internal class TerrainOpPatch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.VeryHigh)]
            [HarmonyPatch(nameof(TerrainOp.Awake))]
            private static void AwakePrefix(TerrainOp __instance)
            {
                if (__instance is null || __instance.gameObject is null || __instance.gameObject.HasComponent<OverlayVisualizer>())
                {
                    return;
                }

                if (__instance.m_settings.m_level)
                {
                    __instance.m_settings.m_levelRadius = ModifyRadius(__instance.m_settings.m_levelRadius, lastTotalDelta);
                    Log.LogInfo($"Applying level radius {__instance.m_settings.m_levelRadius}", LogLevel.Medium);
                }

                if (__instance.m_settings.m_raise)
                {
                    __instance.m_settings.m_raiseRadius = ModifyRadius(__instance.m_settings.m_raiseRadius, lastTotalDelta);
                    Log.LogInfo($"Applying raise radius {__instance.m_settings.m_raiseRadius}", LogLevel.Medium);
                }

                if (__instance.m_settings.m_smooth)
                {
                    __instance.m_settings.m_smoothRadius = ModifyRadius(__instance.m_settings.m_smoothRadius, lastTotalDelta);
                    Log.LogInfo($"Applying smooth radius {__instance.m_settings.m_smoothRadius}", LogLevel.Medium);
                }

                if (__instance.m_settings.m_paintCleared)
                {
                    __instance.m_settings.m_paintRadius = ModifyRadius(__instance.m_settings.m_paintRadius, lastTotalDelta);
                    Log.LogInfo($"Applying paint radius {__instance.m_settings.m_paintRadius}", LogLevel.Medium);
                }
            }
        }

        private static float ModifyRadius(float radius, float delta)
        {
            return Mathf.Clamp(radius + delta, MinRadius, TerrainTools.MaxRadius);
        }

        private static void SetRadius(Player player, float delta)
        {
            var piece = player.GetSelectedPiece();
            if (piece is null || piece.gameObject is null || piece.gameObject.HasComponent<OverlayVisualizer>())
            {
                return;
            }

            var terrainOp = piece.gameObject.GetComponent<TerrainOp>();
            if (terrainOp == null) { return; }

            Log.LogInfo($"Adjusting radius by {delta}", LogLevel.Medium);

            float originalRadius = 0;
            float moddedRadius = ModifyRadius(lastModdedRadius, delta);
            lastTotalDelta += delta;

            if (lastOriginalRadius == 0)
            {
                if (terrainOp.m_settings.m_level && originalRadius < terrainOp.m_settings.m_levelRadius)
                {
                    originalRadius = terrainOp.m_settings.m_levelRadius;
                    moddedRadius = ModifyRadius(terrainOp.m_settings.m_levelRadius, delta);
                }
                if (terrainOp.m_settings.m_raise && originalRadius < terrainOp.m_settings.m_raiseRadius)
                {
                    originalRadius = terrainOp.m_settings.m_raiseRadius;
                    moddedRadius = ModifyRadius(terrainOp.m_settings.m_raiseRadius, delta);
                }
                if (terrainOp.m_settings.m_smooth && originalRadius < terrainOp.m_settings.m_smoothRadius)
                {
                    originalRadius = terrainOp.m_settings.m_smoothRadius;
                    moddedRadius = ModifyRadius(terrainOp.m_settings.m_smoothRadius, delta);
                }
                if (terrainOp.m_settings.m_paintCleared && originalRadius < terrainOp.m_settings.m_paintRadius)
                {
                    originalRadius = terrainOp.m_settings.m_paintRadius;
                    moddedRadius = ModifyRadius(terrainOp.m_settings.m_paintRadius, delta);
                }
                lastOriginalRadius = originalRadius;
            }
            lastModdedRadius = moddedRadius;

            if (lastOriginalRadius > 0 && lastModdedRadius > 0)
            {
                Log.LogInfo($"total delta {lastTotalDelta}", LogLevel.Medium);

                var ghost = player.m_placementGhost?.transform.Find("_GhostOnly");
                if (ghost != null)
                {
                    Log.LogInfo($"Adjusting ghost scale to {lastModdedRadius / lastOriginalRadius}x", LogLevel.Medium);
                    ghost.localScale = new Vector3(
                        lastModdedRadius / lastOriginalRadius,
                        lastModdedRadius / lastOriginalRadius,
                        lastModdedRadius / lastOriginalRadius
                    );
                }
            }
        }
    }
}