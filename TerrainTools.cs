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

namespace TerrainTools;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[BepInDependency(Jotunn.Main.ModGuid, Jotunn.Main.Version)]
[NetworkCompatibility(CompatibilityLevel.VersionCheckOnly, VersionStrictness.Patch)]
[SynchronizationMode(AdminOnlyStrictness.IfOnServer)]
internal sealed class TerrainTools : BaseUnityPlugin
{
    internal const string Author = "Searica";
    public const string PluginName = "AdvancedTerrainModifiers";
    public const string PluginGUID = $"{Author}.Valheim.TerrainTools";
    public const string PluginVersion = "1.4.1";

    public static TerrainTools Instance;

    #region Section Names

    private const string MainSection = "1 - Global";
    private const string RadiusSection = "2 - Radius";
    private const string SharpnessSection = "3 - Sharpness";
    private const string ShovelSection = "4 - Shovel";
    private const string HoeSection = "5 - Hoe";
    private const string CultivatorSection = "6 - Cultivator";

    internal bool UpdatePlugin = false;

    /// <summary>
    ///     Get the appropriate configuratin section based on PieceTable and fall back to MainSection if needed.
    /// </summary>
    /// <param name="pieceTable"></param>
    /// <returns></returns>
    private static string GetSectionName(string pieceTable)
    {
        if (pieceTable == PieceTables.Hoe) { return HoeSection; }
        if (pieceTable == PieceTables.Cultivator) { return CultivatorSection; }
        if (pieceTable == Shovel.ShovelPieceTable) { return ShovelSection; }
        return MainSection;
    }

    #endregion Section Names

    #region Tool Configs

    private ConfigEntry<bool> hoverInfoEnabled;
    internal bool IsHoverInforEnabled => hoverInfoEnabled.Value;

    /// <summary>
    ///     Dictionary of tool names to corresponding config entry that sets if they are enabled.
    /// </summary>
    private readonly Dictionary<string, ConfigEntry<bool>> ToolConfigEntries = new();

    internal bool IsToolEnabled(string toolName)
    {
        if (ToolConfigEntries.TryGetValue(toolName, out ConfigEntry<bool> configEntry))
        {
            return configEntry != null && configEntry.Value;
        }
        return false;
    }

    #endregion Tool Configs

    #region Radius Configs

    private ConfigEntry<bool> enableRadiusModifier;
    private ConfigEntry<KeyCode> radiusModKey;
    private ConfigEntry<float> radiusScrollScale;
    private ConfigEntry<float> maxRadius;
    internal bool IsEnableRadiusModifier => enableRadiusModifier.Value;
    internal float MaxRadius => maxRadius.Value;
    internal KeyCode RadiusKey => radiusModKey.Value;
    internal float RadiusScrollScale => radiusScrollScale.Value;

    #endregion Radius Configs

    #region Sharpness Configs

    private ConfigEntry<bool> enableSharpnessModifier;
    private ConfigEntry<KeyCode> hardnessModKey;
    private ConfigEntry<float> hardnessScrollScale;

    internal bool IsEnableSharpnessModifier => enableSharpnessModifier.Value;
    internal KeyCode SharpnessKey => hardnessModKey.Value;
    internal float SharpnessScrollScale => hardnessScrollScale.Value;

    #endregion Sharpness Configs

    private ConfigEntry<bool> enableShovel;
    internal bool IsShovelEnabled => enableShovel.Value;

    public void Awake()
    {
        Instance = this;
        Log.Init(Logger);

        Config.Init(PluginGUID, saveOnConfigSet: false);
        SetUpConfigEntries();
        Config.Save();
        Config.SaveOnConfigSet = true;

        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginGUID);
        Game.isModded = true;

        PrefabManager.OnVanillaPrefabsAvailable += Shovel.CreateShovel;
        PieceManager.OnPiecesRegistered += InitManager.InitToolPieces;

        _ = GUIManager.Instance; // Fix rare NRE on shutdown
        Config.SetupWatcher();

        // Update tools if config file reloaded
        ConfigFileManager.OnConfigFileReloaded += () =>
        {
            if (UpdatePlugin)
            {
                InitManager.UpdatePlugin();
                UpdatePlugin = false;
            }
        };

        // Update tools if in-game config manager window is closed
        SynchronizationManager.OnConfigurationWindowClosed += () =>
        {
            if (UpdatePlugin)
            {
                InitManager.UpdatePlugin();
            }
        };

        // Update tools if in-game config manager window is closed
        SynchronizationManager.OnConfigurationSynchronized += (obj, args) =>
        {
            if (UpdatePlugin)
            {
                InitManager.UpdatePlugin();
                UpdatePlugin = false;
            }
        };
    }

    public void OnDestroy()
    {
        Config.Save();
    }

    internal void SetUpConfigEntries()
    {
        Log.Verbosity = Config.BindConfigInOrder(
            MainSection,
            "Verbosity",
            LogLevel.Low,
            "Low will log basic information about the mod. Medium will log information that " +
            "is useful for troubleshooting. High will log a lot of information, do not set " +
            "it to this without good reason as it will slow Down your game.",
            synced: false
        );

        hoverInfoEnabled = Config.BindConfigInOrder(
            MainSection,
            "HoverInfo",
            true,
            "Set to true/enabled to show terrain height when using square terrain tools."
        );

        enableRadiusModifier = Config.BindConfigInOrder(
            RadiusSection,
            "Radius Modifier",
            true,
            "Set to true/enabled to allow modifying the radius of terrain tools using the scroll wheel. " +
            "Note: Radius cannot be changed on square terraforming tools."
        );

        radiusModKey = Config.BindConfigInOrder(
            RadiusSection,
            "Adjust Radius Key",
            KeyCode.LeftAlt,
            "Modifier key that must be held down when using scroll wheel to change the radius of terrain tools.",
            synced: false
        );

        radiusScrollScale = Config.BindConfigInOrder(
            RadiusSection,
            "Radius Scroll Speed",
            0.1f,
            "How much each tick of movement from the scroll wheel will change radius size."
            + " Larger magnitude means the radius will faster."
            + " Negative numbers will reverse the scroll direction to adjust the radius.",
            new AcceptableValueRange<float>(-1f, 1f),
            synced: false
        );

        maxRadius = Config.BindConfigInOrder(
            RadiusSection,
            "Max Radius",
            10f,
            "Maximum radius of terrain tools.",
            new AcceptableValueRange<float>(4f, 20f)
        );

        enableSharpnessModifier = Config.BindConfigInOrder(
            SharpnessSection,
            "Sharpness Modifier",
            true,
            "Set to true/enabled to allow modifying the hardness of terrain tools using the scroll wheel. " +
            "Note: Sharpness cannot be changed on square terraforming tools and tools that do not alter " +
            "ground height do not have a hardness."
        );

        hardnessModKey = Config.BindConfigInOrder(
            SharpnessSection,
            "Adjust Sharpness Key",
            KeyCode.LeftControl,
            "Modifier key that must be held down when using scroll wheel to change the hardness of terrain tools.",
            synced: false
        );

        hardnessScrollScale = Config.BindConfig(
            SharpnessSection,
            "Sharpness Scroll Speed",
            0.1f,
            "How much each tick of movement from the scroll wheel will change sharpness."
            + " Larger magnitude means the sharpness will faster."
            + " Negative numbers will reverse the scroll direction to adjust the sharpness.",
            new AcceptableValueRange<float>(-1f, 1f),
            synced: false
        );

        enableShovel = Config.BindConfigInOrder(
            ShovelSection,
            "Shovel",
            true,
            "Whether the shovel is craftable in-game. If Enabled then crafting is allowed."
            + " If Disabled then crafting new shovels is prevented but existing shovels are untouched."
        );
        enableShovel.SettingChanged += SetUpdatePlugin;

        foreach (string key in ToolConfigs.ToolConfigsMap.Keys)
        {
            ToolDB toolDB = ToolConfigs.ToolConfigsMap[key];

            ConfigEntry<bool> configEntry = Config.BindConfigInOrder(
                GetSectionName(toolDB.pieceTable),
                key,
                true,
                "Set to true/enabled to add this terrain tool. Set to false/disabled to remove it."
            );
            configEntry.SettingChanged += SetUpdatePlugin;
            ToolConfigEntries.Add(key, configEntry);
        }
    }

    /// <summary>
    ///     Event delegate to set flag that settings have been changed
    ///     that require updating plugin.
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="e"></param>
    private void SetUpdatePlugin(object obj, EventArgs e)
    {
        UpdatePlugin = !UpdatePlugin || UpdatePlugin;
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
    internal static bool IsVerbosityLow => Verbosity.Value >= LogLevel.Low;
    internal static bool IsVerbosityMedium => Verbosity.Value >= LogLevel.Medium;
    internal static bool IsVerbosityHigh => Verbosity.Value >= LogLevel.High;

    #endregion Verbosity

    private static ManualLogSource logSource;

    internal static void Init(ManualLogSource logSource)
    {
        Log.logSource = logSource;
    }

    internal static void LogDebug(object data) => logSource.LogDebug(data);

    internal static void LogError(object data) => logSource.LogError(data);

    internal static void LogFatal(object data) => logSource.LogFatal(data);

    internal static void LogMessage(object data) => logSource.LogMessage(data);

    internal static void LogWarning(object data) => logSource.LogWarning(data);

    internal static void LogInfo(object data, LogLevel level = LogLevel.Low)
    {
        if (Verbosity is null || VerbosityLevel >= level)
        {
            logSource.LogInfo(data);
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
            if (!child) { continue; }

            LogInfo($" - {child.name}");
            foreach (Component compo in child.GetComponents<Component>())
            {
                LogComponent(compo);
            }
        }
    }

    internal static void LogComponent(Component compo)
    {
        if (!compo) { return; }
        try
        {
            LogInfo($"--- {compo.GetType().Name}: {compo.name} ---");
        }
        catch (Exception ex)
        {
            Log.LogError(ex.ToString());
            Log.LogWarning("Could not get type name for component!");
            return;
        }

        try
        {
            List<PropertyInfo> properties = AccessTools.GetDeclaredProperties(compo.GetType());
            foreach (PropertyInfo property in properties)
            {
                try
                {
                    LogInfo($" - {property.Name} = {property.GetValue(compo)}");
                }
                catch (Exception ex)
                {
                    Log.LogError(ex.ToString());
                    Log.LogWarning($"Could not get property: {property.Name} for component!");
                }
            }
        }
        catch (Exception ex)
        {
            Log.LogError(ex.ToString());
            Log.LogWarning("Could not get properties for component!");
        }

        try
        {

            List<FieldInfo> fields = AccessTools.GetDeclaredFields(compo.GetType()); ;
            foreach (FieldInfo field in fields)
            {
                try
                {
                    LogInfo($" - {field.Name} = {field.GetValue(compo)}");
                }
                catch (Exception ex)
                {
                    Log.LogError(ex.ToString());
                    Log.LogWarning($"Could not get field: {field.Name} for component!");
                }
            }
        }
        catch (Exception ex)
        {
            Log.LogError(ex.ToString());
            Log.LogWarning("Could not get fields for component!");
        }

    }
}
