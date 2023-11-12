using HarmonyLib;
using UnityEngine;

namespace TerrainTools
{
    [HarmonyPatch(typeof(TerrainComp))]
    public static class PreciseTerrainModifier
    {
        public const int SizeInTiles = 1;

        [HarmonyPrefix]
        [HarmonyPatch(nameof(TerrainComp.InternalDoOperation))]
        private static bool InternalDoOperationPrefix(
            Vector3 pos,
            TerrainOp.Settings modifier,
            Heightmap ___m_hmap,
            int ___m_width,
            ref float[] ___m_levelDelta,
            ref float[] ___m_smoothDelta,
            ref Color[] ___m_paintMask,
            ref bool[] ___m_modifiedHeight,
            ref bool[] ___m_modifiedPaint
        )
        {
            if (!modifier.m_level && !modifier.m_raise && !modifier.m_smooth && !modifier.m_paintCleared)
            {
                RemoveTerrainModifications(pos, ___m_hmap, ___m_width, ref ___m_levelDelta, ref ___m_smoothDelta, ref ___m_modifiedHeight);
                RecolorTerrain(pos, TerrainModifier.PaintType.Reset, ___m_hmap, ___m_width, ref ___m_paintMask, ref ___m_modifiedPaint);
            }
            return true;
        }

        public static void SmoothenTerrain(
            Vector3 worldPos,
            Heightmap hMap,
            TerrainComp compiler,
            int worldWidth,
            ref float[] smoothDelta,
            ref bool[] modifiedHeight
        )
        {
            Debug.Log("[INIT] Smooth Terrain Modification");

            var worldSize = worldWidth + 1;
            hMap.WorldToVertex(worldPos, out var xPos, out var yPos);
            var referenceH = worldPos.y - compiler.transform.position.y;
            Debug.Log($"worldPos: {worldPos}, xPos: {xPos}, yPos: {yPos}, referenceH: {referenceH}");

            FindExtremums(xPos, worldSize, out var xMin, out var xMax);
            FindExtremums(yPos, worldSize, out var yMin, out var yMax);
            for (var x = xMin; x <= xMax; x++)
            {
                for (var y = yMin; y <= yMax; y++)
                {
                    var tileIndex = y * worldSize + x;
                    var tileH = hMap.GetHeight(x, y);
                    var deltaH = referenceH - tileH;
                    var oldDeltaH = smoothDelta[tileIndex];
                    var newDeltaH = oldDeltaH + deltaH;
                    var roundedNewDeltaH = RoundToTwoDecimals(tileH, oldDeltaH, newDeltaH);
                    var limDeltaH = Mathf.Clamp(roundedNewDeltaH, -1.0f, 1.0f);
                    smoothDelta[tileIndex] = limDeltaH;
                    modifiedHeight[tileIndex] = true;
                    Debug.Log($"tilePos: ({x}, {y}), tileH: {tileH}, deltaH: {deltaH}, oldDeltaH: {oldDeltaH}, newDeltaH: {newDeltaH}, roundedNewDeltaH: {roundedNewDeltaH}, limDeltaH: {limDeltaH}");
                }
            }
            Debug.Log("[SUCCESS] Smooth Terrain Modification");
        }

        public static void RaiseTerrain(
            Vector3 worldPos,
            Heightmap hMap,
            TerrainComp compiler,
            int worldWidth,
            float power,
            ref float[] levelDelta,
            ref float[] smoothDelta,
            ref bool[] modifiedHeight
        )
        {
            Debug.Log("[INIT] Raise Terrain Modification");

            var worldSize = worldWidth + 1;
            hMap.WorldToVertex(worldPos, out var xPos, out var yPos);
            var referenceH = worldPos.y - compiler.transform.position.y + power;
            Debug.Log($"worldPos: {worldPos}, xPos: {xPos}, yPos: {yPos}, power: {power}, referenceH: {referenceH}");

            FindExtremums(xPos, worldSize, out var xMin, out var xMax);
            FindExtremums(yPos, worldSize, out var yMin, out var yMax);
            for (var x = xMin; x <= xMax; x++)
            {
                for (var y = yMin; y <= yMax; y++)
                {
                    var tileIndex = y * worldSize + x;
                    var tileH = hMap.GetHeight(x, y);
                    var deltaH = referenceH - tileH;
                    if (deltaH >= 0)
                    {
                        var oldLevelDelta = levelDelta[tileIndex];
                        var oldSmoothDelta = smoothDelta[tileIndex];
                        var newLevelDelta = oldLevelDelta + oldSmoothDelta + deltaH;
                        var newSmoothDelta = 0f;
                        var roundedNewLevelDelta = RoundToTwoDecimals(tileH, oldLevelDelta + oldSmoothDelta, newLevelDelta + newSmoothDelta);
                        var limitedNewLevelDelta = Mathf.Clamp(roundedNewLevelDelta, -16.0f, 16.0f);
                        levelDelta[tileIndex] = limitedNewLevelDelta;
                        smoothDelta[tileIndex] = newSmoothDelta;
                        modifiedHeight[tileIndex] = true;
                        Debug.Log($"tilePos: ({x}, {y}), tileH: {tileH}, deltaH: {deltaH}, oldLevelDelta: {oldLevelDelta}, oldSmoothDelta: {oldSmoothDelta}, newLevelDelta: {newLevelDelta}, newSmoothDelta: {newSmoothDelta}, roundedNewLevelDelta: {roundedNewLevelDelta}, limitedNewLevelDelta: {limitedNewLevelDelta}");
                    }
                    else
                    {
                        Debug.Log("Declined to process tile: deltaH < 0!");
                        Debug.Log($"tilePos: ({x}, {y}), tileH: {tileH}, deltaH: {deltaH}");
                    }
                }
            }

            Debug.Log("[SUCCESS] Raise Terrain Modification");
        }

        public static void RecolorTerrain(
            Vector3 worldPos,
            TerrainModifier.PaintType paintType,
            Heightmap hMap,
            int worldWidth,
            ref Color[] paintMask,
            ref bool[] modifiedPaint
        )
        {
            Debug.Log("[INIT] Color Terrain Modification");
            worldPos -= new Vector3(0.5f, 0, 0.5f);
            hMap.WorldToVertex(worldPos, out var xPos, out var yPos);
            Debug.Log($"worldPos: {worldPos}, vertexPos: ({xPos}, {yPos})");

            var tileColor = ResolveColor(paintType);
            var removeColor = tileColor == Color.black;
            FindExtremums(xPos, worldWidth, out var xMin, out var xMax);
            FindExtremums(yPos, worldWidth, out var yMin, out var yMax);
            for (var x = xMin; x <= xMax; x++)
            {
                for (var y = yMin; y <= yMax; y++)
                {
                    var tileIndex = y * worldWidth + x;
                    paintMask[tileIndex] = tileColor;
                    modifiedPaint[tileIndex] = !removeColor;
                    Debug.Log($"tilePos: ({x}, {y}), tileIndex: {tileIndex}, tileColor: {tileColor}");
                }
            }
            Debug.Log("[SUCCESS] Color Terrain Modification");
        }

        public static void RemoveTerrainModifications(
            Vector3 worldPos,
            Heightmap hMap,
            int worldWidth,
            ref float[] levelDelta,
            ref float[] smoothDelta,
            ref bool[] modifiedHeight
        )
        {
            Debug.Log("[INIT] Remove Terrain Modifications");

            var worldSize = worldWidth + 1;
            hMap.WorldToVertex(worldPos, out var xPos, out var yPos);
            Debug.Log($"worldPos: {worldPos}, vertexPos: ({xPos}, {yPos})");

            FindExtremums(xPos, worldSize, out var xMin, out var xMax);
            FindExtremums(yPos, worldSize, out var yMin, out var yMax);
            for (var x = xMin; x <= xMax; x++)
            {
                for (var y = yMin; y <= yMax; y++)
                {
                    var tileIndex = y * worldSize + x;
                    levelDelta[tileIndex] = 0;
                    smoothDelta[tileIndex] = 0;
                    modifiedHeight[tileIndex] = false;
                    Debug.Log($"tilePos: ({x}, {y}), tileIndex: {tileIndex}");
                }
            }
            Debug.Log("[SUCCESS] Remove Terrain Modifications");
        }

        public static Color ResolveColor(TerrainModifier.PaintType paintType)
        {
            if (paintType == TerrainModifier.PaintType.Dirt) { return Color.red; }
            if (paintType == TerrainModifier.PaintType.Paved) { return Color.blue; }
            if (paintType == TerrainModifier.PaintType.Cultivate) { return Color.green; }
            return Color.black;
        }

        public static void FindExtremums(int x, int worldSize, out int xMin, out int xMax)
        {
            xMin = Mathf.Max(0, x - SizeInTiles);
            xMax = Mathf.Min(x + SizeInTiles, worldSize - 1);
        }

        public static float RoundToTwoDecimals(float oldH, float oldDeltaH, float newDeltaH)
        {
            var newH = oldH - oldDeltaH + newDeltaH;
            var roundedNewH = Mathf.Round(newH * 100) / 100;
            var roundedNewDeltaH = roundedNewH - oldH + oldDeltaH;
            Debug.Log($"oldH: {oldH}, oldDeltaH: {oldDeltaH}, newDeltaH: {newDeltaH}, newH: {newH}, roundedNewH: {roundedNewH}, roundedNewDeltaH: {roundedNewDeltaH}");
            return roundedNewDeltaH;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(TerrainComp.SmoothTerrain))]
        private static bool SmoothTerrianPrefix(
            Vector3 worldPos,
            float radius,
            bool square,
            float power,
            TerrainComp __instance,
            Heightmap ___m_hmap,
            int ___m_width,
            ref float[] ___m_smoothDelta,
            ref bool[] ___m_modifiedHeight
        )
        {
            if (IsGridModeEnabled(radius))
            {
                SmoothenTerrain(worldPos, ___m_hmap, __instance, ___m_width, ref ___m_smoothDelta, ref ___m_modifiedHeight);
                return false;
            }
            else
            {
                return true;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(TerrainComp.RaiseTerrain))]
        private static bool RaiseTerrainPrefix(
            Vector3 worldPos,
            float radius,
            float delta,
            bool square,
            float power,
            TerrainComp __instance,
            Heightmap ___m_hmap,
            int ___m_width,
            ref float[] ___m_levelDelta,
            ref float[] ___m_smoothDelta,
            ref bool[] ___m_modifiedHeight
        )
        {
            if (IsGridModeEnabled(radius))
            {
                RaiseTerrain(worldPos, ___m_hmap, __instance, ___m_width, delta, ref ___m_levelDelta, ref ___m_smoothDelta, ref ___m_modifiedHeight);
                return false;
            }
            else
            {
                return true;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(TerrainComp.PaintCleared))]
        private static bool PreciseColorModificaton(
            Vector3 worldPos,
            float radius,
            TerrainModifier.PaintType paintType,
            bool heightCheck,
            bool apply,
            Heightmap ___m_hmap,
            int ___m_width,
            ref Color[] ___m_paintMask,
            ref bool[] ___m_modifiedPaint
        )
        {
            if (IsGridModeEnabled(radius))
            {
                RecolorTerrain(worldPos, paintType, ___m_hmap, ___m_width, ref ___m_paintMask, ref ___m_modifiedPaint);
                return false;
            }
            else
            {
                return true;
            }
        }

        // DIRTY HACK: bend the flow to our will with a hijacked "unused" variable. Thus is the life of the modder ;)
        [HarmonyPrefix]
        [HarmonyPatch(nameof(TerrainComp.ApplyOperation))]
        private static bool ClientSideGridModeOverride(TerrainOp modifier)
        {
            if (Keybindings.GridModeEnabled)
            {
                if (modifier.m_settings.m_smooth)
                {
                    modifier.m_settings.m_smoothRadius = float.NegativeInfinity;
                }
                if (modifier.m_settings.m_raise && modifier.m_settings.m_raiseDelta >= 0)
                {
                    modifier.m_settings.m_raiseRadius = float.NegativeInfinity;
                    modifier.m_settings.m_raiseDelta = GroundLevelSpinner.Value;
                }
                if (modifier.m_settings.m_paintCleared)
                {
                    modifier.m_settings.m_paintRadius = float.NegativeInfinity;
                }
            }
            return true;
        }

        // DIRTY HACK: This is surely how I will be remembered ;)
        public static bool IsGridModeEnabled(float radius)
        {
            return radius == float.NegativeInfinity;
        }
    }
}