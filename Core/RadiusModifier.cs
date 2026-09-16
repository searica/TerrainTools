using HarmonyLib;
using Logging;
using TerrainTools.Extensions;
using TerrainTools.Visualization;
using UnityEngine;

namespace TerrainTools.Core;

[HarmonyPatch]
internal static class RadiusModifier
{
    private static bool RadiusToolIsInUse = false;
    private static float lastOriginalRadius;
    private static float lastModdedRadius;
    private static float lastTotalDelta;

    private static float lastRaiseRadius;
    private static float lastSmoothRadius;
    private static float lastPaintRadius;
    private static float lastLevelRadius;

    private static Vector3 lastGhostScale = Vector3.zero;
    private const float MinRadius = 0.5f;

    private const float Tolerance = 0.01f;


    [HarmonyPrefix]
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    private static void UpdatePrefix(Player __instance)
    {
        if (!__instance || __instance != Player.m_localPlayer)
        {
            return;
        }

        if (!__instance.InPlaceMode() || Hud.IsPieceSelectionVisible())
        {
            if (RadiusToolIsInUse)
            {
                RadiusToolIsInUse = false;
                lastOriginalRadius = 0;
                lastModdedRadius = 0;
                lastTotalDelta = 0;
                lastGhostScale = Vector3.zero;
            }

            return;
        }

        if (!IsValidSelectedPiece(__instance, out TerrainOp terrainOp))
        {
            return;
        }

        if (ShouldModifyRadius())
        {
            SetRadius(terrainOp, Input.mouseScrollDelta.y * TerrainTools.Instance.RadiusScrollScale);
        }

        // this is constantly refreshing if FastTools is in use but turns out that bug
        // occurs when using FastTools even without TerrainTools
        RefreshGhostScale(__instance);
    }

    /// <summary>
    ///     Checks if selected piece is a valid target for modifying radius.
    /// </summary>
    /// <param name="player"></param>
    /// <param name="terrainOp"></param>
    /// <returns></returns>
    internal static bool IsValidSelectedPiece(Player player, out TerrainOp terrainOp)
    {
        Piece piece = player.GetSelectedPiece();
        if (!piece || !piece.gameObject || piece.gameObject.GetComponent<OverlayVisualizer>())
        {
            terrainOp = null;
            return false;
        }

        terrainOp = piece.gameObject.GetComponent<TerrainOp>();
        if (!terrainOp)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Checks if radius modification is enabled and being adjusted.
    /// </summary>
    /// <returns></returns>
    internal static bool ShouldModifyRadius()
    {
        return TerrainTools.Instance.IsEnableRadiusModifier && Input.GetKey(TerrainTools.Instance.RadiusKey) && Input.mouseScrollDelta.y != 0;
    }

    /// <summary>
    ///     Apply changes to radius just before performing the actual operation.
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="modifier"></param>
    [HarmonyPrefix]
    [HarmonyPriority(Priority.VeryLow)]
    [HarmonyPatch(typeof(TerrainComp), nameof(TerrainComp.InternalDoOperation))]
    private static void InternalDoOperationPrefix(TerrainComp __instance, TerrainOp.Settings modifier)
    {
        if (!__instance || modifier == null || modifier.IsPrecisionModifier())
        {
            return;
        }

        if (modifier.m_level)
        {
            lastLevelRadius = modifier.m_levelRadius;
            modifier.m_levelRadius = ModifyRadius(modifier.m_levelRadius, lastTotalDelta);
            Log.LogInfo($"Applying level radius {modifier.m_levelRadius}", Log.InfoLevel.Medium);
        }

        if (modifier.m_raise)
        {
            lastRaiseRadius = modifier.m_raiseRadius;
            modifier.m_raiseRadius = ModifyRadius(modifier.m_raiseRadius, lastTotalDelta);
            Log.LogInfo($"Applying raise radius {modifier.m_raiseRadius}", Log.InfoLevel.Medium);
        }

        if (modifier.m_smooth)
        {
            lastSmoothRadius = modifier.m_smoothRadius;
            modifier.m_smoothRadius = ModifyRadius(modifier.m_smoothRadius, lastTotalDelta);
            Log.LogInfo($"Applying smooth radius {modifier.m_smoothRadius}", Log.InfoLevel.Medium);
        }

        if (modifier.m_paintCleared)
        {
            lastPaintRadius = modifier.m_paintRadius;
            modifier.m_paintRadius = ModifyRadius(modifier.m_paintRadius, lastTotalDelta);
            Log.LogInfo($"Applying paint radius {modifier.m_paintRadius}", Log.InfoLevel.Medium);
        }
    }

    /// <summary>
    ///     Revert changes to radius just after performing operation to avoid cumulatively stacking delta.
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="modifier"></param>
    [HarmonyPostfix]
    [HarmonyPriority(Priority.VeryHigh)]
    [HarmonyPatch(typeof(TerrainComp), nameof(TerrainComp.InternalDoOperation))]
    private static void InternalDoOperationPostfix(TerrainComp __instance, TerrainOp.Settings modifier)
    {
        if (!__instance || modifier == null || modifier.IsPrecisionModifier())
        {
            return;
        }

        if (modifier.m_level)
        {
            modifier.m_levelRadius = lastLevelRadius;
            Log.LogInfo($"Resoted level radius {modifier.m_levelRadius}", Log.InfoLevel.Medium);
        }

        if (modifier.m_raise)
        {
            modifier.m_raiseRadius = lastRaiseRadius;
            Log.LogInfo($"Restored raise radius {modifier.m_raiseRadius}", Log.InfoLevel.Medium);
        }

        if (modifier.m_smooth)
        {
            modifier.m_smoothRadius = lastSmoothRadius;
            Log.LogInfo($"Restored smooth radius {modifier.m_smoothRadius}", Log.InfoLevel.Medium);
        }

        if (modifier.m_paintCleared)
        {
            modifier.m_paintRadius = lastPaintRadius;
            Log.LogInfo($"Restored paint radius {modifier.m_paintRadius}", Log.InfoLevel.Medium);
        }
    }

    private static float ModifyRadius(float radius, float delta)
    {
        return Mathf.Clamp(radius + delta, MinRadius, TerrainTools.Instance.MaxRadius);
    }

    private static void SetRadius(TerrainOp terrainOp, float delta)
    {
        Log.LogInfo($"Adjusting radius by {delta}", Log.InfoLevel.High);

        if (!RadiusToolIsInUse && terrainOp)
        {
            if (TryGetMaximumRadius(terrainOp, out float radius))
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
        Log.LogInfo($"Total delta {lastTotalDelta}", Log.InfoLevel.High);

        lastGhostScale = new Vector3(
            lastModdedRadius / lastOriginalRadius,
            lastModdedRadius / lastOriginalRadius,
            lastModdedRadius / lastOriginalRadius
        );
    }

    private static void RefreshGhostScale(Player player)
    {
        if (!RadiusToolIsInUse || !player.m_placementGhost)
        {
            return;
        }

        Transform ghost = player.m_placementGhost.transform.Find("_GhostOnly");
        if (!ghost)
        {
            return;
        }

        // handle pieces like path_v2 that have the particle effect nested in a child of _GhostOnly
        ParticleSystem particleEffect = ghost.GetComponentInChildren<ParticleSystem>();
        if (!particleEffect || lastGhostScale == Vector3.zero)
        {
            return;
        }

        float diff = Vector3.Distance(particleEffect.transform.localScale, lastGhostScale);
        if (diff > Tolerance)
        {
            Log.LogInfo($"Adjusting ghost scale to {lastGhostScale}x", Log.InfoLevel.High);
            particleEffect.transform.localScale = lastGhostScale;
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
