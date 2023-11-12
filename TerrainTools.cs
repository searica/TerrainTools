// Ignore Spelling: TerrainTools Jotunn

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using TerrainTools.Configs;
using System.Reflection;
using Jotunn.Utils;
using System.IO;
using UnityEngine;
using Jotunn.Managers;
using Jotunn.Configs;
using Jotunn.Entities;
using TerrainTools.Visualization;

namespace TerrainTools
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid, Jotunn.Main.Version)]
    [NetworkCompatibility(CompatibilityLevel.VersionCheckOnly, VersionStrictness.Patch)]
    internal class TerrainTools : BaseUnityPlugin
    {
        internal const string Author = "Searica";
        public const string PluginName = "TerrainTools";
        public const string PluginGUID = $"{Author}.Valheim.{PluginName}";
        public const string PluginVersion = "0.0.1";

        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        //public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        private static TerrainTools Instance { get; set; }

        internal static Texture2D LoadTextureFromDisk(string fileName) => AssetUtils.LoadTexture(Path.Combine(Path.GetDirectoryName(Instance.Info.Location), fileName));

        // Roadmap

        // Make pathen versions of:
        // - Cobblestone
        // - Cultivate

        // Make Square versions of:
        // - Cultivate
        // - Cultivate (Pathen)
        // - Grass
        // - Level
        // - Pathen
        // - Cobblestone
        // - Cobblestone (Pathen)
        // - Raise ground

        // Add tools:
        // - Remove Terrain modifications
        // - Precision raise ground

        // Add feature:
        // - Hotkey to let scroll wheel change size of any of the above

        // Configuration:
        // - Option to enable/disable each of the above tools
        // - Options to set max and min size for when changing tool size
        // - Configure hotkey for changing size with scroll wheel

        public void Awake()
        {
            Instance = this;

            Log.Init(Logger);

            ConfigManager.Init(PluginGUID, Config);
            ConfigManager.SetUpConfig();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginGUID);

            Game.isModded = true;

            ConfigManager.SetupWatcher();
            ConfigManager.CheckForConfigManager();

            PrefabManager.OnVanillaPrefabsAvailable += AddToolPieces;
        }

        public void OnDestroy()
        {
            ConfigManager.Save();
        }

        private void AddToolPieces()
        {
            AddToolPiece<LevelGroundOverlayVisualizer>(
                "Level Terrain (Square)",
                "mud_road_v2",
                PieceTables.Hoe,
                IconCache.MudRoadSquare
            );

            AddToolPiece<LevelGroundOverlayVisualizer>(
                "Raise Terrain (Square)",
                "raise_v2",
                PieceTables.Hoe,
                IconCache.RaiseSquare
            );

            AddToolPiece<PaveRoadOverlayVisualizer>(
                "Pathen (Square)",
                "path_v2",
                PieceTables.Hoe,
                IconCache.MudRoadPathSquare
            );

            AddToolPiece<PaveRoadOverlayVisualizer>(
                "Cobblestone (Square)",
                "paved_road_v2",
                PieceTables.Hoe,
                IconCache.PavedRoadSquare
            );

            AddToolPiece<PaveRoadOverlayVisualizer>(
                "Cobblestone Path (Square)",
                "paved_road_v2",
                PieceTables.Hoe,
                IconCache.PavedRoadPathSquare,
                smooth: false
            );

            AddToolPiece<CultivateOverlayVisualizer>(
                "Cultivate (Square)",
                "cultivate_v2",
                PieceTables.Cultivator,
                IconCache.CultivateSquare
            );

            AddToolPiece<CultivateOverlayVisualizer>(
                "Cultivate Path (Square)",
                "cultivate_v2",
                PieceTables.Cultivator,
                IconCache.CultivatePathSquare,
                smooth: false
            );

            AddToolPiece<SeedGrassOverlayVisualizer>(
                "Replant (Square)",
                "replant_v2",
                PieceTables.Cultivator,
                IconCache.ReplantSquare
            );

            AddToolPiece<RemoveModificationsOverlayVisualizer>(
                "Remove Terrain Modifications",
                "mud_road_v2",
                PieceTables.Hoe,
                IconCache.Remove,
                level: false,
                raise: false,
                smooth: false,
                clearPaint: false
            );

            PrefabManager.OnVanillaPrefabsAvailable -= AddToolPieces;
        }

        private void AddToolPiece<TOverlayVisualizer>(
            string pieceName,
            string basePieceName,
            string pieceTable,
            Texture2D iconTexture,
            bool? level = null,
            bool? raise = null,
            bool? smooth = null,
            bool? clearPaint = null
        ) where TOverlayVisualizer : OverlayVisualizer
        {
            if (PieceManager.Instance.GetPiece(pieceName) != null) { return; }

            var pieceIcon = Sprite.Create(iconTexture, new Rect(0, 0, iconTexture.width, iconTexture.height), Vector2.zero);
            var piece = new CustomPiece(
                pieceName,
                basePieceName,
                new PieceConfig
                {
                    Name = pieceName,
                    Icon = pieceIcon,
                    PieceTable = pieceTable
                }
            );

            var settings = piece.PiecePrefab.GetComponent<TerrainOp>().m_settings;
            settings.m_level = level != null ? level.Value : settings.m_level;
            settings.m_raise = raise != null ? raise.Value : settings.m_raise;
            settings.m_smooth = smooth != null ? smooth.Value : settings.m_smooth;
            settings.m_paintCleared = clearPaint != null ? clearPaint.Value : settings.m_paintCleared;
            piece.PiecePrefab.AddComponent<TOverlayVisualizer>();

            PieceManager.Instance.AddPiece(piece);
        }
    }

    /// <summary>
    /// Helper class for properly logging from static contexts.
    /// </summary>
    internal static class Log
    {
        internal static ManualLogSource _logSource;

        internal static void Init(ManualLogSource logSource)
        {
            _logSource = logSource;
        }

        internal static void LogDebug(object data) => _logSource.LogDebug(data);

        internal static void LogError(object data) => _logSource.LogError(data);

        internal static void LogFatal(object data) => _logSource.LogFatal(data);

        internal static void LogInfo(object data) => _logSource.LogInfo(data);

        internal static void LogMessage(object data) => _logSource.LogMessage(data);

        internal static void LogWarning(object data) => _logSource.LogWarning(data);
    }
}