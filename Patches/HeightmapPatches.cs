using UnityEngine;
using HarmonyLib;
using UnityEngine.UIElements;

namespace TerrainTools.Patches;


/// <summary>
///     Fix Vanilla issues with misaligned paint mask pixel centers
/// </summary>
[HarmonyPatch]
internal static class HeightmapPatches
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(Heightmap), nameof(Heightmap.IsCleared))]
    private static void IsClearedPrefix(Heightmap __instance, ref Vector3 worldPos)
    {
        CounterOffset(ref worldPos);
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(Heightmap), nameof(Heightmap.GetVegetationMask))]
    private static void GetVegetationMaskPrefix(Heightmap __instance, ref Vector3 worldPos)
    {
        CounterOffset(ref worldPos);
    }

    [HarmonyPostfix]
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(typeof(Heightmap), nameof(Heightmap.IsCleared))]
    private static void IsClearedPostfix(Heightmap __instance, ref Vector3 worldPos)
    {
        UndoCounterOffset(ref worldPos);
    }

    [HarmonyPostfix]
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(typeof(Heightmap), nameof(Heightmap.GetVegetationMask))]
    private static void GetVegetationMaskPostfix(Heightmap __instance, ref Vector3 worldPos)
    {
        UndoCounterOffset(ref worldPos);
    }


    /// <summary>
    ///     Add 0.5 to cancel out the -0.5 done in GetVegetationMask and IsCleared
    /// </summary>
    /// <param name="worldPos"></param>
    private static void CounterOffset(ref Vector3 worldPos)
    {
        worldPos.x += 0.5f;
        worldPos.z += 0.5f;
    }

    /// <summary>
    ///     Remove 0.5 to cancel out the +0.5 done in CounterOffset
    /// </summary>
    /// <param name="worldPos"></param>
    private static void UndoCounterOffset(ref Vector3 worldPos)
    {
        worldPos.x -= 0.5f;
        worldPos.z -= 0.5f;
    }

    /// <summary>
    ///     Swap out m_scale to reflect actual positioning of paint mask pixel centers
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="__state"></param>
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(Heightmap), nameof(Heightmap.WorldToVertexMask))] 
    public static void WorldPosToPaintMaskVertexPrefix(Heightmap __instance, out float __state)
    {
        __state = __instance.m_scale;
        __instance.m_scale = __instance.GetPaintMaskScale();
    }


    /// <summary>
    ///     Swap m_scale back to original value
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="__state"></param>
    [HarmonyPostfix]
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(typeof(Heightmap), nameof(Heightmap.WorldToVertexMask))]
    public static void WorldPosToPaintMaskVertexPostfix(Heightmap __instance, ref float __state)
    {
        __instance.m_scale = __state;
    }


    /// <summary>
    ///     Calculate the correct scale factor for the width of paint mask pixels
    /// </summary>
    /// <param name="hm"></param>
    /// <returns></returns>
    public static float GetPaintMaskScale(this Heightmap hm)
    {
        float width = (float)hm.m_width;
        return width / (width + 1f);
    }

    /// <summary>
    ///     Get pixel width of paint mask.
    /// </summary>
    /// <param name="hm"></param>
    /// <returns></returns>
    public static int GetPaintMaskWidth(this Heightmap hm)
    {
        return hm.m_width + 1;
    }

    public static Vector3 PaintMaskVertexToWorldPos(this Heightmap hm, int i, int j)
    {
        Vector3 worldPos = hm.transform.position;
        int half = hm.GetPaintMaskWidth() / 2;
        float scale = hm.GetPaintMaskScale();
        worldPos.x = worldPos.x + scale*(i - half);
        worldPos.z = worldPos.z + scale*(j - half);
        return worldPos;
    }
}
