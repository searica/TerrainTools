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
                pieceName: "$mud_road_v2_sq_name",
                pieceDesc: "$mud_road_v2_sq_desc",
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
                pieceName: "$raise_v2_precise_name",
                pieceDesc: "$raise_v2_precise_desc",
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
                pieceName: "$lower_v2_name",
                pieceDesc: "$lower_v2_desc",
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
                pieceName: "$path_v2_square_name",
                pieceDesc: "$path_v2_square_desc",
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
                pieceName: "$paved_road_v2_square_name",
                pieceDesc: "$paved_road_v2_square_desc",
                icon: IconCache.PavedRoadSquare,
                pieceTable: PieceTables.Hoe,
                overlayType: typeof(SquarePathOverlayVisualizer),
                insertIndex: 4
            )
        },
        {
            "paved_road_v2_path",
            new ToolDB(
                name: "paved_road_v2_path",
                basePrefab: "paved_road_v2",
                pieceName: "$paved_road_v2_path_name",
                pieceDesc: "$paved_road_v2_path_desc",
                icon: IconCache.PavedRoadPath,
                pieceTable: PieceTables.Hoe,
                smooth: false,
                insertIndex: 4
            )
        },
        {
            "paved_road_v2_path_square",
            new ToolDB(
                name: "paved_road_v2_path_square",
                basePrefab: "paved_road_v2",
                pieceName: "$paved_road_v2_path_square_name",
                pieceDesc: "$paved_road_v2_path_square_desc",
                icon: IconCache.PavedRoadPathSquare,
                pieceTable: PieceTables.Hoe,
                overlayType: typeof(SquarePathOverlayVisualizer),
                smooth: false,
                insertIndex: 4
            )
        },
        {
            "remove_terrain_mods",
            new ToolDB(
                name: "remove_terrain_mods",
                basePrefab: "mud_road_v2",
                pieceName: "$remove_terrain_mods_name",
                pieceDesc: "$remove_terrain_mods_desc",
                icon: IconCache.Remove,
                pieceTable: PieceTables.Hoe,
                overlayType: typeof(RemoveModificationsOverlayVisualizer),
                smooth: false,
                level: false,
                raise: false,
                clearPaint: false,
                allowedInDeepSnow: true,
                insertIndex: 4
            )
        },

        // Cultivator Tools
        {
            "cultivate_v2_square",
            new ToolDB(
                name: "cultivate_v2_square",
                basePrefab: "cultivate_v2",
                pieceName: "$cultivate_v2_square_name",
                pieceDesc: "$cultivate_v2_square_desc",
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
                pieceName: "$cultivate_v2_path_name",
                pieceDesc: "$cultivate_v2_path_desc",
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
                pieceName: "$cultivate_v2_path_square_name",
                pieceDesc: "$cultivate_v2_path_square_desc",
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
                pieceName: "$replant_v2_square_name",
                pieceDesc: "$replant_v2_square_desc",
                icon: IconCache.ReplantSquare,
                pieceTable: PieceTables.Cultivator,
                overlayType: typeof(SeedGrassOverlayVisualizer),
                insertIndex: 2
            )
        },
    };
}
