using Logging;
using System.Collections.Generic;
using UnityEngine;

namespace TerrainTools.Core;

internal static class TerrainOperations
{
    public class HeightIndex
    {
        public int Index;
        public Vector3 Position;
        public float DistanceWidth;
        public float DistanceDepth;
        public float Distance;
    }

    public class PaintIndex
    {
        public int Index;
        public Vector3 Position;
    }

    public class Indices
    {
        public HeightIndex[] HeightIndices = new HeightIndex[0];
        public PaintIndex[] PaintIndices = new PaintIndex[0];
    }

    public const int FixedRadius = 1;
    public const float RoundOff = 0.05f;
    
    private static float GetX(float x, float y, float angle) => Mathf.Cos(angle) * x - Mathf.Sin(angle) * y;
    private static float GetY(float x, float y, float angle) => Mathf.Sin(angle) * x + Mathf.Cos(angle) * y;

    /// <summary>
    ///     Get fixed radius. Do not scale because this is already in
    ///     units of vertex numbering.
    /// </summary>
    /// <param name="comp"></param>
    /// <returns></returns>
    public static int GetFixedRadius(this TerrainComp comp)
    {
        return FixedRadius;
    }

    /// <summary>
    ///     Get fixed radius. Do not scale because this is already in
    ///     units of vertex numbering.
    /// </summary>
    /// <param name="terrainOp"></param>
    /// <returns></returns>
    public static int GetFixedRadius(this TerrainOp terrainOp)
    {
        return FixedRadius;
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
    /// <param name="settings"></param>
    /// <returns></returns>
    public static bool IsPrecisionModifier(this TerrainOp.Settings settings)
    {
        if (settings.m_raise && IsPrecisionModifier(settings.m_raiseRadius)) 
        {
            return true; // precise raise tool
        }
        else if (settings.m_smooth && IsPrecisionModifier(settings.m_smoothRadius)) 
        {
            return true; // precise smooth tool
        }
        else if (settings.m_paintCleared && IsPrecisionModifier(settings.m_paintRadius)) 
        {
            return true; // precise paint tool
        }
        else if (settings.m_level && IsPrecisionModifier(settings.m_levelRadius))
        {
            return true; // precise level tool
        }
        else if (!settings.m_raise && !settings.m_smooth && !settings.m_paintCleared && !settings.m_level) 
        {
            return true; // precise remove modifications tool
        }
        return false;
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
        Log.LogInfo("Remove Terrain Modifications", Log.InfoLevel.Medium);

        int fixedRadius = comp.GetFixedRadius();
        IEnumerable<HeightIndex> indices = GetHeightIndicesWithRect(comp, worldPos, fixedRadius, fixedRadius, 0);
        foreach (HeightIndex heightIndex in indices)
        {
            comp.m_levelDelta[heightIndex.Index] = 0;
            comp.m_smoothDelta[heightIndex.Index] = 0;
            comp.m_modifiedHeight[heightIndex.Index] = false;
        }
    }

    public static void PreciseRaiseTerrain(this TerrainComp comp, Vector3 worldPos, float delta)
    {
        Log.LogInfo("Precise Raise Terrain", Log.InfoLevel.Medium);

        int squareLength = 2 * comp.GetFixedRadius();
        float refHeight = worldPos.y - comp.transform.position.y;
        IEnumerable<HeightIndex> indices = GetHeightIndicesWithRect(comp, worldPos, squareLength, squareLength, 0);

        float tileHeight;
        float targetHeight;
        float deltaH;
        foreach (HeightIndex heightIndex in indices)
        {
            tileHeight = comp.m_hmap.m_heights[heightIndex.Index];
            targetHeight = refHeight + delta;

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
      
            deltaH = targetHeight - tileHeight + comp.m_smoothDelta[heightIndex.Index];
            comp.m_smoothDelta[heightIndex.Index] = 0f;
            comp.m_levelDelta[heightIndex.Index] += deltaH;
            comp.m_levelDelta[heightIndex.Index] = Mathf.Clamp(comp.m_levelDelta[heightIndex.Index], -8f, 8f);
            comp.m_modifiedHeight[heightIndex.Index] = comp.m_levelDelta[heightIndex.Index] != 0f; 
        }
    }

    public static void PreciseSmoothTerrain(this TerrainComp comp, Vector3 worldPos)
    {
        Log.LogInfo("Precise Smooth Terrain", Log.InfoLevel.Medium);

        int squareLength = 2 * comp.GetFixedRadius();
        float refHeight = worldPos.y - comp.transform.position.y;
        IEnumerable<HeightIndex> indices = GetHeightIndicesWithRect(comp, worldPos, squareLength, squareLength, 0);

        float tileHeight;
        float deltaH;
        float prevSmoothDelta;
        foreach (HeightIndex heightIndex in indices)
        {
            tileHeight = comp.m_hmap.m_heights[heightIndex.Index];
            deltaH = refHeight - tileHeight;
            prevSmoothDelta = comp.m_smoothDelta[heightIndex.Index];
            comp.m_smoothDelta[heightIndex.Index] = Mathf.Clamp(prevSmoothDelta + deltaH, -1f, 1f);
            comp.m_modifiedHeight[heightIndex.Index] = comp.m_levelDelta[heightIndex.Index] != 0f;
        }
    }

    public static void PreciseRecolorTerrain(
        this TerrainComp comp,
        Vector3 worldPos,
        TerrainModifier.PaintType paintType
    )
    {
        Log.LogInfo("Precise Recolor Terrain", Log.InfoLevel.Medium);
        int squareLength = 2 * comp.GetFixedRadius();
        IEnumerable<PaintIndex> indices = GetPaintIndicesWithRect(comp, worldPos, squareLength, squareLength, 0);

        Color targetColor = ResolveColor(paintType);
        bool resetColor = paintType == TerrainModifier.PaintType.Reset;

        // Lava is implemented with alpha 1, which unfortunately is same alpha as on regular terrain.
        // So always copy the existing alpha value before modifying it to preserve whatever state the terrain should be in

        foreach (PaintIndex index in indices)
        {
            Color newColor = targetColor;    
            newColor.a  = comp.m_paintMask[index.Index].a;
            comp.m_paintMask[index.Index] = newColor;
            comp.m_modifiedPaint[index.Index] = !resetColor;
        }
    }

    public static Color ResolveColor(TerrainModifier.PaintType paintType)
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
    

    private static IEnumerable<HeightIndex> GetHeightIndicesWithRect(
        TerrainComp compiler, Vector3 centerPos, float width, float depth, float angle
    )
    {
        List<HeightIndex> indices = new List<HeightIndex>();
        float maxWidth = width / 2f;
        float maxDepth = depth / 2f;
        int max = compiler.m_width + 1;

        Vector3 position;
        float dx;
        float dy;
        float distanceX;
        float distanceY;
        for (int x = 0; x < max; x++)
        {
            for (int y = 0; y < max; y++)
            {
                position = VertexToWorld(compiler.m_hmap, x, y);
                dx = position.x - centerPos.x;
                dy = position.z - centerPos.z;
                distanceX = GetX(dx, dy, angle);
                distanceY = GetY(dx, dy, angle);
                if (Mathf.Abs(distanceX) > maxWidth) continue;
                if (Mathf.Abs(distanceY) > maxDepth) continue;
                // Distance to the nearest edge relative to the shorter half side, so smoothing is
                // equally wide on all sides of a rectangle and unchanged for a square
                float edgeDistance = Mathf.Min(maxWidth - Mathf.Abs(distanceX), maxDepth - Mathf.Abs(distanceY));
                indices.Add(new HeightIndex()
                {
                    Index = y * max + x,
                    Position = position,
                    DistanceWidth = distanceX / maxWidth,
                    DistanceDepth = distanceY / maxDepth,
                    Distance = 1f - edgeDistance / Mathf.Min(maxWidth, maxDepth)
                });
            }
        }

        return indices;
    }

    //private static IEnumerable<PaintIndex> GetPaintIndicesWithRect(
    //    TerrainComp compiler, Vector3 centerPos, float width, float depth, float angle
    //)
    //{
    //    List<PaintIndex> indices = new List<PaintIndex>();
    //    compiler.m_hmap.WorldToVertexMask(centerPos, out var cx, out var cy);
    //    float maxWidth = width / 2f / compiler.m_hmap.m_scale;
    //    float maxDepth = depth / 2f / compiler.m_hmap.m_scale;
    //    int max = compiler.m_width + 1;
    //    for (int x = 0; x < max; x++)
    //    {
    //        for (int y = 0; y < max; y++)
    //        {
    //            float dx = x - cx;
    //            float dy = y - cy;
    //            float distanceX = GetX(dx, dy, angle);
    //            float distanceY = GetY(dx, dy, angle);
    //            if (Mathf.Abs(distanceX) > maxWidth) continue;
    //            if (Mathf.Abs(distanceY) > maxDepth) continue;
    //            indices.Add(new PaintIndex()
    //            {
    //                Index = y * max + x,
    //                Position = VertexToWorld(compiler.m_hmap, x, y)
    //            });
    //        }
    //    }

    //    return indices;
    //}

    private static IEnumerable<PaintIndex> GetPaintIndicesWithRect(
        TerrainComp compiler, Vector3 centerPos, float width, float depth, float angle
    )
    {
        List<PaintIndex> indices = new List<PaintIndex>();
        float maxWidth = width / 2f / compiler.m_hmap.m_scale;
        float maxDepth = depth / 2f / compiler.m_hmap.m_scale;
        int max = compiler.m_width + 1;

        Vector3 nodePos;
        float dx;
        float dy;
        float distanceX;
        float distanceY;
        for (int x = 0; x < max; x++)
        {
            for (int y = 0; y < max; y++)
            {
                nodePos = VertexToWorld(compiler.m_hmap, x, y);
                dx = nodePos.x - centerPos.x;
                dy = nodePos.z - centerPos.z;
                distanceX = GetX(dx, dy, angle);
                distanceY = GetY(dx, dy, angle);
                if (Mathf.Abs(distanceX) > maxWidth) continue;
                if (Mathf.Abs(distanceY) > maxDepth) continue;
                indices.Add(new PaintIndex()
                {
                    Index = y * max + x,
                    Position = nodePos
                });
            }
        }

        return indices;
    }

    // Height vertices sit on whole grid positions, see Heightmap.WorldToVertex
    private static Vector3 VertexToWorld(Heightmap hmap, int x, int y)
    {
        Vector3 vector = hmap.transform.position;
        vector.x += (x - hmap.m_width / 2) * hmap.m_scale;
        vector.z += (y - hmap.m_width / 2) * hmap.m_scale;
        return vector;
    }
}
