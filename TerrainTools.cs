// Ignore Spelling: TerrainTools Jotunn

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using TerrainTools.Configs;
using System.Reflection;
using System.Collections.Generic;
using Jotunn.Utils;
using System.IO;
using UnityEngine;
using Jotunn.Managers;
using BepInEx.Configuration;
using TerrainTools.Helpers;
using TerrainTools.Extensions;
using System.Drawing;
using System.Drawing.Imaging;

namespace TerrainTools
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid, Jotunn.Main.Version)]
    [NetworkCompatibility(CompatibilityLevel.VersionCheckOnly, VersionStrictness.Patch)]
    internal class TerrainTools : BaseUnityPlugin
    {
        internal const string Author = "Searica";
        public const string PluginName = "AdvancedTerrainModifiers";
        public const string PluginGUID = $"{Author}.Valheim.TerrainTools";
        public const string PluginVersion = "1.1.0";

        internal static Texture2D LoadTextureFromResources(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();
            if (extension != ".png" && extension != ".jpg")
            {
                Log.LogWarning("LoadTextureFromResources can only load png or jpg textures");
                return null;
            }
            fileName = Path.GetFileNameWithoutExtension(fileName);

            Bitmap resource = Properties.Resources.ResourceManager.GetObject(fileName) as Bitmap;
            using (var mStream = new MemoryStream())
        {
                resource.Save(mStream, ImageFormat.Png);
                var buffer = new byte[mStream.Length];
                mStream.Position = 0;
                mStream.Read(buffer, 0, buffer.Length);
                var texture = new Texture2D(0, 0);
                texture.LoadImage(buffer);
                return texture;
            }
        }

        #region Section Names

        private static readonly string MainSection = ConfigManager.SetStringPriority("Global", 3);

        private static readonly string RadiusSection = ConfigManager.SetStringPriority("Radius", 2);
        private static readonly string HardnessSection = ConfigManager.SetStringPriority("Hardness", 1);
        private static readonly string ToolsSection = ConfigManager.SetStringPriority("Tools", 0);

        #endregion Section Names

        #region Tool Configs

        private static ConfigEntry<bool> hoverInfoEnabled;
        internal static bool IsHoverInforEnabled => hoverInfoEnabled.Value;

        /// <summary>
        ///     Dictionary of tool names to corresponding config entry that sets if they are enabled.
        /// </summary>
        private static readonly Dictionary<string, ConfigEntry<bool>> ToolConfigEntries = new();

        private static bool UpdateTools = false;

        internal static bool IsToolEnabled(string toolName)
        {
            if (ToolConfigEntries.TryGetValue(toolName, out ConfigEntry<bool> configEntry))
            {
                if (configEntry != null) return configEntry.Value;
            }
            return false;
        }

        #endregion Tool Configs

        #region Radius Configs

        private static ConfigEntry<bool> enableRadiusModifier;
        private static ConfigEntry<KeyCode> radiusModKey;
        private static ConfigEntry<float> radiusScrollScale;
        private static ConfigEntry<float> maxRadius;
        internal static bool IsEnableRadiusModifier => enableRadiusModifier.Value;
        internal static float MaxRadius => maxRadius.Value;
        internal static KeyCode RadiusKey => radiusModKey.Value;
        internal static float RadiusScrollScale => radiusScrollScale.Value;

        #endregion Radius Configs

        #region Hardness Configs

        private static ConfigEntry<bool> enableHardnessModifier;
        private static ConfigEntry<KeyCode> hardnessModKey;
        private static ConfigEntry<float> hardnessScrollScale;

        internal static bool IsEnableHardnessModifier => enableHardnessModifier.Value;
        internal static KeyCode HardnessKey => hardnessModKey.Value;
        internal static float HardnessScrollScale => hardnessScrollScale.Value;

        //private static ConfigEntry<MessageHud.MessageType> hardnessMsgType;
        //internal static MessageHud.MessageType HardnessMsgType => hardnessMsgType.Value;

        #endregion Hardness Configs

        // Stretch Goal:
        // - Add a shovel tool that lets you lower terrain

        public void Awake()
        {
            Instance = this;

            Log.Init(Logger);

            ConfigManager.Init(PluginGUID, Config);
            SetUpConfigEntries();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginGUID);
            Game.isModded = true;

            PieceManager.OnPiecesRegistered += InitManager.InitToolPieces;

            ConfigManager.SetupWatcher();
            ConfigManager.CheckForConfigManager();

            // Update tools if config file reloaded
            ConfigManager.OnConfigFileReloaded += () =>
            {
                if (UpdateTools)
                {
                    InitManager.UpdatePlugin();
                    UpdateTools = false;
                }
            };

            // Update tools if in-game config manager window is closed
            ConfigManager.OnConfigWindowClosed += () =>
            {
                if (UpdateTools)
                {
                    InitManager.UpdatePlugin();
                    ConfigManager.Save();
                    UpdateTools = false;
                }
            };

            // Update tools if in-game config manager window is closed
            SynchronizationManager.OnConfigurationSynchronized += (obj, args) =>
            {
                if (UpdateTools)
                {
                    InitManager.UpdatePlugin();
                    ConfigManager.Save();
                    UpdateTools = false;
                }
            };
        }

        public void OnDestroy()
        {
            ConfigManager.Save();
        }

        internal static void SetUpConfigEntries()
        {
            Log.Verbosity = ConfigManager.BindConfig(
                MainSection,
                "Verbosity",
                LogLevel.Low,
                "Low will log basic information about the mod. Medium will log information that " +
                "is useful for troubleshooting. High will log a lot of information, do not set " +
                "it to this without good reason as it will slow Down your game.",
                synced: false
            );

            enableRadiusModifier = ConfigManager.BindConfig(
                RadiusSection,
                ConfigManager.SetStringPriority("RadiusModifier", 1),
                true,
                "Set to true/enabled to allow modifying the radius of terrain tools using the scroll wheel. " +
                "Note: Radius cannot be changed on square terraforming tools."
            );

            radiusModKey = ConfigManager.BindConfig(
                RadiusSection,
                "RadiusModKey",
                KeyCode.LeftAlt,
                "Modifier key that must be held down when using scroll wheel to change the radius of terrain tools."
            );

            radiusScrollScale = ConfigManager.BindConfig(
                RadiusSection,
                "RadiusScrollScale",
                0.1f,
                "Scroll wheel change scale",
                new AcceptableValueRange<float>(0.05f, 2f)
            );

            maxRadius = ConfigManager.BindConfig(
                RadiusSection,
                "MaxRadius",
                10f,
                "Maximum radius of terrain tools.",
                new AcceptableValueRange<float>(4f, 20f)
            );

            enableHardnessModifier = ConfigManager.BindConfig(
                HardnessSection,
                ConfigManager.SetStringPriority("HardnessModifier", 1),
                true,
                "Set to true/enabled to allow modifying the hardness of terrain tools using the scroll wheel. " +
                "Note: Hardness cannot be changed on square terraforming tools and tools that do not alter " +
                "ground height do not have a hardness."
            );

            hardnessModKey = ConfigManager.BindConfig(
                HardnessSection,
                "HardnessModKey",
                KeyCode.LeftControl,
                "Modifier key that must be held down when using scroll wheel to change the hardness of terrain tools."
            );

            hardnessScrollScale = ConfigManager.BindConfig(
                HardnessSection,
                "HardnessScrollScale",
                0.1f,
                "Scroll wheel change scale",
                new AcceptableValueRange<float>(0.05f, 2f)
            );

            hoverInfoEnabled = ConfigManager.BindConfig(
                ToolsSection,
                ConfigManager.SetStringPriority("HoverInfo", 1),
                true,
                "Set to true/enabled to show terrain height when using square terrain tools."
            );

            foreach (var key in ToolConfigs.ToolConfigsMap.Keys)
            {
                var configEntry = ConfigManager.BindConfig(
                    ToolsSection,
                    key,
                    true,
                    "Set to true/enabled to add this terrain tool. Set to false/disabled to remove it."
                );
                configEntry.SettingChanged += delegate { UpdateTools = !UpdateTools || UpdateTools; };
                ToolConfigEntries.Add(key, configEntry);
            }
            ConfigManager.Save();
        }
    }

    /// <summary>
    ///     Log level to control output to BepInEx log
    /// </summary>
    internal enum LogLevel
    {
        Low = 0,
        Medium = 1,
        High = 2,
    }

    /// <summary>
    ///     Helper class for properly logging from static contexts.
    /// </summary>
    internal static class Log
    {
        #region Verbosity

        internal static ConfigEntry<LogLevel> Verbosity { get; set; }
        internal static LogLevel VerbosityLevel => Verbosity.Value;

        #endregion Verbosity

        internal static ManualLogSource _logSource;

        internal static void Init(ManualLogSource logSource)
        {
            _logSource = logSource;
        }

        internal static void LogDebug(object data) => _logSource.LogDebug(data);

        internal static void LogError(object data) => _logSource.LogError(data);

        internal static void LogFatal(object data) => _logSource.LogFatal(data);

        internal static void LogMessage(object data) => _logSource.LogMessage(data);

        internal static void LogWarning(object data) => _logSource.LogWarning(data);

        internal static void LogInfo(object data, LogLevel level = LogLevel.Low)
        {
            if (Verbosity is null || VerbosityLevel >= level)
            {
                _logSource.LogInfo(data);
            }
        }

        internal static void LogGameObject(GameObject prefab, bool includeChildren = false)
        {
            LogInfo("***** " + prefab.name + " *****");
            foreach (Component compo in prefab.GetComponents<Component>())
            {
                LogComponent(compo);
            }

            if (!includeChildren) { return; }

            LogInfo("***** " + prefab.name + " (children) *****");
            foreach (Transform child in prefab.transform)
            {
                LogInfo($" - {child.gameObject.name}");
                foreach (Component compo in child.gameObject.GetComponents<Component>())
                {
                    LogComponent(compo);
                }
            }
        }

        internal static void LogComponent(Component compo)
        {
            LogInfo($"--- {compo.GetType().Name}: {compo.name} ---");

            PropertyInfo[] properties = compo.GetType().GetProperties(ReflectionUtils.AllBindings);
            foreach (var property in properties)
            {
                LogInfo($" - {property.Name} = {property.GetValue(compo)}");
            }

            FieldInfo[] fields = compo.GetType().GetFields(ReflectionUtils.AllBindings);
            foreach (var field in fields)
            {
                LogInfo($" - {field.Name} = {field.GetValue(compo)}");
            }
        }
    }
}