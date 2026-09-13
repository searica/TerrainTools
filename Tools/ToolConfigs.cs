using Jotunn.Configs;
using System;
using System.Collections.Generic;
using TerrainTools.Core;
using TerrainTools.Visualization;

namespace TerrainTools.Tools;

internal static class ToolConfigs
{
    internal static Dictionary<string, ToolDB> ToolConfigsMap = new()
    {
        // Hoe Tools
        {
            "mud_road_v2_sq",
            new ToolDB(
                name: "mud_road_v2_sq",
                basePrefab:"mud_road_v2",
                pieceName: "$atmc_level_square_name",
                pieceDesc: "$atmc_level_square_desc",
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
                pieceName: "$atmc_raise_precision_name",
                pieceDesc: "$atmc_raise_precision_desc",
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
                pieceName: "$atmc_lower_name",
                pieceDesc: "$atmc_lower_desc",
                icon: IconCache.Lower,
                pieceTable: Shovel.ShovelPieceTable,
                overlayType: null, // should make a lower ground visualizer
                raiseRadius: 1.5f,
                raisePower: 0.5f,
                raiseDelta: -0.5f,
                requirements: Array.Empty<Piece.Requirement>(),
                invertGhost: true
            )
        },

        {
            "path_v2_square",
            new ToolDB(
                name: "path_v2_square",
                basePrefab:"path_v2",
                pieceName: "$atmc_path_square_name",
                pieceDesc: "$atmc_path_square_desc",
                icon: IconCache.MudRoadPathSquare,
                pieceTable: PieceTables.Hoe,
                overlayType: typeof(SquarePathOverlayVisualizer),
                insertIndex: 3
            )
        },

        {
            "paved_road_v2_square",
            new ToolDB(
                name: "paved_road_v2_square",
                basePrefab: "paved_road_v2",
                pieceName: "$atmc_paved_square_name",
                pieceDesc: "$atmc_paved_square_desc",
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
                pieceName: "$atmc_paved_path_name",
                pieceDesc: "$atmc_paved_path_desc",
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
                pieceName: "$atmc_paved_path_square_name",
                pieceDesc: "$atmc_paved_path_square_desc",
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
                pieceName: "$atmc_remove_name",
                pieceDesc: "$atmc_remove_desc",
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
                pieceName: "$atmc_cultivate_square_name",
                pieceDesc: "$atmc_cultivate_square_desc",
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
                pieceName: "$atmc_cultivate_path_name",
                pieceDesc: "$atmc_cultivate_path_desc",
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
                pieceName: "$atmc_cultivate_path_square_name",
                pieceDesc: "$atmc_cultivate_path_square_desc",
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
                pieceName: "$atmc_replant_square_name",
                pieceDesc: "$atmc_replant_square_desc",
                icon: IconCache.ReplantSquare,
                pieceTable: PieceTables.Cultivator,
                overlayType: typeof(SeedGrassOverlayVisualizer),
                insertIndex: 2
            )
        },
    };
}
