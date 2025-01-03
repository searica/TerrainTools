using System;
using UnityEngine;
using Logging;
using TerrainTools.Patches;

namespace TerrainTools.Extensions;

internal static class TerrainCompExtensions
{
    public const int FixedRadius = 1;
    public const float RoundOff = 0.05f;
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
        Log.LogInfo("[INIT] Remove Terrain Modifications", Log.InfoLevel.Medium);

        int fixedRadius = comp.GetFixedRadius();
        int nVertsInGrid = comp.m_width + 1;
        FindSquareBounds(comp, worldPos, fixedRadius, out SquareBounds xBounds, out SquareBounds zBounds);
        Log.LogInfo($"worldPos: {worldPos}, Bounds (X,Z): Min=({xBounds.min}, {zBounds.min}), Max=({xBounds.max}, {zBounds.max})", Log.InfoLevel.Medium);

        for (int i = xBounds.min; i <= xBounds.max; i++)
        {
            for (int j = zBounds.min; j <= zBounds.max; j++)
            {
                int vertIndex = (j * nVertsInGrid) + i;
                comp.m_levelDelta[vertIndex] = 0;
                comp.m_smoothDelta[vertIndex] = 0;
                comp.m_modifiedHeight[vertIndex] = false;
                Log.LogInfo($"Vertex: ({i}, {j}), Index: {vertIndex}", Log.InfoLevel.Medium);
            }
        }
        Log.LogInfo("[SUCCESS] Remove Terrain Modifications", Log.InfoLevel.Medium);
    }

    public static void PreciseRaiseTerrain(this TerrainComp comp, Vector3 worldPos, float delta)
    {
        Log.LogInfo("[INIT] Raise Terrain Modification", Log.InfoLevel.Medium);

        int fixedRadius = comp.GetFixedRadius();
        int nVertsInGrid = comp.m_width + 1;
        FindSquareBounds(comp, worldPos, fixedRadius, out SquareBounds xBounds, out SquareBounds zBounds);
        Log.LogInfo($"worldPos: {worldPos}, Bounds (X,Z): Min=({xBounds.min}, {zBounds.min}), Max=({xBounds.max}, {zBounds.max})", Log.InfoLevel.Medium);

        float refHeight = worldPos.y - comp.transform.position.y;
        Log.LogInfo($"worldPos: {worldPos}, delta: {delta}, refHeight: {refHeight}", Log.InfoLevel.Medium);

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
        Log.LogInfo("[SUCCESS] Raise Terrain Modification", Log.InfoLevel.Medium);
    }

    public static void PreciseSmoothTerrain(this TerrainComp comp, Vector3 worldPos)
    {
        Log.LogInfo("PreciseSmoothTerrain", Log.InfoLevel.Medium);

        int fixedRadius = comp.GetFixedRadius();
        int nVertsInGrid = comp.m_width + 1;
        FindSquareBounds(comp, worldPos, fixedRadius, out SquareBounds xBounds, out SquareBounds zBounds);
        float refHeight = worldPos.y - comp.transform.position.y;
        Log.LogInfo($"worldPos: {worldPos}, Bounds (X,Z): Min=({xBounds.min}, {zBounds.min}), Max=({xBounds.max}, {zBounds.max})", Log.InfoLevel.Medium);

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
                Log.LogInfo($"Vertex: ({i}, {j}), Index: {vertexIndex}, tileH: {tileHeight}, deltaH: {deltaH}, prevSmoothDelta: {prevSmoothDelta}, newSmoothDelta {comp.m_smoothDelta[vertexIndex]}", Log.InfoLevel.Medium);
            }
        }
        Log.LogInfo("[SUCCESS] Smooth Terrain Modification", Log.InfoLevel.Medium);
    }

    public static void PreciseRecolorTerrain(
        this TerrainComp comp,
        Vector3 worldPos,
        TerrainModifier.PaintType paintType
    )
    {
        // TODO: make the distance differences relative to actual worldPos
        // TODO confirm modifying via ref worked
        Log.LogInfo("[INIT] PreciseRecolorTerrain", Log.InfoLevel.Medium);
        comp.m_hmap.WorldToVertexMask(worldPos, out int xCenterIdx, out int zCenterIdx);

        float pixelWidth = comp.m_hmap.GetPaintMaskScale();
        float pixelArea = pixelWidth * pixelWidth;
        int pixelDelta = Mathf.CeilToInt((float)FixedRadius / pixelWidth);
        int iMin = Math.Max(xCenterIdx - pixelDelta, 0);
        int iMax = Math.Min(xCenterIdx + pixelDelta, comp.m_width);
        int jMin = Math.Max(zCenterIdx - pixelDelta, 0);
        int jMax = Math.Min(zCenterIdx + pixelDelta, comp.m_width);

        Color newColor = ResolveColor(paintType);
        bool resetColor = paintType == TerrainModifier.PaintType.Reset;
        int nVertsInGrid = comp.m_width + 1;

        //Log.LogInfo($"WorldPos: {worldPos}");
        for (int i = iMin; i <= iMax; i++)
        {
            for (int j = jMin; j <= jMax; j++)
            {
                Color currentColor = comp.m_hmap.GetPaintMask(i, j);
                int vertexIndex = (j * nVertsInGrid) + i;

                // Distance to outer edge of pixel
                Vector3 vertexPos = comp.m_hmap.PaintMaskVertexToWorldPos(i, j);
                float xOutside = Mathf.Max(Mathf.Abs(vertexPos.x - worldPos.x) + (0.5f * pixelWidth) - FixedRadius, 0f);
                float zOutside = Mathf.Max(Mathf.Abs(vertexPos.z - worldPos.z) + (0.5f * pixelWidth) - FixedRadius, 0f);
                //Log.LogInfo($"VertexPos: {vertexPos}");
                //Log.LogInfo($"Distance Outside, X={xOutside}, Z={zOutside}");

                if (xOutside > pixelWidth || zOutside > pixelWidth)
                {
                    continue; // out of bounds.
                }
                // blend color based on how much of the pixel is within the fixed square
                float areaOutside = pixelWidth*(xOutside + zOutside) - (xOutside * zOutside);
                float percentageInside = (pixelArea - areaOutside) / pixelArea;
                Log.LogInfo($"Percentage Inside: {percentageInside}");
                comp.m_paintMask[vertexIndex] = BlendColor(currentColor, newColor, percentageInside);
               
                comp.m_modifiedPaint[vertexIndex] = !resetColor;
                Log.LogInfo($"Vertex: ({i}, {j}), Index: {vertexIndex}, Color: {comp.m_paintMask[vertexIndex]}", Log.InfoLevel.Medium);
            }
        }
        Log.LogInfo("[SUCCESS] Color Terrain Modification", Log.InfoLevel.Medium);
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
    ///     to form a square grid for manipulating the terrain height nodes. Do not use
    ///     this for paint mask pixels.
    /// </summary>
    /// <param name="comp"></param>
    /// <param name="worldPos">Position to define the bounds around.</param>
    /// <param name="radius">Radius used to determine the bounds.</param>
    /// <param name="xBounds">Vertex min and max ID along Unity X-axis</param>
    /// <param name="zBounds">Vertex min and max ID along Unity Z-axis</param>
    private static void FindSquareBounds(
        this TerrainComp comp,
        Vector3 worldPos,
        int radius,
        out SquareBounds xBounds,
        out SquareBounds zBounds
    )
    {
        // Get 2D index numbering of the nearest vertex within the zone 
        comp.m_hmap.WorldToVertex(worldPos, out int vertIdX, out int vertIdY);

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


    /// <summary>
    ///     Interpolate color while clamping the result to prevent bleeds on repeat color changes.
    ///     Also directly copies the alpha value without Lerp. The approach used here only works because
    ///     each paint type in Valheim is a single primary color (R, G, B).
    ///     
    ///     Some color channels seem to take priority though which causes issues
    /// </summary>
    /// <param name="currentColor"></param>
    /// <param name="newColor"></param>
    /// <param name="t"></param>
    /// <returns></returns>
    private static Color BlendColor(Color currentColor, Color newColor, float t)
    {
        // Raise current colors to 1's if they are greater than 0.05 and
        // then lerp them with the new color based on percentage

        return new(
            LerpToUpperOrLowerRange(currentColor.r, newColor.r, t),
            LerpToUpperOrLowerRange(currentColor.g, newColor.g, t),
            LerpToUpperOrLowerRange(currentColor.b, newColor.b, t),
            currentColor.a
        );
    }

    /// <summary>
    ///     Used to clamps Lerp changes to color for repeated changes.
    /// </summary>
    /// <param name="currentVal"></param>
    /// <param name="newVal"></param>
    /// <param name="t"></param>
    /// <returns>Clamp new value to range (1-t, 1) if it is lower than currentVal. Clamp new value to range (0, t) if it is higher than currentVal.</returns>
    private static float LerpToUpperOrLowerRange(float currentVal, float newVal, float t) 
    {
        float blendVal = Mathf.Lerp(currentVal, newVal, t);
        if (currentVal > newVal)
        {
            blendVal = Mathf.Clamp(blendVal, Mathf.Min(1f - t, 0.5f), 1f);
        }
        else if (currentVal < newVal)
        {
            blendVal = Mathf.Clamp(blendVal, 0f, t);
        }
        return blendVal;

        //return tmpVal.Equals(1f, eps: RoundOff) ? 1f : tmpVal;
        //return tmpVal.Equals(0f, eps: RoundOff) ? 0f: tmpVal;
    }
}
