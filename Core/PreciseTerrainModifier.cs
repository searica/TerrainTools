using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using TerrainTools.Extensions;
using TerrainTools.Visualization;

namespace TerrainTools.Core;

[HarmonyPatch(typeof(PreciseTerrainModifier))]
public static class PreciseTerrainModifier
{
    private const int SettingsPayloadMagic = 0x41544D53; // ATMS
    private const int SettingsPayloadVersion = 1;

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
    private static void ApplyOperationPrefix(TerrainComp __instance, TerrainOp modifier)
    {
        if (!modifier || !modifier.gameObject) { return; }

        // Valheim 1.0 resolves TerrainOp settings on the owner. Claim before
        // serializing the operation so the patched owner receives runtime values.
        if (__instance && __instance.m_nview && !__instance.m_nview.IsOwner())
        {
            __instance.m_nview.ClaimOwnership();
        }

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

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TerrainOp), nameof(TerrainOp.GetRadius))]
    private static void GetRadiusPostfix(TerrainOp __instance, ref float __result)
    {
        if (__instance && __instance.gameObject.GetComponent<RemoveModificationsOverlayVisualizer>())
        {
            __result = Mathf.Max(__result, __instance.m_settings.m_levelRadius + 1f);
        }
    }

    private static void RemoveLegacyTerrainModifiers(Vector3 position, float radius)
    {
        var modifiers = new List<TerrainModifier>();
        TerrainModifier.GetModifiers(position, radius + 1f, modifiers);
        foreach (TerrainModifier modifier in modifiers)
        {
            if (!modifier || !modifier.m_nview)
            {
                continue;
            }
            modifier.m_nview.ClaimOwnership();
            ZNetScene.instance.Destroy(modifier.gameObject);
        }
    }

    /// <summary>
    ///     Valheim 1.0 serializes only the TerrainOp prefab hash. Append the
    ///     runtime values changed by precision, radius, and hardness controls.
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TerrainOp.Settings), nameof(TerrainOp.Settings.Serialize))]
    private static void SerializeSettingsPostfix(TerrainOp.Settings __instance, ZPackage pkg)
    {
        if (__instance == null || pkg == null)
        {
            return;
        }

        pkg.Write(SettingsPayloadMagic);
        pkg.Write(SettingsPayloadVersion);
        pkg.Write(__instance.m_levelRadius);
        pkg.Write(__instance.m_raiseRadius);
        pkg.Write(__instance.m_raisePower);
        pkg.Write(__instance.m_raiseDelta);
        pkg.Write(__instance.m_smoothRadius);
        pkg.Write(__instance.m_smoothPower);
        pkg.Write(__instance.m_paintRadius);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TerrainOp.Settings), nameof(TerrainOp.Settings.Deserialize))]
    private static void DeserializeSettingsPostfix(ZPackage pkg, ref TerrainOp.Settings __result)
    {
        const int payloadSize = sizeof(int) * 2 + sizeof(float) * 7;
        if (__result == null || pkg == null || pkg.Size() - pkg.GetPos() < payloadSize)
        {
            return;
        }

        int payloadStart = pkg.GetPos();
        if (pkg.ReadInt() != SettingsPayloadMagic || pkg.ReadInt() != SettingsPayloadVersion)
        {
            pkg.SetPos(payloadStart);
            return;
        }

        TerrainOp.Settings settings = CopySettings(__result);
        settings.m_levelRadius = pkg.ReadSingle();
        settings.m_raiseRadius = pkg.ReadSingle();
        settings.m_raisePower = pkg.ReadSingle();
        settings.m_raiseDelta = pkg.ReadSingle();
        settings.m_smoothRadius = pkg.ReadSingle();
        settings.m_smoothPower = pkg.ReadSingle();
        settings.m_paintRadius = pkg.ReadSingle();
        __result = settings;
    }

    private static TerrainOp.Settings CopySettings(TerrainOp.Settings source)
    {
        return new TerrainOp.Settings
        {
            m_levelOffset = source.m_levelOffset,
            m_level = source.m_level,
            m_levelRadius = source.m_levelRadius,
            m_square = source.m_square,
            m_raise = source.m_raise,
            m_raiseRadius = source.m_raiseRadius,
            m_raisePower = source.m_raisePower,
            m_raiseDelta = source.m_raiseDelta,
            m_smooth = source.m_smooth,
            m_smoothRadius = source.m_smoothRadius,
            m_smoothPower = source.m_smoothPower,
            m_paintCleared = source.m_paintCleared,
            m_paintHeightCheck = source.m_paintHeightCheck,
            m_paintType = source.m_paintType,
            m_paintRadius = source.m_paintRadius,
            m_paintStrength = source.m_paintStrength,
            m_paintExp = source.m_paintExp,
            m_paintCurve = source.m_paintCurve,
            m_rotation = source.m_rotation,
            m_sides = source.m_sides,
            m_addMedianMax = source.m_addMedianMax,
            m_centerMultiplicationFactor = source.m_centerMultiplicationFactor,
            m_pointMultiplicationFactor = source.m_pointMultiplicationFactor,
            m_halfOffset = source.m_halfOffset
        };
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
            int radius = Mathf.Clamp(
                Mathf.RoundToInt(modifier.m_levelRadius),
                TerrainCompExtensions.FixedRadius,
                Mathf.CeilToInt(TerrainTools.Instance.MaxRadius)
            );
            RemoveLegacyTerrainModifiers(pos, radius);
            __instance.RemoveTerrainModifications(pos, radius);
            __instance.PreciseRecolorTerrain(pos, TerrainModifier.PaintType.Reset, radius);
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

        __instance.PreciseRecolorTerrain(worldPos, paintType);
        return false;
    }
}
