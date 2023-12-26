using HarmonyLib;
using TerrainTools.Visualization;
using UnityEngine;

namespace TerrainTools.Helpers {
    [HarmonyPatch(typeof(TerrainComp))]
    public static class PreciseTerrainModifier {
        public const int SizeInTiles = 1;

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

                // could maybe claim ownership here?
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
                RemoveTerrainModifications(
                    pos,
                    __instance.m_hmap,
                    __instance.m_width,
                    ref __instance.m_levelDelta,
                    ref __instance.m_smoothDelta,
                    ref __instance.m_modifiedHeight
                );
                RecolorTerrain(
                    pos,
                    TerrainModifier.PaintType.Reset,
                    __instance.m_hmap,
                    __instance.m_width,
                    ref __instance.m_paintMask,
                    ref __instance.m_modifiedPaint
                );
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(TerrainComp.SmoothTerrain))]
        private static bool SmoothTerrianPrefix(TerrainComp __instance, Vector3 worldPos, float radius) {
            if (IsPrecisionModifier(radius)) {
                SmoothenTerrain(
                    __instance,
                    worldPos,
                    __instance.m_hmap,
                    __instance.m_width,
                    ref __instance.m_smoothDelta,
                    ref __instance.m_modifiedHeight
                );
                return false;
            }
            else {
                return true;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(TerrainComp.RaiseTerrain))]
        private static bool RaiseTerrainPrefix(TerrainComp __instance, Vector3 worldPos, float radius, float delta) {
            if (IsPrecisionModifier(radius)) {
                RaiseTerrain(
                    __instance,
                    worldPos,
                    __instance.m_hmap,
                    __instance.m_width,
                    delta,
                    ref __instance.m_levelDelta,
                    ref __instance.m_smoothDelta,
                    ref __instance.m_modifiedHeight
                );
                return false;
            }
            else {
                return true;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(TerrainComp.PaintCleared))]
        private static bool PreciseColorModificaton(
            TerrainComp __instance,
            Vector3 worldPos,
            float radius,
            TerrainModifier.PaintType paintType
        ) {
            if (IsPrecisionModifier(radius)) {
                RecolorTerrain(
                    worldPos,
                    paintType,
                    __instance.m_hmap,
                    __instance.m_width,
                    ref __instance.m_paintMask,
                    ref __instance.m_modifiedPaint
                );
                return false;
            }
            else {
                return true;
            }
        }

        public static void SmoothenTerrain(
            TerrainComp compiler,
            Vector3 worldPos,
            Heightmap hMap,
            int worldWidth,
            ref float[] smoothDelta,
            ref bool[] modifiedHeight
        ) {
            Log.LogInfo("[INIT] Smooth Terrain Modification", LogLevel.Medium);
            var worldSize = worldWidth + 1;
            hMap.WorldToVertex(worldPos, out var xPos, out var yPos);
            var referenceH = worldPos.y - compiler.transform.position.y;
            Log.LogInfo($"worldPos: {worldPos}, xPos: {xPos}, yPos: {yPos}, referenceH: {referenceH}", LogLevel.Medium);

            FindExtrema(xPos, worldSize, out var xMin, out var xMax);
            FindExtrema(yPos, worldSize, out var yMin, out var yMax);
            for (var x = xMin; x <= xMax; x++) {
                for (var y = yMin; y <= yMax; y++) {
                    var tileIndex = y * worldSize + x;
                    var tileH = hMap.GetHeight(x, y);
                    var deltaH = referenceH - tileH;
                    var oldDeltaH = smoothDelta[tileIndex];
                    var newDeltaH = oldDeltaH + deltaH;
                    var roundedNewDeltaH = RoundToTwoDecimals(tileH, oldDeltaH, newDeltaH);
                    var limDeltaH = Mathf.Clamp(roundedNewDeltaH, -1.0f, 1.0f);
                    smoothDelta[tileIndex] = limDeltaH;
                    modifiedHeight[tileIndex] = true;

                    Log.LogInfo($"tilePos: ({x}, {y}), tileH: {tileH}, deltaH: {deltaH}, oldDeltaH: {oldDeltaH}, newDeltaH: {newDeltaH}, roundedNewDeltaH: {roundedNewDeltaH}, limDeltaH: {limDeltaH}", LogLevel.Medium);
                }
            }
            Log.LogInfo("[SUCCESS] Smooth Terrain Modification", LogLevel.Medium);
        }

        public static void RaiseTerrain(
            TerrainComp compiler,
            Vector3 worldPos,
            Heightmap hMap,
            int worldWidth,
            float power,
            ref float[] levelDelta,
            ref float[] smoothDelta,
            ref bool[] modifiedHeight
        ) {
            Log.LogInfo("[INIT] Raise Terrain Modification", LogLevel.Medium);
            var worldSize = worldWidth + 1;
            hMap.WorldToVertex(worldPos, out var xPos, out var yPos);
            var referenceH = worldPos.y - compiler.transform.position.y + power;
            Log.LogInfo(
                $"worldPos: {worldPos}, xPos: {xPos}, yPos: {yPos}, power: {power}, referenceH: {referenceH}",
                LogLevel.Medium
            );
            FindExtrema(xPos, worldSize, out var xMin, out var xMax);
            FindExtrema(yPos, worldSize, out var yMin, out var yMax);
            for (var x = xMin; x <= xMax; x++) {
                for (var y = yMin; y <= yMax; y++) {
                    var tileIndex = y * worldSize + x;
                    var tileH = hMap.GetHeight(x, y);
                    var deltaH = referenceH - tileH;
                    if (deltaH >= 0) {
                        var oldLevelDelta = levelDelta[tileIndex];
                        var oldSmoothDelta = smoothDelta[tileIndex];
                        var newLevelDelta = oldLevelDelta + oldSmoothDelta + deltaH;
                        var newSmoothDelta = 0f;
                        var roundedNewLevelDelta = RoundToTwoDecimals(
                            tileH,
                            oldLevelDelta + oldSmoothDelta,
                            newLevelDelta + newSmoothDelta
                        );
                        var limitedNewLevelDelta = Mathf.Clamp(roundedNewLevelDelta, -16.0f, 16.0f);
                        levelDelta[tileIndex] = limitedNewLevelDelta;
                        smoothDelta[tileIndex] = newSmoothDelta;
                        modifiedHeight[tileIndex] = true;

                        Log.LogInfo(
                            $"tilePos: ({x}, {y}), tileH: {tileH}, deltaH: {deltaH}, oldLevelDelta: {oldLevelDelta}, oldSmoothDelta: {oldSmoothDelta}, newLevelDelta: {newLevelDelta}, newSmoothDelta: {newSmoothDelta}, roundedNewLevelDelta: {roundedNewLevelDelta}, limitedNewLevelDelta: {limitedNewLevelDelta}",
                            LogLevel.Medium
                        );
                    }
                    else {
                        Log.LogInfo("Declined to process tile: deltaH < 0!", LogLevel.Medium);
                        Log.LogInfo($"tilePos: ({x}, {y}), tileH: {tileH}, deltaH: {deltaH}", LogLevel.Medium);
                    }
                }
            }
            Log.LogInfo("[SUCCESS] Raise Terrain Modification", LogLevel.Medium);
        }

        public static void RemoveTerrainModifications(
            Vector3 worldPos,
            Heightmap hMap,
            int worldWidth,
            ref float[] levelDelta,
            ref float[] smoothDelta,
            ref bool[] modifiedHeight
        ) {
            Log.LogInfo("[INIT] Remove Terrain Modifications", LogLevel.Medium);

            var worldSize = worldWidth + 1;
            hMap.WorldToVertex(worldPos, out var xPos, out var yPos);
            Log.LogInfo($"worldPos: {worldPos}, vertexPos: ({xPos}, {yPos})", LogLevel.Medium);

            FindExtrema(xPos, worldSize, out var xMin, out var xMax);
            FindExtrema(yPos, worldSize, out var yMin, out var yMax);
            for (var x = xMin; x <= xMax; x++) {
                for (var y = yMin; y <= yMax; y++) {
                    var tileIndex = y * worldSize + x;
                    levelDelta[tileIndex] = 0;
                    smoothDelta[tileIndex] = 0;
                    modifiedHeight[tileIndex] = false;
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

        public static Color ResolveColor(TerrainModifier.PaintType paintType) {
            if (paintType == TerrainModifier.PaintType.Dirt) { return Color.red; }
            if (paintType == TerrainModifier.PaintType.Paved) { return Color.blue; }
            if (paintType == TerrainModifier.PaintType.Cultivate) { return Color.green; }
            return Color.black;
        }

        public static void FindExtrema(int x, int worldSize, out int xMin, out int xMax) {
            xMin = Mathf.Max(0, x - SizeInTiles);
            xMax = Mathf.Min(x + SizeInTiles, worldSize - 1);
        }

        public static float RoundToTwoDecimals(float oldH, float oldDeltaH, float newDeltaH) {
            var newH = oldH - oldDeltaH + newDeltaH;
            var roundedNewH = Mathf.Round(newH * 100) / 100;
            var roundedNewDeltaH = roundedNewH - oldH + oldDeltaH;
            Log.LogInfo(
                $"oldH: {oldH}, oldDeltaH: {oldDeltaH}, newDeltaH: {newDeltaH}, newH: {newH}, roundedNewH: {roundedNewH}, roundedNewDeltaH: {roundedNewDeltaH}",
                LogLevel.Medium
            );

            return roundedNewDeltaH;
        }
    }
}