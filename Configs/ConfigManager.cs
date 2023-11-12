﻿// Ignore Spelling: Jotunn

using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using TerrainTools.Extensions;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using HoeRadius;

namespace TerrainTools.Configs
{
    internal class ConfigManager
    {
        private static string ConfigFileName;
        private static string ConfigFileFullPath;

        private static ConfigFile configFile;
        private static BaseUnityPlugin ConfigurationManager;
        private const string ConfigManagerGUID = "com.bepis.bepinex.configurationmanager";

        private static readonly string MainSection = SetStringPriority("Global", 3);
        private static readonly string RadiusSection = SetStringPriority("Radius", 2);
        private static readonly string ToolsSection = SetStringPriority("Tools", 1);

        #region Events

        /// <summary>
        ///     Event triggered after a the in-game configuration manager is closed.
        /// </summary>
        internal static event Action OnConfigWindowClosed;

        /// <summary>
        ///     Safely invoke the <see cref="OnConfigWindowClosed"/> event
        /// </summary>
        private static void InvokeOnConfigWindowClosed()
        {
            OnConfigWindowClosed?.SafeInvoke();
        }

        /// <summary>
        ///     Event triggered after the file watcher reloads the configuration file.
        /// </summary>
        internal static event Action OnConfigFileReloaded;

        /// <summary>
        ///     Safely invoke the <see cref="OnConfigFileReloaded"/> event
        /// </summary>
        private static void InvokeOnConfigFileReloaded()
        {
            OnConfigFileReloaded?.SafeInvoke();
        }

        #endregion Events

        #region LoggerLevel

        internal enum LoggerLevel
        {
            Low = 0,
            Medium = 1,
            High = 2,
        }

        internal static ConfigEntry<LoggerLevel> Verbosity { get; private set; }
        internal static LoggerLevel VerbosityLevel => Verbosity.Value;
        internal static bool IsVerbosityLow => Verbosity.Value >= LoggerLevel.Low;
        internal static bool IsVerbosityMedium => Verbosity.Value >= LoggerLevel.Medium;
        internal static bool IsVerbosityHigh => Verbosity.Value >= LoggerLevel.High;

        #endregion LoggerLevel

        #region BindConfig

        internal static ConfigEntry<T> BindConfig<T>(
            string section,
            string name,
            T value,
            string description,
            AcceptableValueBase acceptVals = null,
            bool synced = true
        )
        {
            string extendedDescription = GetExtendedDescription(description, synced);
            ConfigEntry<T> configEntry = configFile.Bind(
                section,
                name,
                value,
                new ConfigDescription(
                    extendedDescription,
                    acceptVals,
                    synced ? AdminConfig : ClientConfig
                )
            );
            return configEntry;
        }

        private static readonly ConfigurationManagerAttributes AdminConfig = new() { IsAdminOnly = true };
        private static readonly ConfigurationManagerAttributes ClientConfig = new() { IsAdminOnly = false };
        private const char ZWS = '\u200B';

        /// <summary>
        ///     Prepends Zero-Width-Space to set ordering of configuration sections
        /// </summary>
        /// <param name="sectionName">Section name</param>
        /// <param name="priority">Number of ZWS chars to prepend</param>
        /// <returns></returns>
        internal static string SetStringPriority(string sectionName, int priority)
        {
            if (priority == 0) { return sectionName; }
            return new string(ZWS, priority) + sectionName;
        }

        internal static string GetExtendedDescription(string description, bool synchronizedSetting)
        {
            return description + (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]");
        }

        #endregion BindConfig

        // Tool Configs
        // Need to be able to keep a reference to each tool I add
        // So I can enable and disable them as needed based on config changes
        // Should set up an InitManager to deal with that

        // Radius configs
        internal static ConfigEntry<bool> UseScrollWheel;

        internal static ConfigEntry<KeyCode> ScrollModKey;
        internal static ConfigEntry<KeyCode> IncreaseHotKey;
        internal static ConfigEntry<KeyCode> DecreaseHotKey;
        internal static ConfigEntry<float> ScrollWheelScale;
        internal static ConfigEntry<float> HotkeyScale;

        private static float lastOriginalRadius;
        private static float lastModdedRadius;
        private static float lastTotalDelta;

        internal static void Init(string GUID, ConfigFile config)
        {
            configFile = config;
            configFile.SaveOnConfigSet = false;
            ConfigFileName = GUID + ".cfg";
            ConfigFileFullPath = string.Concat(Paths.ConfigPath, Path.DirectorySeparatorChar, ConfigFileName);
        }

        internal static void SetUpConfig()
        {
            Verbosity = BindConfig(
                MainSection,
                "Verbosity",
                LoggerLevel.Low,
                "Low will log basic information about the mod. Medium will log information that " +
                "is useful for troubleshooting. High will log a lot of information, do not set " +
                "it to this without good reason as it will slow Down your game.",
                synced: false
            );

            UseScrollWheel = BindConfig(
                RadiusSection,
                "UseScrollWheel",
                true,
                "Use scroll wheel to modify radius"
            );

            ScrollWheelScale = BindConfig(
                RadiusSection,
                "ScrollWheelScale",
                0.1f,
                "Scroll wheel change scale",
                new AcceptableValueRange<float>(0.05f, 2f)
            );

            ScrollModKey = BindConfig(
                RadiusSection,
                "ScrollModKey",
                KeyCode.LeftAlt,
                "Modifer key to allow scroll wheel change. Use https://docs.unity3d.com/Manual/class-InputManager.html"
            );

            IncreaseHotKey = BindConfig(
                RadiusSection,
                "IncreaseHotKey",
                KeyCode.None,
                "Hotkey to increase radius. Use https://docs.unity3d.com/Manual/class-InputManager.html"
            );

            DecreaseHotKey = BindConfig(
                RadiusSection,
                "DecreaseHotKey",
                KeyCode.None,
                "Hotkey to decrease radius. Use https://docs.unity3d.com/Manual/class-InputManager.html"
            );

            HotkeyScale = BindConfig(
                RadiusSection,
                "HotkeyScale",
                0.1f,
                "Hotkey change scale",
                new AcceptableValueRange<float>(0.05f, 2f)
            );

            Save();
        }

        #region Saving

        /// <summary>
        ///     Sets SaveOnConfigSet to false and returns
        ///     the Value prior to calling this method.
        /// </summary>
        /// <returns></returns>
        private static bool DisableSaveOnConfigSet()
        {
            var val = configFile.SaveOnConfigSet;
            configFile.SaveOnConfigSet = false;
            return val;
        }

        /// <summary>
        ///     Set the Value for the SaveOnConfigSet field.
        /// </summary>
        /// <param name="value"></param>
        internal static void SaveOnConfigSet(bool value)
        {
            configFile.SaveOnConfigSet = value;
        }

        /// <summary>
        ///     Save config file to disk.
        /// </summary>
        internal static void Save()
        {
            configFile.Save();
        }

        #endregion Saving

        #region FileWatcher

        internal static void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReloadConfigFile;
            watcher.Created += ReloadConfigFile;
            watcher.Renamed += ReloadConfigFile;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private static void ReloadConfigFile(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) { return; }
            try
            {
                Log.LogInfo("Reloading config file");

                // turn off saving on config entry set
                var saveOnConfigSet = DisableSaveOnConfigSet();
                configFile.Reload();
                SaveOnConfigSet(saveOnConfigSet); // reset config saving state
                InvokeOnConfigFileReloaded(); // fire event
            }
            catch
            {
                Log.LogError($"There was an issue loading your {ConfigFileName}");
                Log.LogError("Please check your config entries for spelling and format!");
            }
        }

        #endregion FileWatcher

        #region ConfigWindow

        /// <summary>
        ///     Checks for in-game configuration manager and
        ///     sets Up OnConfigWindowClosed event if it is present
        /// </summary>
        internal static void CheckForConfigManager()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                return;
            }

            if (Chainloader.PluginInfos.TryGetValue(ConfigManagerGUID, out PluginInfo configManagerInfo) && configManagerInfo.Instance)
            {
                ConfigurationManager = configManagerInfo.Instance;
                Log.LogDebug("Configuration manager found, hooking DisplayingWindowChanged");

                EventInfo eventinfo = ConfigurationManager.GetType().GetEvent("DisplayingWindowChanged");

                if (eventinfo != null)
                {
                    Action<object, object> local = new(OnConfigManagerDisplayingWindowChanged);
                    Delegate converted = Delegate.CreateDelegate(
                        eventinfo.EventHandlerType,
                        local.Target,
                        local.Method
                    );
                    eventinfo.AddEventHandler(ConfigurationManager, converted);
                }
            }
        }

        private static void OnConfigManagerDisplayingWindowChanged(object sender, object e)
        {
            PropertyInfo pi = ConfigurationManager.GetType().GetProperty("DisplayingWindow");
            bool ConfigurationManagerWindowShown = (bool)pi.GetValue(ConfigurationManager, null);

            if (!ConfigurationManagerWindowShown)
            {
                InvokeOnConfigWindowClosed();
            }
        }

        #endregion ConfigWindow
    }
}