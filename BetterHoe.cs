// Ignore Spelling: BetterHoe Jotunn

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using BetterHoe.Configs;
using System.Reflection;
using Jotunn.Utils;
using System.IO;
using UnityEngine;
using Jotunn.Managers;
using Jotunn.Configs;
using Jotunn.Entities;

namespace BetterHoe
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid, Jotunn.Main.Version)]
    [NetworkCompatibility(CompatibilityLevel.VersionCheckOnly, VersionStrictness.Patch)]
    internal class BetterHoe : BaseUnityPlugin
    {
        internal const string Author = "Searica";
        public const string PluginName = "BetterHoe";
        public const string PluginGUID = $"{Author}.Valheim.{PluginName}";
        public const string PluginVersion = "0.0.1";

        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        //public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        private static BetterHoe Instance { get; set; }

        internal static Texture2D LoadTextureFromDisk(string fileName) => AssetUtils.LoadTexture(Path.Combine(Path.GetDirectoryName(Instance.Info.Location), fileName));

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
            PrefabManager.OnVanillaPrefabsAvailable += ModVanillaValheimTools;
        }

        public void OnDestroy()
        {
            ConfigManager.Save();
        }

        private void AddToolPieces()
        {
            //AddToolPiece<UndoModificationsOverlayVisualizer>("Undo Terrain Modification", "mud_road_v2", "Hoe", OverlayVisualizer.undo);
            //AddToolPiece<RedoModificationsOverlayVisualizer>("Redo Terrain Modification", "mud_road_v2", "Hoe", OverlayVisualizer.redo);
            AddToolPiece<RemoveModificationsOverlayVisualizer>("Remove Terrain Modifications", "mud_road_v2", "Hoe", OverlayVisualizer.remove);
        }

        private void AddToolPiece<TOverlayVisualizer>(string pieceName, string basePieceName, string pieceTable, Texture2D iconTexture, bool level = false, bool raise = false, bool smooth = false, bool paint = false) where TOverlayVisualizer : OverlayVisualizer
        {
            var pieceExists = PieceManager.Instance.GetPiece(pieceName) != null;
            if (pieceExists)
                return;

            var pieceIcon = Sprite.Create(iconTexture, new Rect(0, 0, iconTexture.width, iconTexture.height), Vector2.zero);
            var piece = new CustomPiece(pieceName, basePieceName, new PieceConfig
            {
                Name = pieceName,
                Icon = pieceIcon,
                PieceTable = pieceTable
            });

            var settings = piece.PiecePrefab.GetComponent<TerrainOp>().m_settings;
            settings.m_level = level;
            settings.m_raise = raise;
            settings.m_smooth = smooth;
            settings.m_paintCleared = paint;
            piece.PiecePrefab.AddComponent<TOverlayVisualizer>();

            PieceManager.Instance.AddPiece(piece);
        }

        private void ModVanillaValheimTools()
        {
            PrefabManager.Instance.GetPrefab("mud_road_v2").AddComponent<LevelGroundOverlayVisualizer>();
            PrefabManager.Instance.GetPrefab("raise_v2").AddComponent<RaiseGroundOverlayVisualizer>();
            PrefabManager.Instance.GetPrefab("path_v2").AddComponent<PaveRoadOverlayVisualizer>();
            PrefabManager.Instance.GetPrefab("paved_road_v2").AddComponent<PaveRoadOverlayVisualizer>();
            PrefabManager.Instance.GetPrefab("cultivate_v2").AddComponent<CultivateOverlayVisualizer>();
            PrefabManager.Instance.GetPrefab("replant_v2").AddComponent<SeedGrassOverlayVisualizer>();
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