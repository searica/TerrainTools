using HarmonyLib;
using TerrainTools.Extensions;
using TerrainTools.Visualization;
using UnityEngine;
using UnityEngine.UIElements;

namespace TerrainTools.Patches;


[HarmonyPatch]
internal class PlayerPatch
{
    private static int m_width = 64;
    private static float m_scale = 1.0f;
    private static float PaintStepSize = 64f / 65f;

    /// <summary>
    ///  Snap precision tools to the existing paint grid
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
    private static void UpdatePlacementGhostPostfix(Player __instance)
    {
        if (!__instance || !__instance.InPlaceMode() || __instance.IsDead())
        {
            return;
        }

        if (!__instance.m_placementGhost || !__instance.m_placementGhost.GetComponent<OverlayVisualizer>())
        {
            return;
        }

        Vector3 position = __instance.m_placementGhost.transform.position;
        position.x = position.x.RoundToNearest(1.0f);
        position.z = position.z.RoundToNearest(1.0f);
        __instance.m_placementGhost.transform.position = position;
    }
}
