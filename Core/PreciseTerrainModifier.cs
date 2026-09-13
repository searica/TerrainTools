using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using TerrainTools.Extensions;
using TerrainTools.Visualization;

namespace TerrainTools.Core;

[HarmonyPatch]
public static class PreciseTerrainModifier
{
    private const int SettingsPayloadMagic = 0x41544D53; // ATMS
    private const int SettingsPayloadVersion = 1;
    private const float MinRadius = 0.5f;
    private const float MinRaisePower = 0.05f;
    private const float MaxRaisePower = 1f;
    private const float MaxSmoothPower = 30f;

    internal sealed class RuntimeSettings : TerrainOp.Settings
    {
        internal bool IsReset;
        internal bool HasOverlay;
    }

    internal static RuntimeSettings EnsureRuntimeSettings(TerrainOp terrainOp, bool isReset = false, bool hasOverlay = false)
    {
        if (terrainOp.m_settings is RuntimeSettings settings)
        {
            settings.IsReset |= isReset;
            settings.HasOverlay |= hasOverlay;
            return settings;
        }

        settings = CopySettings(terrainOp.m_settings, isReset, hasOverlay);
        terrainOp.m_settings = settings;
        return settings;
    }

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
    private static bool ApplyOperationPrefix(TerrainComp __instance, TerrainOp modifier)
    {
        if (!modifier || !modifier.gameObject) { return true; }

        OverlayVisualizer overlay = modifier.gameObject.GetComponentInChildren<OverlayVisualizer>();
        RuntimeSettings settings = modifier.m_settings as RuntimeSettings;
        if (settings == null && (overlay || InitManager.IsCustomTool(modifier.gameObject)))
        {
            settings = EnsureRuntimeSettings(
                modifier,
                overlay is RemoveModificationsOverlayVisualizer,
                overlay != null
            );
        }
        if (settings == null) { return true; }

        if (!HasAreaAccess(modifier, modifier.transform.position, false))
        {
            Logging.Log.LogWarning("Blocked a terrain operation whose brush intersects a protected area");
            return false;
        }

        // Valheim 1.0 resolves TerrainOp settings on the owner. Claim before
        // serializing the operation so the patched owner receives runtime values.
        if (__instance && __instance.m_nview && __instance.m_nview.IsValid() && !__instance.m_nview.IsOwner())
        {
            __instance.m_nview.ClaimOwnership();
        }

        // Set radius to -inf so I can check if custom overlay in later methods
        if (settings.HasOverlay)
        {
            if (settings.m_smooth)
            {
                settings.m_smoothRadius = float.NegativeInfinity;
            }
            if (settings.m_raise && settings.m_raiseDelta >= 0)
            {
                settings.m_raiseRadius = float.NegativeInfinity;
                settings.m_raiseDelta = GroundLevelSpinner.Value;
            }
            if (settings.m_paintCleared)
            {
                settings.m_paintRadius = float.NegativeInfinity;
            }
        }
        return true;
    }

    internal static bool HasAreaAccess(TerrainOp modifier, Vector3 position, bool flash)
    {
        if (!Player.m_localPlayer || !modifier) { return true; }

        float radius = modifier.GetRadius();
        RuntimeSettings settings = modifier.m_settings as RuntimeSettings;
        bool hasOverlay = settings?.HasOverlay == true || modifier.gameObject.GetComponentInChildren<OverlayVisualizer>();
        if (hasOverlay && (!IsFinite(radius) || radius < TerrainCompExtensions.FixedRadius))
        {
            radius = TerrainCompExtensions.FixedRadius;
        }
        if (!IsFinite(radius) || radius < 0f) { return false; }

        if (modifier.m_settings.m_square || hasOverlay || settings?.IsReset == true)
        {
            radius *= 1.414214f;
        }
        return PrivateArea.CheckAccess(position, radius, flash, true);
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
            float modifierRadius = modifier ? modifier.GetRadius() : 0f;
            if (!modifier || !modifier.m_nview
                || !modifier.m_nview.IsValid()
                || !modifier.m_playerModifiction
                || modifier.GetComponentInParent<Piece>()
                || modifier.GetComponentInParent<WearNTear>()
                || Mathf.Abs(modifier.transform.position.x - position.x) + modifierRadius > radius
                || Mathf.Abs(modifier.transform.position.z - position.z) + modifierRadius > radius
                || (Player.m_localPlayer && !PrivateArea.CheckAccess(modifier.transform.position, modifierRadius, false, true)))
            {
                continue;
            }
            modifier.m_nview.ClaimOwnership();
            if (ZNetScene.instance)
            {
                ZNetScene.instance.Destroy(modifier.gameObject);
            }
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
        if (__instance is not RuntimeSettings settings || pkg == null)
        {
            return;
        }

        pkg.Write(SettingsPayloadMagic);
        pkg.Write(SettingsPayloadVersion);
        pkg.Write(settings.m_levelRadius);
        pkg.Write(settings.m_raiseRadius);
        pkg.Write(settings.m_raisePower);
        pkg.Write(settings.m_raiseDelta);
        pkg.Write(settings.m_smoothRadius);
        pkg.Write(settings.m_smoothPower);
        pkg.Write(settings.m_paintRadius);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TerrainOp.Settings), nameof(TerrainOp.Settings.Deserialize))]
    private static void DeserializeSettingsPostfix(ZPackage pkg, ref TerrainOp.Settings __result)
    {
        const int payloadSize = sizeof(int) * 2 + sizeof(float) * 7;
        if (__result == null || pkg == null || pkg.Size() - pkg.GetPos() < sizeof(int))
        {
            return;
        }

        int payloadStart = pkg.GetPos();
        if (pkg.ReadInt() != SettingsPayloadMagic)
        {
            pkg.SetPos(payloadStart);
            return;
        }

        if (pkg.Size() - payloadStart < payloadSize || pkg.ReadInt() != SettingsPayloadVersion)
        {
            Logging.Log.LogWarning("Rejected incomplete or unsupported terrain-operation settings");
            __result = null;
            return;
        }

        RuntimeSettings source = __result as RuntimeSettings;
        RuntimeSettings settings = CopySettings(__result, source?.IsReset ?? false, source?.HasOverlay ?? false);
        settings.m_levelRadius = pkg.ReadSingle();
        settings.m_raiseRadius = pkg.ReadSingle();
        settings.m_raisePower = pkg.ReadSingle();
        settings.m_raiseDelta = pkg.ReadSingle();
        settings.m_smoothRadius = pkg.ReadSingle();
        settings.m_smoothPower = pkg.ReadSingle();
        settings.m_paintRadius = pkg.ReadSingle();
        if (!IsValid(settings))
        {
            Logging.Log.LogWarning("Rejected invalid network terrain-operation settings");
            __result = null;
            return;
        }
        __result = settings;
    }

    private static RuntimeSettings CopySettings(TerrainOp.Settings source, bool isReset, bool hasOverlay)
    {
        return new RuntimeSettings
        {
            IsReset = isReset,
            HasOverlay = hasOverlay,
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

    private static bool IsValid(RuntimeSettings settings)
    {
        if (!IsRadius(settings.m_levelRadius, settings.IsReset ? TerrainCompExtensions.FixedRadius : MinRadius)) { return false; }
        if (settings.m_raise && !IsPrecisionRadius(settings.m_raiseRadius, settings.HasOverlay)) { return false; }
        if (settings.m_smooth && !IsPrecisionRadius(settings.m_smoothRadius, settings.HasOverlay)) { return false; }
        if (settings.m_paintCleared && !IsPrecisionRadius(settings.m_paintRadius, settings.HasOverlay)) { return false; }
        if (settings.m_raise && (!IsFinite(settings.m_raisePower) || settings.m_raisePower < MinRaisePower || settings.m_raisePower > MaxRaisePower)) { return false; }
        if (settings.m_raise && (!IsFinite(settings.m_raiseDelta) || settings.m_raiseDelta < -1f || settings.m_raiseDelta > 1f)) { return false; }
        if (settings.m_smooth && (!IsFinite(settings.m_smoothPower) || settings.m_smoothPower < 1f || settings.m_smoothPower > MaxSmoothPower)) { return false; }
        return true;
    }

    private static bool IsPrecisionRadius(float value, bool allowPrecision)
    {
        return (allowPrecision && TerrainCompExtensions.IsPrecisionModifier(value)) || IsRadius(value, MinRadius);
    }

    private static bool IsRadius(float value, float minimum)
    {
        return IsFinite(value) && value >= minimum && value <= TerrainTools.Instance.MaxRadius;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
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
        if (modifier is RuntimeSettings { IsReset: true })
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
