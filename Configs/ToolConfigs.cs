using System.Collections.Generic;
using Jotunn.Configs;
using TerrainTools.Visualization;

namespace TerrainTools.Configs
{
    internal class ToolConfigs
    {
        internal static Dictionary<string, ToolDB> ToolConfigsMap = new()
        {
            // Hoe Tools
            {
                "mud_road_v2_sq",
                new ToolDB(
                    name: "mud_road_v2_sq",
                    basePrefab:"mud_road_v2",
                    pieceName: "Level ground(square)",
                    pieceDesc: "Levels ground according to the world grid based on player position. Use shift+click to level ground based on where you are pointing instead (this will smooth the terrain).",
                    icon: IconCache.MudRoadSquare,
                    pieceTable: PieceTables.Hoe,
                    overlayType: typeof(LevelGroundOverlayVisualizer),
                    insertIndex: 1
                )
            },

            {
                "raise_v2_precise",
                new ToolDB(
                    name: "raise_v2_precise",
                    basePrefab:"raise_v2",
                    pieceName: "Raise ground (precision)",
                    pieceDesc: "Raise ground with precision accuracy by using scroll wheel to set ground height.",
                    icon: IconCache.RaiseSquare,
                    pieceTable: PieceTables.Hoe,
                    overlayType: typeof(RaiseGroundOverlayVisualizer),
                    insertIndex: 2
                )
            },

            {
                "lower_v2",
                new ToolDB(
                    name: "lower_v2",
                    basePrefab:"raise_v2",
                    pieceName: "Lower ground",
                    pieceDesc: "Lowers ground.",
                    icon: IconCache.RaiseSquare,
                    pieceTable: PieceTables.Hoe,
                    overlayType: null, // should make a lower ground visualizer
                    insertIndex: 2,
                    raiseRadius: 1.5f,
                    raisePower: 0.5f,
                    raiseDelta: -0.5f
                    requirements: Array.Empty<Piece.Requirement>(),
                )
            },

            {
                "path_v2_square",
                new ToolDB(
                    name: "path_v2_square",
                    basePrefab:"path_v2",
                    pieceName: "Pathen (square)",
                    pieceDesc: "Creates a dirt path according to the world grid without affecting ground height.",
                    icon: IconCache.MudRoadPathSquare,
                    pieceTable: PieceTables.Hoe,
                    overlayType: typeof(SquarePathOverlayVisualizer),
                    insertIndex: 3
                )
            },

            {
    "paved_road_v2_square",
                new ToolDB(
                    name: "Paved road (Square)",
                    basePrefab: "paved_road_v2",
                    pieceName: "Paved road (square)",
                    pieceDesc: "Creates a paved path according to the world grid and levels ground based on player position. Use shift+click to level ground based on where you are pointing (this will smooth the terrain).",
                    icon: IconCache.PavedRoadSquare,
                    pieceTable: PieceTables.Hoe,
                    overlayType: typeof(SquarePathOverlayVisualizer)
                )
            },
            {
    "paved_road_v2_path",
                new ToolDB(
                    name: "paved_road_v2_path",
                    basePrefab: "paved_road_v2",
                    pieceName: "Paved road (path)",
                    pieceDesc: "Creates a paved path without affecting ground height",
                    icon: IconCache.PavedRoadPath,
                    pieceTable: PieceTables.Hoe,
                    smooth: false
                )
            },
            {
    "paved_road_v2_path_square",
                new ToolDB(
                    name: "paved_road_v2_path_square",
                    basePrefab: "paved_road_v2",
                    pieceName: "Paved road (path, square)",
                    pieceDesc: "Created a paved path according to the world grid without affecting ground height",
                    icon: IconCache.PavedRoadPathSquare,
                    pieceTable: PieceTables.Hoe,
                    overlayType: typeof(SquarePathOverlayVisualizer),
                    smooth: false
                )
            },
            {
    "remove_terrain_mods",
                new ToolDB(
                    name: "remove_terrain_mods",
                    basePrefab: "mud_road_v2",
                    pieceName: "Remove Terrain Modifications",
                    pieceDesc: "Resets ground height and paint",
                    icon: IconCache.Remove,
                    pieceTable: PieceTables.Hoe,
                    overlayType: typeof(RemoveModificationsOverlayVisualizer),
                    smooth: false,
                    level: false,
                    raise: false,
                    clearPaint: false
                )
            },

            // Cultivator Tools
            {
    "cultivate_v2_square",
                new ToolDB(
                    name: "cultivate_v2_square",
                    basePrefab: "cultivate_v2",
                    pieceName: "Cultivate (square)",
                    pieceDesc: "Cultivates ground according to the world grid and levels terrain based on player position. Use shift+click to level ground based on where you are pointing (this will smooth the terrain).",
                    icon: IconCache.CultivateSquare,
                    pieceTable: PieceTables.Cultivator,
                    overlayType: typeof(CultivateOverlayVisualizer),
                    insertIndex: 1
                )
            },
            {
    "cultivate_v2_path",
                new ToolDB(
                    name: "cultivate_v2_path",
                    basePrefab: "cultivate_v2",
                    pieceName: "Cultivate (path)",
                    pieceDesc: "Cultivates ground without affecting ground height.",
                    icon: IconCache.CultivatePath,
                    pieceTable: PieceTables.Cultivator,
                    smooth: false,
                    insertIndex: 1
                )
            },
            {
    "cultivate_v2_path_square",
                new ToolDB(
                    name: "cultivate_v2_path_square",
                    basePrefab: "cultivate_v2",
                    pieceName: "Cultivate (path, square)",
                    pieceDesc: "Cultivates ground according to the world grid without affecting ground height.",
                    icon: IconCache.CultivatePathSquare,
                    pieceTable: PieceTables.Cultivator,
                    overlayType: typeof(CultivateOverlayVisualizer),
                    smooth: false,
                    insertIndex: 1
                )
            },
            {
    "replant_v2_square",
                new ToolDB(
                    name: "replant_v2_square",
                    basePrefab: "replant_v2",
                    pieceName: "Replant (square)",
                    pieceDesc: "Replants terrain according to world grid without affecting ground height.",
                    icon: IconCache.ReplantSquare,
                    pieceTable: PieceTables.Cultivator,
                    overlayType: typeof(SeedGrassOverlayVisualizer),
                    insertIndex: 2
                )
            },
        };
    }
}