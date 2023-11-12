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
                "Level Terrain (Square)",
                new ToolDB(
                    name: "Level Terrain (Square)",
                    basePrefab:"mud_road_v2",
                    pieceName: "Level Terrain (Square)",
                    pieceDesc: "",
                    icon: IconCache.MudRoadSquare,
                    pieceTable: PieceTables.Hoe,
                    overlayType: typeof(LevelGroundOverlayVisualizer)
                )
            },
            {
                "Raise Terrain (Precision)",
                new ToolDB(
                    name: "Raise Terrain (Precision)",
                    basePrefab:"raise_v2",
                    pieceName: "Raise Terrain (Precision)",
                    pieceDesc: "",
                    icon: IconCache.RaiseSquare,
                    pieceTable: PieceTables.Hoe,
                    overlayType: typeof(RaiseGroundOverlayVisualizer)
                )
            },
            {
                "Pathen (Square)",
                new ToolDB(
                    name: "Pathen (Square)",
                    basePrefab:"",
                    pieceName: "Pathen (Square)",
                    pieceDesc: "path_v2",
                    icon: IconCache.MudRoadPathSquare,
                    pieceTable: PieceTables.Hoe,
                    overlayType: typeof(SquarePathOverlayVisualizer)
                )
            },
            {
                "Cobblestone (Square)",
                new ToolDB(
                    name: "Cobblestone (Square)",
                    basePrefab:"paved_road_v2",
                    pieceName: "Cobblestone (Square)",
                    pieceDesc: "",
                    icon: IconCache.PavedRoadSquare,
                    pieceTable: PieceTables.Hoe,
                    overlayType: typeof(SquarePathOverlayVisualizer)
                )
            },
            {
                "Cobblestone Path",
                new ToolDB(
                    name: "Cobblestone Path",
                    basePrefab:"paved_road_v2",
                    pieceName: "Cobblestone Path",
                    pieceDesc: "",
                    icon: IconCache.PavedRoadPath,
                    pieceTable: PieceTables.Hoe,
                    smooth: false
                )
            },
            {
                "Cobblestone Path (Square)",
                new ToolDB(
                    name: "Cobblestone Path (Square)",
                    basePrefab:"paved_road_v2",
                    pieceName: "Cobblestone Path (Square)",
                    pieceDesc: "",
                    icon: IconCache.PavedRoadPathSquare,
                    pieceTable: PieceTables.Hoe,
                    overlayType: typeof(SquarePathOverlayVisualizer),
                    smooth: false
                )
            },
            {
                "Remove Terrain Modifications",
                new ToolDB(
                    name: "Remove Terrain Modifications",
                    basePrefab:"mud_road_v2",
                    pieceName: "Remove Terrain Modifications",
                    pieceDesc: "",
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
                "Cultivate (Square)",
                new ToolDB(
                    name: "Cultivate (Square)",
                    basePrefab:"cultivate_v2",
                    pieceName: "Cultivate (Square)",
                    pieceDesc: "",
                    icon: IconCache.CultivateSquare,
                    pieceTable: PieceTables.Cultivator,
                    overlayType: typeof(CultivateOverlayVisualizer)
                )
            },
            {
                "Cultivate Path",
                new ToolDB(
                    name: "Cultivate Path",
                    basePrefab:"cultivate_v2",
                    pieceName: "Cultivate Path",
                    pieceDesc: "",
                    icon: IconCache.CultivatePath,
                    pieceTable: PieceTables.Cultivator,
                    smooth: false
                )
            },
            {
                "Cultivate Path (Square)",
                new ToolDB(
                    name: "Cultivate Path (Square)",
                    basePrefab:"cultivate_v2",
                    pieceName: "Cultivate Path (Square)",
                    pieceDesc: "",
                    icon: IconCache.CultivatePathSquare,
                    pieceTable: PieceTables.Cultivator,
                    overlayType: typeof(CultivateOverlayVisualizer),
                    smooth: false
                )
            },
            {
                "Replant (Square)",
                new ToolDB(
                    name: "Replant (Square)",
                    basePrefab:"replant_v2",
                    pieceName: "Replant (Square)",
                    pieceDesc: "",
                    icon: IconCache.ReplantSquare,
                    pieceTable: PieceTables.Cultivator,
                    overlayType: typeof(SeedGrassOverlayVisualizer)
                )
            },
        };
    }
}

//private void AddToolPieces()
//{
//    AddToolPiece<LevelGroundOverlayVisualizer>(
//        "Level Terrain (Square)",
//        "mud_road_v2",
//        PieceTables.Hoe,
//        IconCache.MudRoadSquare
//    );

//    AddToolPiece<RaiseGroundOverlayVisualizer>(
//        "Raise Terrain (Precision)",
//        "raise_v2",
//        PieceTables.Hoe,
//        IconCache.RaiseSquare
//    );

//    AddToolPiece<SquarePathOverlayVisualizer>(
//        "Pathen (Square)",
//        "path_v2",
//        PieceTables.Hoe,
//        IconCache.MudRoadPathSquare
//    );

//    AddToolPiece<SquarePathOverlayVisualizer>(
//        "Cobblestone (Square)",
//        "paved_road_v2",
//        PieceTables.Hoe,
//        IconCache.PavedRoadSquare
//    );

//    AddToolPiece(
//        "Cobblestone Path",
//        "paved_road_v2",
//        PieceTables.Hoe,
//        IconCache.PavedRoadPath,
//        smooth: false
//    );

//    AddToolPiece<SquarePathOverlayVisualizer>(
//        "Cobblestone Path (Square)",
//        "paved_road_v2",
//        PieceTables.Hoe,
//        IconCache.PavedRoadPathSquare,
//        smooth: false
//    );

//    AddToolPiece<CultivateOverlayVisualizer>(
//        "Cultivate (Square)",
//        "cultivate_v2",
//        PieceTables.Cultivator,
//        IconCache.CultivateSquare
//    );

//    AddToolPiece(
//        "Cultivate Path",
//        "cultivate_v2",
//        PieceTables.Cultivator,
//        IconCache.CultivatePath,
//        smooth: false
//    );

//    AddToolPiece<CultivateOverlayVisualizer>(
//        "Cultivate Path (Square)",
//        "cultivate_v2",
//        PieceTables.Cultivator,
//        IconCache.CultivatePathSquare,
//        smooth: false
//    );

//    AddToolPiece<SeedGrassOverlayVisualizer>(
//        "Replant (Square)",
//        "replant_v2",
//        PieceTables.Cultivator,
//        IconCache.ReplantSquare
//    );

//    AddToolPiece<RemoveModificationsOverlayVisualizer>(
//        "Remove Terrain Modifications",
//        "mud_road_v2",
//        PieceTables.Hoe,
//        IconCache.Remove,
//        level: false,
//        raise: false,
//        smooth: false,
//        clearPaint: false
//    );

//    PrefabManager.OnVanillaPrefabsAvailable -= AddToolPieces;
//}