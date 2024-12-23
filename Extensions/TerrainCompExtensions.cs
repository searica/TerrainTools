using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static ClutterSystem;
using static TerrainModifier;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

namespace TerrainTools.Extensions;

internal static class TerrainCompExtensions
{
    public const int FixedRadius = 1;
    internal struct SquareBounds
    {
        public int min;
        public int max;
        public SquareBounds(int min, int max)
        {
            this.min = min;
            this.max = max;
        }
    }

    /// <summary>
    ///     Get fixed radius scaled by comp.m_h_map.m_scale
    /// </summary>
    /// <param name="comp"></param>
    /// <returns></returns>
    public static int GetScaledFixedRadius(this TerrainComp comp)
    {
        return Mathf.CeilToInt(FixedRadius / comp.m_hmap.m_scale);
    }

    /// <summary>
    ///     Checks if radius is set as flag for precision modifier.
    /// </summary>
    /// <param name="radius"></param>
    /// <returns></returns>
    public static bool IsPrecisionModifier(this TerrainComp terrainComp)
    {
        return IsPrecisionModifier(terrainComp.m_lastOpRadius);
    }

    /// <summary>
    ///     Checks if radius is set as flag for precision modifier.
    /// </summary>
    /// <param name="radius"></param>
    /// <returns></returns>
    public static bool IsPrecisionModifier(float radius)
    {
        return radius == float.NegativeInfinity;
    }

    public static void RemoveTerrainModifications(this TerrainComp comp, Vector3 worldPos)
    {
        Log.LogInfo("[INIT] Remove Terrain Modifications", LogLevel.Medium);

        int fixedRadius = comp.GetScaledFixedRadius();
        int nVertsInGrid = comp.m_width + 1;
        FindSquareBounds(comp, worldPos, fixedRadius, out SquareBounds xBounds, out SquareBounds zBounds, offset: false);
        Log.LogInfo($"worldPos: {worldPos}, Bounds (X,Z): Min=({xBounds.min}, {zBounds.min}), Max=({xBounds.max}, {zBounds.max})", LogLevel.Medium);

        for (int i = xBounds.min; i <= xBounds.max; i++)
        {
            for (int j = zBounds.min; j <= zBounds.max; j++)
            {
                int vertIndex = (j * nVertsInGrid) + i;
                comp.m_levelDelta[vertIndex] = 0;
                comp.m_smoothDelta[vertIndex] = 0;
                comp.m_modifiedHeight[vertIndex] = false;
                Log.LogInfo($"Vertex: ({i}, {j}), Index: {vertIndex}", LogLevel.Medium);
            }
        }
        Log.LogInfo("[SUCCESS] Remove Terrain Modifications", LogLevel.Medium);
    }

    public static void PreciseRaiseTerrain(this TerrainComp comp, Vector3 worldPos, float delta)
    {
        Log.LogInfo("[INIT] Raise Terrain Modification", LogLevel.Medium);

        int fixedRadius = comp.GetScaledFixedRadius();
        int nVertsInGrid = comp.m_width + 1;
        FindSquareBounds(comp, worldPos, fixedRadius, out SquareBounds xBounds, out SquareBounds zBounds, offset: false);
        Log.LogInfo($"worldPos: {worldPos}, Bounds (X,Z): Min=({xBounds.min}, {zBounds.min}), Max=({xBounds.max}, {zBounds.max})", LogLevel.Medium);

        float refHeight = worldPos.y - comp.transform.position.y;
        Log.LogInfo($"worldPos: {worldPos}, delta: {delta}, refHeight: {refHeight}", LogLevel.Medium);

        for (int i = xBounds.min; i <= xBounds.max; i++)
        {
            for (int j = zBounds.min; j <= zBounds.max; j++)
            {
                float tileHeight = comp.m_hmap.GetHeight(i, j);
                float targetHeight = refHeight + delta;

                if (delta < 0f && targetHeight > tileHeight)
                {
                    continue;  // can't lower terrain to higher position
                }

                if (delta >= 0f)
                {
                    if (targetHeight < tileHeight)
                    {
                        continue; // can't raise raise to a lower position
                    }
                    if (targetHeight > tileHeight + delta)
                    {
                        targetHeight = tileHeight + delta;  // make raise to uniform height
                    }
                }

                int vertexIndex = (j * nVertsInGrid) + i;
                float deltaH = targetHeight - tileHeight + comp.m_smoothDelta[vertexIndex];
                comp.m_smoothDelta[vertexIndex] = 0f;
                comp.m_levelDelta[vertexIndex] += deltaH;
                comp.m_levelDelta[vertexIndex] = Mathf.Clamp(comp.m_levelDelta[vertexIndex], -8f, 8f);
                comp.m_modifiedHeight[vertexIndex] = true;
            }
        }
        Log.LogInfo("[SUCCESS] Raise Terrain Modification", LogLevel.Medium);
    }


    public static void PreciseSmoothTerrain(this TerrainComp comp, Vector3 worldPos)
    {
        Log.LogInfo("PreciseSmoothTerrain", LogLevel.Medium);

        int fixedRadius = comp.GetScaledFixedRadius();
        int nVertsInGrid = comp.m_width + 1;
        FindSquareBounds(comp, worldPos, fixedRadius, out SquareBounds xBounds, out SquareBounds zBounds, offset: false);
        float refHeight = worldPos.y - comp.transform.position.y;
        Log.LogInfo($"worldPos: {worldPos}, Bounds (X,Z): Min=({xBounds.min}, {zBounds.min}), Max=({xBounds.max}, {zBounds.max})", LogLevel.Medium);

        for (int i = xBounds.min; i <= xBounds.max; i++)
        {
            for (int j = zBounds.min; j <= zBounds.max; j++)
            {
                int vertexIndex = (j * nVertsInGrid) + i;
                float tileHeight = comp.m_hmap.GetHeight(i, j);
                float deltaH = refHeight - tileHeight;
                float prevSmoothDelta = comp.m_smoothDelta[vertexIndex];
                comp.m_smoothDelta[vertexIndex] = Mathf.Clamp(prevSmoothDelta + deltaH, -1f, 1f);
                comp.m_modifiedHeight[vertexIndex] = true;
                Log.LogInfo($"Vertex: ({i}, {j}), Index: {vertexIndex}, tileH: {tileHeight}, deltaH: {deltaH}, prevSmoothDelta: {prevSmoothDelta}, newSmoothDelta {comp.m_smoothDelta[vertexIndex]}", LogLevel.Medium);
            }
        }
        Log.LogInfo("[SUCCESS] Smooth Terrain Modification", LogLevel.Medium);
    }

    public static void PreciseRecolorTerrain(
        this TerrainComp comp,
        Vector3 worldPos,
        TerrainModifier.PaintType paintType,
        bool heightCheck = false
    )
    {
        Log.LogInfo("[INIT] PreciseRecolorTerrain", LogLevel.Medium);
        int radius = Mathf.CeilToInt(FixedRadius / comp.m_hmap.m_scale);
        int nVertsInGrid = comp.m_width + 1;
        FindSquareBounds(comp, worldPos, radius, out SquareBounds xBounds, out SquareBounds zBounds, offset: true);

        Color vtxColor = ResolveColor(paintType);
        bool resetColor = paintType == TerrainModifier.PaintType.Reset;

        for (int i = xBounds.min; i <= xBounds.max; i++)
        {
            for (int j = zBounds.min; j <= zBounds.max; j++)
            {
                vtxColor.a = comp.m_hmap.GetPaintMask(i, j).a;  // avoids lava
                int vertexIndex = (j * nVertsInGrid) + i;
                comp.m_paintMask[vertexIndex] = vtxColor;
                comp.m_modifiedPaint[vertexIndex] = !resetColor;
                Log.LogInfo($"Vertex: ({i}, {j}), Index: {vertexIndex}, Color: {vtxColor}", LogLevel.Medium);
            }
        }
        Log.LogInfo("[SUCCESS] Color Terrain Modification", LogLevel.Medium);
    }

    public static UnityEngine.Color ResolveColor(TerrainModifier.PaintType paintType)
    {
        switch (paintType)
        {
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



    /// <summary>
    ///     Get Min and Max vertex IDs for x and z directions (Unity CSYS) based on radius 
    ///     to form a square grid.
    /// </summary>
    /// <param name="comp"></param>
    /// <param name="worldPos">Position to define the bounds around.</param>
    /// <param name="radius">Radius used to determine the bounds.</param>
    /// <param name="xBounds">Vertex min and max ID along Unity X-axis</param>
    /// <param name="zBounds">Vertex min and max ID along Unity Z-axis</param>
    /// <param name="offset">Whether to apply a -0.5 offset to the worldPos before finding the nearest vertex.</param>
    private static void FindSquareBounds(
        this TerrainComp comp,
        Vector3 worldPos,
        int radius,
        out SquareBounds xBounds,
        out SquareBounds zBounds,
        bool offset = false
    )
    {
        if (offset)
        {
            worldPos = new(worldPos.x - 0.5f, worldPos.y, worldPos.z - 0.5f);
        }

        // Get 2D index numbering of the nearest vertex within the zone 
        comp.m_hmap.WorldToVertexMask(worldPos, out int vertIdX, out int vertIdY);

        // m_width is the number of tiles so number of vertexes is m_width + 1
        xBounds = new SquareBounds(
            Mathf.Max(0, vertIdX - radius),
            Mathf.Min(vertIdX + radius, comp.m_width)
        );
        zBounds = new SquareBounds(
            Mathf.Max(0, vertIdY - radius),
            Mathf.Min(vertIdY + radius, comp.m_width)
        );
    }
}
