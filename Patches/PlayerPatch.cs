using HarmonyLib;
using UnityEngine;
using TerrainTools.Extensions;
using TerrainTools.Visualization;

namespace TerrainTools.Patches;


[HarmonyPatch]
internal class PlayerPatch
{

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
