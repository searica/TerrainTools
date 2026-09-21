using HarmonyLib;
using Logging;
using TerrainTools.Extensions;
using TerrainTools.Visualization;
using UnityEngine;

namespace TerrainTools.Core;

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

    /// <summary>
    ///     Claim ownership before sending RPC to do terrain operation to
    ///     ensure that custom terrain ops run on a PC with the mod.
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPrefix]
    [HarmonyPriority(Priority.VeryLow)]
    [HarmonyPatch(typeof(TerrainComp), nameof(TerrainComp.ApplyOperation))]
    private static void ApplyOperationPrefix(TerrainComp __instance, TerrainOp modifier)
    {
        if (!__instance || !__instance.m_nview || !modifier || !modifier.gameObject)
        {
            return;
        }

        //RegisterTerrainOpInObjectDB(modifier);

        if (!__instance.m_nview.IsOwner())
        {
            __instance.m_nview.ClaimOwnership();
        }

        
    }

    ///// <summary>
    /////     Ensure custom TerrainOp prefabs are registered in ObjectDB so that they can be deserialized correctly. 
    ///// </summary>
    //internal static void RegisterTerrainOpInObjectDB(TerrainOp modifer)
    //{
    //    if (
    //        !ObjectDB.instance || !modifer || !modifer.gameObject || ObjectDB.instance.m_terrainOpsByHash == null || ObjectDB.instance.m_terrainOps == null
    //    )
    //    {
    //        return;
    //    }

    //    int hash = modifer.name.GetStableHashCode();
    //    if (ObjectDB.instance.m_terrainOpsByHash.TryGetValue(hash, out TerrainOp registeredTerrainOp))
    //    {
    //        if (registeredTerrainOp != modifer)
    //        {
    //            Log.LogWarning($"TerrainOp prefab hash collision for {modifer.name} ({hash}); keeping the registered prefab");
    //        }
    //        return;
    //    }

    //    if (!ObjectDB.instance.m_terrainOps.Contains(modifer))
    //    {
    //        ObjectDB.instance.m_terrainOps.Add(modifer);
    //    }

    //    if (!ObjectDB.instance.m_terrainOpsByHash.ContainsKey(hash))
    //    {
    //        ObjectDB.instance.m_terrainOpsByHash.Add(hash, modifer);
    //    }

    //    Log.LogInfo($"Registered TerrainOp {modifer.name} in ObjectDB", Log.InfoLevel.Medium);
    //}

    /// <summary>
    ///     Modify deserialized result to have setting that all me to check if it is a precision terrain operation
    ///     and update the raise delta for the precise raise tool
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPostfix]
    [HarmonyPriority(Priority.VeryHigh)]
    [HarmonyPatch(typeof(TerrainOp.Settings), nameof(TerrainOp.Settings.Deserialize))]
    private static void TerrainOpDeserializePostfix(ZPackage pkg, ref TerrainOp.Settings __result)
    {
        if (__result is null || pkg is null)
        {
            return;
        }

        // read int to get hash
        pkg.SetPos(pkg.GetPos() - 4);  
        int hash = pkg.ReadInt();

        if (ObjectDB.instance && ObjectDB.instance.TryGetTerrainOp(hash, out TerrainOp terrainOp) && terrainOp)
        {
            // Set radius to -inf so I can check if custom overlay in later methods
            if (terrainOp.GetComponentInChildren<OverlayVisualizer>())
            {
                Log.LogInfo($"Deserializing precision tool: {terrainOp.name}", Log.InfoLevel.Medium);
                if (__result.m_smooth)
                {
                    __result.m_smoothRadius = float.NegativeInfinity;
                }
                if (__result.m_raise && __result.m_raiseDelta >= 0)
                {
                    __result.m_raiseRadius = float.NegativeInfinity;
                    __result.m_raiseDelta = GroundLevelSpinner.Value;
                }
                if (__result.m_paintCleared)
                {
                    __result.m_paintRadius = float.NegativeInfinity;
                }
            }
        }
    }

    /// <summary>
    ///     Modify the operation radius to accurately reflect the fixed radius of precise terrain modifications. This ensures that
    ///     the m_lastOpRadius is not set to an invalid radius when a precision modifier is the last operation applied and avoids issues
    ///     caused by saving an invalid radius.
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="__result"></param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TerrainOp), nameof(TerrainOp.GetRadius))]
    private static void GetRadiusPostfix(TerrainOp __instance, ref float __result)
    {
        if (!__instance)
        {
            return;
        }

        // return correct radius for precision tools
        if (__instance.m_settings.IsPrecisionModifier())
        {
            __result = Mathf.Max(__result, __instance.GetFixedRadius());
        }
    }


    /// <summary>
    ///     Apply TerrainReset operation if valid
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="pos"></param>
    /// <param name="modifier"></param>
    [HarmonyPrefix]
    [HarmonyPriority(Priority.VeryHigh)]
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


    [HarmonyPrefix]
    [HarmonyPatch(typeof(TerrainComp), nameof(TerrainComp.SmoothTerrain))]
    private static bool SmoothTerrainPrefix(
        TerrainComp __instance,
        Vector3 worldPos,
        float radius
    )
    {
        if (TerrainCompExtensions.IsPrecisionModifier(radius))
        {
            __instance.PreciseSmoothTerrain(worldPos);
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(TerrainComp), nameof(TerrainComp.RaiseTerrain))]
    private static bool RaiseTerrainPrefix(TerrainComp __instance, Vector3 worldPos, float radius, float delta)
    {
        if (TerrainCompExtensions.IsPrecisionModifier(radius))
        {
            __instance.PreciseRaiseTerrain(worldPos, delta);
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(TerrainComp), nameof(TerrainComp.PaintCleared))]
    private static bool PaintClearedPrefix(
        TerrainComp __instance,
        Vector3 worldPos,
        Vector3 rot,
        TerrainOp.Settings settings
    )
    {
        if (TerrainCompExtensions.IsPrecisionModifier(settings.m_paintRadius))
        {
            __instance.PreciseRecolorTerrain(worldPos, settings.m_paintType);
            return false;
        }
        return true;
        
    }
}
