using HarmonyLib;
using Logging;
using System.Collections.Generic;
using TerrainTools.Extensions;
using TerrainTools.Visualization;
using UnityEngine;

namespace TerrainTools.Core;

[HarmonyPatch]
internal static class SharpnessModifier
{
    /* For Raise Power the effect over the tool radius is calculated as:
     * y = (1 - x/radius)^p where x is distance from center.
     *
     * For Smooth Power the effect over the tool radius is calculated as:
     * y = 1 - (x/radius)^p
     *
     * So for smoothing, increasing the power increases the "sharpness" or evenness of the effect over the area.
     *
     * But for raising, increasing the power decreases the "sharpness" or evenness of the effect over the area.
     */
    private static bool SmoothToolIsInUse = false;
    private static float lastModdedSmoothPwr;
    private static float lastTotalSmoothDelta;
    private const float MinSmoothPwr = 1f;
    private const float MaxSmoothPwr = 30f;

    private static bool RaiseToolIsInUse = false;
    private static float lastModdedRaisePwr;
    private static float lastTotalRaiseDelta;
    private const float MinRaisePwr = 0.05f;
    private const float MaxRaisePwr = 1f;

    private static float lastRaisePower;
    private static float lastSmoothPower;

    private const float DisplayThreshold = 0.9f; // percentage
    private static float lastDisplayedSmoothSharpness;
    private static float lastDisplayedRaiseSharpness;


    [HarmonyPrefix]
    [HarmonyPriority(Priority.LowerThanNormal)]
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    private static void UpdatePrefix(Player __instance)
    {
        if (!__instance || __instance != Player.m_localPlayer)
        {
            return;
        }

        if (!__instance.InPlaceMode() || Hud.IsPieceSelectionVisible())
        {
            if (SmoothToolIsInUse)
            {
                SmoothToolIsInUse = false;
                lastModdedSmoothPwr = 0;
                lastTotalSmoothDelta = 0;
                lastDisplayedSmoothSharpness = -1;
                SetPower(__instance, 0);
            }

            if (RaiseToolIsInUse)
            {
                RaiseToolIsInUse = false;
                lastModdedRaisePwr = 0;
                lastTotalRaiseDelta = 0;
                lastDisplayedRaiseSharpness = -1;
                SetPower(__instance, 0);
            }

            return;
        }

        if (ShouldModifySharpness())
        {
            SetPower(__instance, Input.mouseScrollDelta.y * TerrainTools.Instance.SharpnessScrollScale);
        }
    }


    internal static bool ShouldModifySharpness()
    {
        return TerrainTools.Instance.IsEnableSharpnessModifier && Input.GetKey(TerrainTools.Instance.SharpnessKey) && Input.mouseScrollDelta.y != 0;
    }

    /// <summary>
    ///     Apply changes to sharpness just before the operation executes.
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="modifier"></param>
    [HarmonyPrefix]
    [HarmonyPriority(101)]
    [HarmonyPatch(typeof(TerrainComp), nameof(TerrainComp.InternalDoOperation))]
    private static void InternalDoOperationPrefix(TerrainComp __instance, TerrainOp.Settings modifier)
    {
        if (!__instance || modifier == null || modifier.IsPrecisionModifier())
        {
            return;
        }

        if (modifier.m_raise)
        {
            lastRaisePower = modifier.m_raisePower;
            modifier.m_raisePower = ModifyRaisePower(modifier.m_raisePower, lastTotalRaiseDelta);
            Log.LogInfo($"Applying raise power {modifier.m_raisePower}", Log.InfoLevel.Medium);
        }

        if (modifier.m_smooth)
        {
            lastSmoothPower = modifier.m_smoothPower;
            modifier.m_smoothPower = ModifySmoothPower(modifier.m_smoothPower, lastTotalSmoothDelta);
            Log.LogInfo($"Applying smooth power {modifier.m_smoothPower}", Log.InfoLevel.Medium);
        }
    }


    /// <summary>
    ///     Revert changes to sharpness just after the operation executes.
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

        if (modifier.m_raise)
        {
            modifier.m_raisePower = lastRaisePower;
            Log.LogInfo($"Restored raise power {modifier.m_raisePower}", Log.InfoLevel.Medium);
        }

        if (modifier.m_smooth)
        {
            modifier.m_smoothPower = lastSmoothPower;
            Log.LogInfo($"Restored smooth power {modifier.m_smoothPower}", Log.InfoLevel.Medium);
        }
    }

    private static void SetPower(Player player, float delta)
    {
        Piece piece = player.GetSelectedPiece();
        if (!piece || !piece.gameObject || piece.gameObject.GetComponent<OverlayVisualizer>())
        {
            return;
        }

        TerrainOp terrainOp = piece.gameObject.GetComponent<TerrainOp>();
        if (!terrainOp)
        {
            return;
        }

        SetSmoothPower(terrainOp, delta);
        SetRaisePower(terrainOp, delta);

        var updateMsg = new List<string>();
        if (SmoothToolIsInUse)
        {
            float smoothSharpness = GetSmoothPowerDisplayValue(lastModdedSmoothPwr);
            if (Mathf.Abs(smoothSharpness - lastDisplayedSmoothSharpness) > DisplayThreshold)
            {
                lastDisplayedSmoothSharpness = Mathf.Round(smoothSharpness);
                updateMsg.Add($"Terrain tool smoothing sharpness: {smoothSharpness:0}%");
            }
        }
        if (RaiseToolIsInUse)
        {
            

            float raiseSharpness = GetRaisePowerDisplayValue(lastModdedRaisePwr);
            if (Mathf.Abs(raiseSharpness - lastDisplayedRaiseSharpness) > DisplayThreshold)
            {
                lastDisplayedRaiseSharpness = Mathf.Round(raiseSharpness);

                string raiseType = terrainOp.m_settings.m_raiseDelta < 0 ? "dig" : "raise";
                updateMsg.Add($"Terrain tool {raiseType} sharpness: {raiseSharpness:0}%");
            }
        }
        if (SmoothToolIsInUse || RaiseToolIsInUse)
        {
            Sprite toolIcon = player.m_placementGhost.GetComponent<Piece>().m_icon;
            if (toolIcon != null && updateMsg.Count > 0)
            {
                player.Message(MessageHud.MessageType.Center, string.Join("\n", updateMsg.ToArray()), icon: toolIcon);
            }
        }
    }

    private static void SetSmoothPower(TerrainOp terrainOp, float delta)
    {
        if (!terrainOp.m_settings.m_smooth)
        {
            return;
        }

        Log.LogInfo($"Adjusting Smooth Power by {delta}", Log.InfoLevel.High);

        if (!SmoothToolIsInUse) // new terrain tool
        {
            SmoothToolIsInUse = true;
            lastModdedSmoothPwr = ModifySmoothPower(terrainOp.m_settings.m_smoothPower, delta);
        }
        else
        {
            lastModdedSmoothPwr = ModifySmoothPower(lastModdedSmoothPwr, delta);
        }
        lastTotalSmoothDelta += delta;
        Log.LogInfo($"Total smooth power delta {lastTotalSmoothDelta}", Log.InfoLevel.High);
    }

    private static void SetRaisePower(TerrainOp terrainOp, float delta)
    {
        if (!terrainOp.m_settings.m_raise)
        {
            return;
        }

        delta = ConvertSmoothDeltaToRaiseDelta(delta);

        Log.LogInfo($"Adjusting Raise Power by {delta}", Log.InfoLevel.High);

        if (!RaiseToolIsInUse) // new terrain tool
        {
            RaiseToolIsInUse = true;
            lastModdedRaisePwr = ModifyRaisePower(terrainOp.m_settings.m_raisePower, delta);
        }
        else
        {
            lastModdedRaisePwr = ModifyRaisePower(lastModdedRaisePwr, delta);
        }
        lastTotalRaiseDelta += delta;
        Log.LogInfo($"Total raise power delta {lastTotalRaiseDelta}", Log.InfoLevel.High);
    }

    /// <summary>
    ///     Converts delta for smooth power to be appropriate for raise power
    ///     since they have different value ranges and opposite signs
    /// </summary>
    /// <param name="delta"></param>
    /// <returns></returns>
    private static float ConvertSmoothDeltaToRaiseDelta(float delta)
    {
        float deltaFraction = delta / (MaxSmoothPwr - MinSmoothPwr);
        return -1 * deltaFraction * (MaxRaisePwr - MinRaisePwr);
    }

    /// <summary>
    ///     Get Smooth Power as a percentage of maximum sharpness
    /// </summary>
    /// <param name="power"></param>
    /// <returns></returns>
    private static float GetSmoothPowerDisplayValue(float power)
    {
        return ((power - MinSmoothPwr) / (MaxSmoothPwr - MinSmoothPwr)) * 100;
    }

    /// <summary>
    ///     Get Raise Power as a percentage of maximum sharpness
    /// </summary>
    /// <param name="power"></param>
    /// <returns></returns>
    private static float GetRaisePowerDisplayValue(float power, bool dig = false)
    {
        return ((MaxRaisePwr - power) / (MaxRaisePwr - MinRaisePwr)) * 100;
    }

    /// <summary>
    ///     Modifies power value and clamps to bounds for smooth power.
    /// </summary>
    /// <param name="power"></param>
    /// <param name="delta"></param>
    /// <returns></returns>
    private static float ModifySmoothPower(float power, float delta)
    {
        return Mathf.Clamp(power + delta, MinSmoothPwr, MaxSmoothPwr);
    }

    /// <summary>
    ///     Modifies power value and clamps to bounds for raise power.
    /// </summary>
    /// <param name="power"></param>
    /// <param name="delta"></param>
    /// <returns></returns>
    private static float ModifyRaisePower(float power, float delta)
    {
        return Mathf.Clamp(power + delta, MinRaisePwr, MaxRaisePwr);
    }
}
