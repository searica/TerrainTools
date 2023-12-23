using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainTools.Visualization;
using UnityEngine;

namespace TerrainTools.Patches {

    [HarmonyPatch]
    internal class PlayerPatch {

        [HarmonyFinalizer]
        [HarmonyPatch(nameof(Player.UpdatePlacementGhost))]
        private static void UpdatePlacementGhostPostfix(Player __instance) {
            if (!__instance || !__instance.InPlaceMode() || __instance.IsDead()) {
                return;
            }

            if (!__instance.m_placementGhost || !__instance.m_placementGhost.GetComponent<OverlayVisualizer>()) {
                return;
            }


            var position = __instance.m_placementGhost.transform.position;
            position.x = RoundToNearest(position.x, 0.5f);
            position.z = RoundToNearest(position.z, 0.5f);
            // Snaps center of piece to the ground, which is not what I want
            //Log.LogInfo($"Pre ground snap {position}");
            //var groundHeight = GetGroundHeight(position);
            //if (position.y != groundHeight) { position.y = groundHeight; }
            //Log.LogInfo($"Post ground snap {position}");
            //if (Mathf.Abs(position.y - groundHeight) < 0.25f) { position.y = groundHeight; }

            __instance.m_placementGhost.transform.position = position;
        }

        /// <summary>
        ///     Round to nearest multiple of precision (midpoint rounds away from zero)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="precision"></param>
        /// <returns></returns>
        private static float RoundToNearest(float x, float precision) {
            if (precision <= 0) { return x; }
            var sign = Mathf.Sign(x);

            var val = (int)Mathf.Abs(x * 1000f);
            var whole = val / 1000;
            var fraction = val % 1000;

            int midPoint = (int)(precision * 1000f / 2f);

            if (fraction < midPoint) {
                return sign * whole;
            }
            return sign * (whole + precision);
        }
    }
}
