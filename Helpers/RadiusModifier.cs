using HarmonyLib;
using TerrainTools.Extensions;
using TerrainTools.Visualization;
using UnityEngine;

namespace TerrainTools.Helpers
{
    [HarmonyPatch]
    internal class RadiusModifier
    {
        private static bool RadiusToolIsInUse = false;
        private static float lastOriginalRadius;
        private static float lastModdedRadius;
        private static float lastTotalDelta;
        private static Vector3 lastGhostScale = Vector3.zero;
        private const float MinRadius = 0.5f;

        private const float Tolerance = 0.01f;

        [HarmonyPatch(typeof(Player))]
        internal class PlayerPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(Player.Update))]
            private static void UpdatePrefix(Player __instance)
            {
                if (__instance == null || !__instance.InPlaceMode() || Hud.IsPieceSelectionVisible())
                {
                    if (RadiusToolIsInUse)
                    {
                        RadiusToolIsInUse = false;
                        lastOriginalRadius = 0;
                        lastModdedRadius = 0;
                        lastTotalDelta = 0;
                        lastGhostScale = Vector3.zero;
                        SetRadius(__instance, 0);
                    }
                    return;
                }

                if (ShouldModifyRadius())
                {
                    SetRadius(__instance, Input.mouseScrollDelta.y * TerrainTools.RadiusScrollScale);
                }
                RefreshGhostScale(__instance); // this is constantly refreshing if FastTools is in use
            }
        }

        internal static bool ShouldModifyRadius()
        {
            return TerrainTools.IsEnableRadiusModifier && Input.GetKey(TerrainTools.RadiusKey) && Input.mouseScrollDelta.y != 0;
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
            var piece = player?.GetSelectedPiece();
            if (piece is null || piece.gameObject is null || piece.gameObject.HasComponent<OverlayVisualizer>())
            {
                return;
            }

            var terrainOp = piece.gameObject.GetComponent<TerrainOp>();
            if (terrainOp == null) { return; }

            Log.LogInfo($"Adjusting radius by {delta}", LogLevel.High);

            if (!RadiusToolIsInUse)
            {
                if (TryGetMaximumRadius(terrainOp, out var radius))
                {
                    RadiusToolIsInUse = true;
                    lastOriginalRadius = radius;
                    lastModdedRadius = ModifyRadius(radius, delta);
                    lastTotalDelta += delta;
                }
            }
            else
            {
                lastModdedRadius = ModifyRadius(lastModdedRadius, delta);
                lastTotalDelta += delta;
            }
            Log.LogInfo($"total delta {lastTotalDelta}", LogLevel.High);

            lastGhostScale = new Vector3(
                lastModdedRadius / lastOriginalRadius,
                lastModdedRadius / lastOriginalRadius,
                lastModdedRadius / lastOriginalRadius
            );
        }

        private static void RefreshGhostScale(Player player)
        {
            if (RadiusToolIsInUse)
            {
                var ghost = player?.m_placementGhost?.transform.Find("_GhostOnly")?.gameObject;
                if (ghost == null) { return; }

                // handle pieces like path_v2 that have the particle effect nested in a child of _GhostOnly
                var particleEffect = ghost.GetComponentInChildren<ParticleSystem>()?.gameObject;
                if (particleEffect != null && lastGhostScale != Vector3.zero)
                {
                    if (Vector3.Distance(particleEffect.transform.localScale, lastGhostScale) > Tolerance)
                    {
                        Log.LogInfo($"Adjusting ghost scale to {lastModdedRadius / lastOriginalRadius}x", LogLevel.High);
                        particleEffect.transform.localScale = lastGhostScale;
                    }
                }
            }
        }

        /// <summary>
        ///     Find the maximum radius value within the TerrainOp Settings
        /// </summary>
        /// <param name="terrainOp"></param>
        /// <returns></returns>
        private static bool TryGetMaximumRadius(TerrainOp terrainOp, out float maxRadius)
        {
            maxRadius = 0f;
            if (terrainOp.m_settings.m_level && maxRadius < terrainOp.m_settings.m_levelRadius)
            {
                maxRadius = terrainOp.m_settings.m_levelRadius;
            }
            if (terrainOp.m_settings.m_raise && maxRadius < terrainOp.m_settings.m_raiseRadius)
            {
                maxRadius = terrainOp.m_settings.m_raiseRadius;
            }
            if (terrainOp.m_settings.m_smooth && maxRadius < terrainOp.m_settings.m_smoothRadius)
            {
                maxRadius = terrainOp.m_settings.m_smoothRadius;
            }
            if (terrainOp.m_settings.m_paintCleared && maxRadius < terrainOp.m_settings.m_paintRadius)
            {
                maxRadius = terrainOp.m_settings.m_paintRadius;
            }
            return maxRadius != 0f;
        }
    }
}