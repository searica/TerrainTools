using HarmonyLib;
using UnityEngine;
using TerrainTools.Core;
using TerrainTools.Extensions;
using TerrainTools.Visualization;

namespace TerrainTools.Patches;


[HarmonyPatch]
internal class PlayerPatch
{

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
    private static void UpdatePlacementGhostPostfix(Player __instance)
    {
        if (!__instance || !__instance.InPlaceMode() || __instance.IsDead())
        {
            return;
        }

        if (__instance != Player.m_localPlayer || !__instance.m_placementGhost || !__instance.m_placementGhost.activeInHierarchy)
        {
            return;
        }

        OverlayVisualizer overlay = __instance.m_placementGhost.GetComponent<OverlayVisualizer>();
        if (!overlay) { return; }

        Vector3 position = __instance.m_placementGhost.transform.position;
        position.x = position.x.RoundToNearest(1.0f);
        position.z = position.z.RoundToNearest(1.0f);
        __instance.m_placementGhost.transform.position = position;

        TerrainOp terrainOp = __instance.m_placementGhost.GetComponent<TerrainOp>();
        if (terrainOp && !PreciseTerrainModifier.HasAreaAccess(terrainOp, position, false))
        {
            __instance.m_placementStatus = Player.PlacementStatus.PrivateZone;
        }
        overlay.Refresh();
        RadiusModifier.RefreshGhostScale(__instance);
    }
}
