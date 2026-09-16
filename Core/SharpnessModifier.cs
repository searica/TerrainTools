using HarmonyLib;
using System.Collections.Generic;
using Logging;
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
     * So for smoothing, increasing the power increases the "hardness" or evenness of the effect over the area.
     *
     * But for raising, increasing the power decreases the "hardness" or evenness of the effect over the area.
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


    [HarmonyPrefix]
    [HarmonyPriority(Priority.High)]
    [HarmonyPatch(typeof(TerrainOp), nameof(TerrainOp.Awake))]
    private static void AwakePrefix(TerrainOp __instance)
    {
        if (!__instance ||
            !__instance.gameObject ||
            __instance.gameObject.GetComponent<OverlayVisualizer>())
        {
            return;
        }

        if (__instance.m_settings.m_raise)
        {
            __instance.m_settings.m_raisePower = ModifyRaisePower(__instance.m_settings.m_raisePower, lastTotalRaiseDelta);
            Log.LogInfo($"Applying raise Power {__instance.m_settings.m_raisePower}", Log.InfoLevel.Medium);
        }

        if (__instance.m_settings.m_smooth)
        {
            __instance.m_settings.m_smoothPower = ModifySmoothPower(__instance.m_settings.m_smoothPower, lastTotalSmoothDelta);
            Log.LogInfo($"Applying smooth Power {__instance.m_settings.m_smoothPower}", Log.InfoLevel.Medium);
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
                updateMsg.Add($"Terrain tool smoothing hardness: {smoothSharpness:0}%");
            }
        }
        if (RaiseToolIsInUse)
        {
            float raiseSharpness = GetRaisePowerDisplayValue(lastModdedRaisePwr);
            if (Mathf.Abs(raiseSharpness - lastDisplayedRaiseSharpness) > DisplayThreshold)
            {
                lastDisplayedRaiseSharpness = Mathf.Round(raiseSharpness);
                updateMsg.Add($"Terrain tool raise hardness: {raiseSharpness:0}%");
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
    ///     Get Smooth Power as a percentage of maximum hardness
    /// </summary>
    /// <param name="power"></param>
    /// <returns></returns>
    private static float GetSmoothPowerDisplayValue(float power)
    {
        return ((power - MinSmoothPwr) / (MaxSmoothPwr - MinSmoothPwr)) * 100;
    }

    /// <summary>
    ///     Get Raise Power as a percentage of maximum hardness
    /// </summary>
    /// <param name="power"></param>
    /// <returns></returns>
    private static float GetRaisePowerDisplayValue(float power)
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
