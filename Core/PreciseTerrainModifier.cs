using HarmonyLib;
using UnityEngine;
using TerrainTools.Extensions;
using TerrainTools.Visualization;

namespace TerrainTools.Helpers;

[HarmonyPatch(typeof(PreciseTerrainModifier))]
public static class PreciseTerrainModifier
{

    /// <summary>
    ///     Catches invalid radius from precise terrain modifications and modifies it
    ///     to match the fixed radius before executing the ClutterSytem.ResetGrass method.
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="center"></param>
    /// <param name="radius"></param>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ClutterSystem), nameof(ClutterSystem.ResetGrass))]
    private static void ResetGrassPrefix(ClutterSystem __instance, Vector3 center, ref float radius)
    {
        if (TerrainCompExtensions.IsPrecisionModifier(radius))
        {
            radius = TerrainCompExtensions.FixedRadius - 0.25f;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(TerrainComp), nameof(TerrainComp.ApplyOperation))]
    private static void ApplyOperationPrefix(TerrainOp modifier)
    {
        if (!modifier || !modifier.gameObject) { return; }

        // Set radius to -inf so I can check if custom overlay in later methods
        if (modifier.gameObject.GetComponentInChildren<OverlayVisualizer>())
        {
            if (modifier.m_settings.m_smooth)
            {
                modifier.m_settings.m_smoothRadius = float.NegativeInfinity;
            }
            if (modifier.m_settings.m_raise && modifier.m_settings.m_raiseDelta >= 0)
            {
                modifier.m_settings.m_raiseRadius = float.NegativeInfinity;
                modifier.m_settings.m_raiseDelta = GroundLevelSpinner.Value;
            }
            if (modifier.m_settings.m_paintCleared)
            {
                modifier.m_settings.m_paintRadius = float.NegativeInfinity;
            }
        }
    }

    /// <summary>
    ///     Claim ownership before sending RPC to do terrain operation to
    ///     ensure that custom terrain ops run on a PC with the mod.
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(TerrainComp), nameof(TerrainComp.RPC_ApplyOperation))]
    private static void RPC_ApplyOperationPrefix(TerrainComp __instance)
    {
        if (!__instance || !__instance.m_nview)
        {
            return;
        }

        if (!__instance.m_nview.IsOwner())
        {
            __instance.m_nview.ClaimOwnership();
        }
    }

    /// <summary>
    ///     Apply TerrainReset operation if valid
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="pos"></param>
    /// <param name="modifier"></param>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(TerrainComp), nameof(TerrainComp.InternalDoOperation))]
    private static void InternalDoOperationPrefix(
        TerrainComp __instance,
        Vector3 pos,
        TerrainOp.Settings modifier
    )
    {
        if (!modifier.m_level && !modifier.m_raise && !modifier.m_smooth && !modifier.m_paintCleared)
        {
            __instance.RemoveTerrainModifications(pos);
            __instance.PreciseRecolorTerrain(pos, TerrainModifier.PaintType.Reset);
        }
    }

    /// <summary>
    ///     Correct m_lastOpRadius to be FixedRadius instead of -Infinity
    ///     if a Precision Modifier was the last operation applied. This
    ///     avoids issues caused by saving an invalid radius.
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="pos"></param>
    /// <param name="modifier"></param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TerrainComp), nameof(TerrainComp.InternalDoOperation))]
    private static void InternalDoOperationPostfix(TerrainComp __instance)
    {
        if (__instance.IsPrecisionModifier())
        {
            __instance.m_lastOpRadius = __instance.GetFixedRadius();
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(TerrainComp), nameof(TerrainComp.SmoothTerrain))]
    private static bool SmoothTerrainPrefix(
        TerrainComp __instance,
        Vector3 worldPos,
        float radius
    )
    {
        if (!TerrainCompExtensions.IsPrecisionModifier(radius))
        {
            return true;
        }

        __instance.PreciseSmoothTerrain(worldPos);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(TerrainComp), nameof(TerrainComp.RaiseTerrain))]
    private static bool RaiseTerrainPrefix(TerrainComp __instance, Vector3 worldPos, float radius, float delta)
    {
        if (!TerrainCompExtensions.IsPrecisionModifier(radius))
        {
            return true;
        }

        __instance.PreciseRaiseTerrain(worldPos, delta);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(TerrainComp), nameof(TerrainComp.PaintCleared))]
    private static bool PaintClearedPrefix(
        TerrainComp __instance,
        Vector3 worldPos,
        float radius,
        TerrainModifier.PaintType paintType,
        bool heightCheck
    )
    {
        if (!TerrainCompExtensions.IsPrecisionModifier(radius))
        {
            return true;
        }

        __instance.PreciseRecolorTerrain(worldPos, paintType, heightCheck);
        return false;
    }
}
