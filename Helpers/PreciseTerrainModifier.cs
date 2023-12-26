using HarmonyLib;
using TerrainTools.Visualization;
using UnityEngine;

namespace TerrainTools.Helpers {
    [HarmonyPatch(typeof(TerrainComp))]
    public static class PreciseTerrainModifier {
        public const int FixedRadius = 1;

        [HarmonyPrefix]
        [HarmonyPatch(nameof(TerrainComp.ApplyOperation))]
        private static void ApplyOperationPrefix(TerrainOp modifier) {
            if (!modifier || !modifier.gameObject) { return; }

            // Set radius to -inf so I can check if custom overlay in later methods
            if (modifier.gameObject.GetComponentInChildren<OverlayVisualizer>()) {
                if (modifier.m_settings.m_smooth) {
                    modifier.m_settings.m_smoothRadius = float.NegativeInfinity;
                }
                if (modifier.m_settings.m_raise && modifier.m_settings.m_raiseDelta >= 0) {
                    modifier.m_settings.m_raiseRadius = float.NegativeInfinity;
                    modifier.m_settings.m_raiseDelta = GroundLevelSpinner.Value;
                }
                if (modifier.m_settings.m_paintCleared) {
                    modifier.m_settings.m_paintRadius = float.NegativeInfinity;
                }
            }
        }

        /// <summary>
        ///     Claim ownership before sending RPC to do terrain operation to
        ///     ensure that custom terrain ops run on a PC with the mod.
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(TerrainComp.RPC_ApplyOperation))]
        private static void RPC_ApplyOperationPrefix(TerrainComp __instance) {
            if (!__instance || !__instance.m_nview) {
                return;
            }

            if (!__instance.m_nview.IsOwner()) {
                __instance.m_nview.ClaimOwnership();
            }
        }

        /// <summary>
        ///     Checks if radius is set as flag for precision modifier.
        /// </summary>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static bool IsPrecisionModifier(float radius) {
            return radius == float.NegativeInfinity;
        }


        [HarmonyPrefix]
        [HarmonyPatch(nameof(TerrainComp.InternalDoOperation))]
        private static bool InternalDoOperationPrefix(
            TerrainComp __instance,
            Vector3 pos,
            TerrainOp.Settings modifier
        ) {
            if (!modifier.m_level && !modifier.m_raise && !modifier.m_smooth && !modifier.m_paintCleared) {
                RemoveTerrainModifications(__instance, pos);
                PreciseRecolorTerrain(__instance, pos, TerrainModifier.PaintType.Reset);
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(TerrainComp.SmoothTerrain))]
        private static bool PreciseSmoothTerrian(TerrainComp __instance, Vector3 worldPos, float radius) {
            if (!IsPrecisionModifier(radius)) {
                return true;
            }

            Log.LogInfo("PreciseSmoothTerrain", LogLevel.Medium);
            var worldSize = __instance.m_hmap.m_width + 1;
            __instance.m_hmap.WorldToVertex(worldPos, out var xPos, out var yPos);
            var refHeight = worldPos.y - __instance.transform.position.y;
            Log.LogInfo($"worldPos: {worldPos}, xPos: {xPos}, yPos: {yPos}, referenceH: {refHeight}", LogLevel.Medium);

            FindExtrema(xPos, worldSize, out var xMin, out var xMax);
            FindExtrema(yPos, worldSize, out var yMin, out var yMax);

            for (var i = xMin; i <= xMax; i++) {
                for (var j = yMin; j <= yMax; j++) {
                    var tileIndex = j * worldSize + i;
                    var tileHeight = __instance.m_hmap.GetHeight(i, j);
                    var deltaHeight = refHeight - tileHeight;
                    var oldSmoothDelta = __instance.m_smoothDelta[tileIndex];
                    __instance.m_smoothDelta[tileIndex] = Mathf.Clamp(oldSmoothDelta + deltaHeight, -1.0f, 1.0f);
                    __instance.m_modifiedHeight[tileIndex] = true;

                    Log.LogInfo($"tilePos: ({i}, {j}), tileH: {tileHeight}, deltaH: {deltaHeight}, oldSmoothDelta: {oldSmoothDelta}, newSmoothDelta {__instance.m_smoothDelta[tileIndex]}", LogLevel.Medium);
                }
            }
            Log.LogInfo("[SUCCESS] Smooth Terrain Modification", LogLevel.Medium);

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(TerrainComp.RaiseTerrain))]
        private static bool RaiseTerrainPrefix(TerrainComp __instance, Vector3 worldPos, float radius, float delta) {
            if (!IsPrecisionModifier(radius)) {
                return true;
            }
            Log.LogInfo("[INIT] Raise Terrain Modification", LogLevel.Medium);
            var worldSize = __instance.m_width + 1;
            __instance.m_hmap.WorldToVertex(worldPos, out var xPos, out var yPos);
            var refHeight = worldPos.y - __instance.transform.position.y;
            Log.LogInfo(
                $"worldPos: {worldPos}, xPos: {xPos}, yPos: {yPos}, delta: {delta}, refHeight: {refHeight}",
                LogLevel.Medium
            );

            FindExtrema(xPos, worldSize, out var xMin, out var xMax);
            FindExtrema(yPos, worldSize, out var yMin, out var yMax);

            for (var i = xMin; i <= xMax; i++) {
                for (var j = yMin; j <= yMax; j++) {
                    var tileHeight = __instance.m_hmap.GetHeight(i, j);
                    var targetHeight = refHeight + delta;

                    if (delta < 0f && targetHeight > tileHeight) {
                        continue;
                    }

                    if (delta >= 0f) {
                        if (targetHeight < tileHeight) {
                            continue;
                    }
                        if (targetHeight > tileHeight + delta) {
                            targetHeight = tileHeight + delta;
                    }
                }

                    var tileIndex = j * worldSize + i;
                    __instance.m_levelDelta[tileIndex] += targetHeight - tileHeight + __instance.m_smoothDelta[tileIndex];
                    __instance.m_smoothDelta[tileIndex] = 0f;
                    __instance.m_levelDelta[tileIndex] = Mathf.Clamp(__instance.m_levelDelta[tileIndex], -8f, 8f);
                    __instance.m_modifiedHeight[tileIndex] = true;
            }
            }
            Log.LogInfo("[SUCCESS] Raise Terrain Modification", LogLevel.Medium);

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(TerrainComp.PaintCleared))]
        private static bool PaintClearedPrefix(
            TerrainComp __instance,
            Vector3 worldPos,
            float radius,
            TerrainModifier.PaintType paintType
        ) {
            if (!IsPrecisionModifier(radius)) {
                return true;
            }

            PreciseRecolorTerrain(__instance, worldPos, paintType);
            return false;
        }


        public static void RemoveTerrainModifications(TerrainComp comp, Vector3 worldPos) {
            Log.LogInfo("[INIT] Remove Terrain Modifications", LogLevel.Medium);

            var worldSize = comp.m_width + 1;
            comp.m_hmap.WorldToVertex(worldPos, out var xPos, out var yPos);
            Log.LogInfo($"worldPos: {worldPos}, vertexPos: ({xPos}, {yPos})", LogLevel.Medium);

            FindExtrema(xPos, worldSize, out var xMin, out var xMax);
            FindExtrema(yPos, worldSize, out var yMin, out var yMax);
            for (var x = xMin; x <= xMax; x++) {
                for (var y = yMin; y <= yMax; y++) {
                    var tileIndex = y * worldSize + x;
                    comp.m_levelDelta[tileIndex] = 0;
                    comp.m_smoothDelta[tileIndex] = 0;
                    comp.m_modifiedHeight[tileIndex] = false;
                    Log.LogInfo($"tilePos: ({x}, {y}), tileIndex: {tileIndex}", LogLevel.Medium);
                }
            }
            Log.LogInfo("[SUCCESS] Remove Terrain Modifications", LogLevel.Medium);
        }

        public static void PreciseRecolorTerrain(
            TerrainComp comp,
            Vector3 worldPos,
            TerrainModifier.PaintType paintType
        ) {
            Log.LogInfo("[INIT] PreciseRecolorTerrain", LogLevel.Medium);
            //worldPos.x -= 0.5f;
            //worldPos.z -= 0.5f;
            comp.m_hmap.WorldToVertex(worldPos, out var xPos, out var yPos);

            var tileColor = ResolveColor(paintType);
            var removeColor = paintType == TerrainModifier.PaintType.Reset;

            FindExtrema(xPos, comp.m_width + 1, out var xMin, out var xMax);
            FindExtrema(yPos, comp.m_width + 1, out var yMin, out var yMax);

            for (var i = xMin; i < xMax; i++) {
                for (var j = yMin; j < yMax; j++) {
                    var tileIndex = j * comp.m_width + i;
                    comp.m_paintMask[tileIndex] = tileColor;
                    comp.m_modifiedPaint[tileIndex] = !removeColor;
                    Log.LogInfo($"tilePos: ({i}, {j}), tileIndex: {tileIndex}, tileColor: {tileColor}", LogLevel.Medium);
                }
            }
            Log.LogInfo("[SUCCESS] Color Terrain Modification", LogLevel.Medium);
        }

        public static UnityEngine.Color ResolveColor(TerrainModifier.PaintType paintType) {
            switch (paintType) {
                case TerrainModifier.PaintType.Dirt:
                    return Heightmap.m_paintMaskDirt;
                case TerrainModifier.PaintType.Cultivate:
                    return Heightmap.m_paintMaskCultivated;
                case TerrainModifier.PaintType.Paved:
                    return Heightmap.m_paintMaskPaved;
                case TerrainModifier.PaintType.Reset:
                    return Heightmap.m_paintMaskNothing;
                default:
                    break;
            }
            return Heightmap.m_paintMaskNothing;
        }

        public static void FindExtrema(int x, int worldSize, out int xMin, out int xMax) {
            xMin = Mathf.Max(0, x - FixedRadius);
            xMax = Mathf.Min(x + FixedRadius, worldSize - 1);
        }
    }
}