// Ignore Spelling: TerrainTools Jotunn

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using TerrainTools.Configs;
using TerrainTools.Extensions;
using TerrainTools.Helpers;
using UnityEngine;

namespace TerrainTools {
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid, Jotunn.Main.Version)]
    [NetworkCompatibility(CompatibilityLevel.VersionCheckOnly, VersionStrictness.Patch)]
    internal sealed class TerrainTools : BaseUnityPlugin {
        internal const string Author = "Searica";
        public const string PluginName = "AdvancedTerrainModifiers";
        public const string PluginGUID = $"{Author}.Valheim.TerrainTools";
        public const string PluginVersion = "1.2.3";

        #region Section Names

        private static readonly string MainSection = ConfigManager.SetStringPriority("Global", 10);
        private static readonly string RadiusSection = ConfigManager.SetStringPriority("Radius", 8);
        private static readonly string HardnessSection = ConfigManager.SetStringPriority("Hardness", 6);
        private static readonly string ShovelSection = ConfigManager.SetStringPriority("Shovel", 4);
        private static readonly string HoeSection = ConfigManager.SetStringPriority("Hoe", 2);
        private static readonly string CultivatorSection = ConfigManager.SetStringPriority("Cultivator", 0);

        internal static bool UpdatePlugin = false;

        /// <summary>
        ///     Get the appropriate configuratin section based on PieceTable and fall back to MainSection if needed.
        /// </summary>
        /// <param name="pieceTable"></param>
        /// <returns></returns>
        private static string GetSectionName(string pieceTable) {
            if (pieceTable == PieceTables.Hoe) { return HoeSection; }
            if (pieceTable == PieceTables.Cultivator) { return CultivatorSection; }
            if (pieceTable == Shovel.ShovelPieceTable) { return ShovelSection; }
            return MainSection;
        }

        #endregion Section Names

        #region Tool Configs

        private static ConfigEntry<bool> hoverInfoEnabled;
        internal static bool IsHoverInforEnabled => hoverInfoEnabled.Value;

        /// <summary>
        ///     Dictionary of tool names to corresponding config entry that sets if they are enabled.
        /// </summary>
        private static readonly Dictionary<string, ConfigEntry<bool>> ToolConfigEntries = new();

        internal static bool IsToolEnabled(string toolName) {
            if (ToolConfigEntries.TryGetValue(toolName, out ConfigEntry<bool> configEntry)) {
                return configEntry != null && configEntry.Value;
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

        #endregion Hardness Configs

        private static ConfigEntry<bool> enableShovel;
        internal static bool IsShovelEnabled => enableShovel.Value;

        public void Awake() {
            Log.Init(Logger);

            ConfigManager.Init(PluginGUID, Config);
            SetUpConfigEntries();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginGUID);
            Game.isModded = true;

            PrefabManager.OnVanillaPrefabsAvailable += Shovel.CreateShovel;
            PieceManager.OnPiecesRegistered += InitManager.InitToolPieces;

            _ = GUIManager.Instance; // Fix rare NRE on shutdown
            ConfigManager.SetupWatcher();
            ConfigManager.CheckForConfigManager();

            // Update tools if config file reloaded
            ConfigManager.OnConfigFileReloaded += () => {
                if (UpdatePlugin) {
                    InitManager.UpdatePlugin();
                    UpdatePlugin = false;
                }
            };

            // Update tools if in-game config manager window is closed
            ConfigManager.OnConfigWindowClosed += () => {
                if (UpdatePlugin) {
                    InitManager.UpdatePlugin();
                }
            };

            // Update tools if in-game config manager window is closed
            SynchronizationManager.OnConfigurationSynchronized += (obj, args) => {
                if (UpdatePlugin) {
                    InitManager.UpdatePlugin();
                    UpdatePlugin = false;
                }
            };
        }

        public void OnDestroy() {
            ConfigManager.Save();
        }

        internal static void SetUpConfigEntries() {
            Log.Verbosity = ConfigManager.BindConfig(
                MainSection,
                "Verbosity",
                LogLevel.Low,
                "Low will log basic information about the mod. Medium will log information that " +
                "is useful for troubleshooting. High will log a lot of information, do not set " +
                "it to this without good reason as it will slow Down your game.",
                synced: false
            );

            hoverInfoEnabled = ConfigManager.BindConfig(
                MainSection,
                "HoverInfo",
                true,
                "Set to true/enabled to show terrain height when using square terrain tools."
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
                "Modifier key that must be held down when using scroll wheel to change the radius of terrain tools.",
                synced: false
            );

            radiusScrollScale = ConfigManager.BindConfig(
                RadiusSection,
                "RadiusScrollScale",
                0.1f,
                "Scroll wheel change scale, larger magnitude means the radius will change " +
                "faster and negative sign will reverse the direction you need to scroll to increase the radius.",
                new AcceptableValueRange<float>(-1f, 1f),
                synced: false
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
                "Modifier key that must be held down when using scroll wheel to change the hardness of terrain tools.",
                synced: false
            );

            hardnessScrollScale = ConfigManager.BindConfig(
                HardnessSection,
                "HardnessScrollScale",
                0.1f,
                "Scroll wheel change scale, larger magnitude means the hardness will change " +
                "faster and negative sign will reverse the direction you need to scroll to increase the hardness.",
                new AcceptableValueRange<float>(-1f, 1f),
                synced: false
            );

            enableShovel = ConfigManager.BindConfig(
                ShovelSection,
                ConfigManager.SetStringPriority("Shovel", 1),
                true,
                "Set to true/enabled to allow crafting the shovel. Setting to false/disabled " +
                "will prevent crafting new shovels but will not affect existing shovels in the world."
            );
            enableShovel.SettingChanged += SetUpdatePlugin;

            foreach (var key in ToolConfigs.ToolConfigsMap.Keys) {
                ToolDB toolDB = ToolConfigs.ToolConfigsMap[key];

                var configEntry = ConfigManager.BindConfig(
                    GetSectionName(toolDB.pieceTable),
                    key,
                    true,
                    "Set to true/enabled to add this terrain tool. Set to false/disabled to remove it."
                );
                configEntry.SettingChanged += SetUpdatePlugin;
                ToolConfigEntries.Add(key, configEntry);
            }
            ConfigManager.Save();
        }

        /// <summary>
        ///     Event delegate to set flag that settings have been changed
        ///     that require updating plugin.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="e"></param>
        private static void SetUpdatePlugin(object obj, EventArgs e) {
            UpdatePlugin = !UpdatePlugin || UpdatePlugin;
        }
    }

    /// <summary>
    ///     Log level to control output to BepInEx log
    /// </summary>
    internal enum LogLevel {
        Low = 0,
        Medium = 1,
        High = 2,
    }

    /// <summary>
    ///     Helper class for properly logging from static contexts.
    /// </summary>
    internal static class Log {
        #region Verbosity

        internal static ConfigEntry<LogLevel> Verbosity { get; set; }
        internal static LogLevel VerbosityLevel => Verbosity.Value;

        #endregion Verbosity

        private static ManualLogSource logSource;

        internal static void Init(ManualLogSource logSource) {
            Log.logSource = logSource;
        }

        internal static void LogDebug(object data) => logSource.LogDebug(data);

        internal static void LogError(object data) => logSource.LogError(data);

        internal static void LogFatal(object data) => logSource.LogFatal(data);

        internal static void LogMessage(object data) => logSource.LogMessage(data);

        internal static void LogWarning(object data) => logSource.LogWarning(data);

        internal static void LogInfo(object data, LogLevel level = LogLevel.Low) {
            if (Verbosity is null || VerbosityLevel >= level) {
                logSource.LogInfo(data);
            }
        }

        internal static void LogGameObject(GameObject prefab, bool includeChildren = false) {
            LogInfo("***** " + prefab.name + " *****");
            foreach (Component compo in prefab.GetComponents<Component>()) {
                LogComponent(compo);
            }

            if (!includeChildren) { return; }

            LogInfo("***** " + prefab.name + " (children) *****");
            foreach (Transform child in prefab.transform) {
                LogInfo($" - {child.gameObject.name}");
                foreach (Component compo in child.gameObject.GetComponents<Component>()) {
                    LogComponent(compo);
                }
            }
        }

        internal static void LogComponent(Component compo) {
            LogInfo($"--- {compo.GetType().Name}: {compo.name} ---");

            PropertyInfo[] properties = compo.GetType().GetProperties(ReflectionUtils.AllBindings);
            foreach (var property in properties) {
                LogInfo($" - {property.Name} = {property.GetValue(compo)}");
            }

            FieldInfo[] fields = compo.GetType().GetFields(ReflectionUtils.AllBindings);
            foreach (var field in fields) {
                LogInfo($" - {field.Name} = {field.GetValue(compo)}");
            }
        }
    }
}