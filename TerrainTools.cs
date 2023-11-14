﻿// Ignore Spelling: TerrainTools Jotunn

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
        public const string PluginVersion = "1.0.1";

        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        //public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        private static TerrainTools Instance { get; set; }

        internal static Texture2D LoadTextureFromDisk(string fileName) => AssetUtils.LoadTexture(Path.Combine(Path.GetDirectoryName(Instance.Info.Location), fileName));

        #region Section Names

        private static readonly string MainSection = ConfigManager.SetStringPriority("Global", 3);

        private static readonly string RadiusSection = ConfigManager.SetStringPriority("Radius", 2);
        private static readonly string ToolsSection = ConfigManager.SetStringPriority("Tools", 1);

        #endregion Section Names

        #region Tool Configs

        private static ConfigEntry<bool> HoverInfoEnabled;
        internal static bool IsHoverInforEnabled => HoverInfoEnabled.Value;

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

        internal static ConfigEntry<bool> EnableRadiusModifier;
        internal static ConfigEntry<KeyCode> _ScrollModKey;
        internal static ConfigEntry<float> _ScrollWheelScale;
        internal static ConfigEntry<float> _MaxRadius;
        internal static bool IsEnableRadiusModifier => EnableRadiusModifier.Value;
        internal static float MaxRadius => _MaxRadius.Value;
        internal static KeyCode ScrollModKey => _ScrollModKey.Value;
        internal static float ScrollWheelScale => _ScrollWheelScale.Value;

        #endregion Radius Configs

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

            EnableRadiusModifier = ConfigManager.BindConfig(
                RadiusSection,
                ConfigManager.SetStringPriority("RadiusModifier", 1),
                true,
                "Set to true/enabled to allow modifying the radius of terrain tools using the scroll wheel. " +
                "Note: Radius cannot be changed on square terraforming tools."
            );

            _ScrollModKey = ConfigManager.BindConfig(
                RadiusSection,
                "ScrollModKey",
                KeyCode.LeftAlt,
                "Modifier key that must be held down when using scroll wheel to change the radius of terrain tools."
            );

            _ScrollWheelScale = ConfigManager.BindConfig(
                RadiusSection,
                "ScrollWheelScale",
                0.1f,
                "Scroll wheel change scale",
                new AcceptableValueRange<float>(0.05f, 2f)
            );

            _MaxRadius = ConfigManager.BindConfig(
                RadiusSection,
                "MaxRadius",
                10f,
                "Maximum radius of terrain tools.",
                new AcceptableValueRange<float>(4f, 20f)
            );

            HoverInfoEnabled = ConfigManager.BindConfig(
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
            if (VerbosityLevel >= level)
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