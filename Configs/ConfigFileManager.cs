using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using TerrainTools.Extensions;

namespace TerrainTools.Configs;


/// <summary>
///     Extends ConfigFile with a convenience method to bind config entries with less boilerplate code 
///     and explicitly expose commonly used configuration manager attributes.
/// </summary>
public static class ConfigFileManager
{
    internal static string ConfigFileName;
    internal static string ConfigFileFullPath;
    internal static DateTime lastRead = DateTime.MinValue;

    private static readonly Dictionary<string, int> _sectionToSectionNumber = [];

    private static string GetOrderedSectionName(string section)
    {
        if (!_sectionToSectionNumber.TryGetValue(section, out int number))
        {
            number = _sectionToSectionNumber.Count + 1;
            _sectionToSectionNumber[section] = number;
        }
        return $"{number} - {section}";
    }

    private static readonly Dictionary<string, int> _sectionToSettingOrder = [];

    private static int GetSettingOrder(string section)
    {
        if (!_sectionToSettingOrder.TryGetValue(section, out int order))
        {
            order = 0;
        }

        _sectionToSettingOrder[section] = order - 1;
        return order;
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


    public static void Init(this ConfigFile configFile, string GUID, bool saveOnConfigSet = false)
    {
        configFile.SaveOnConfigSet = saveOnConfigSet;
        ConfigFileName = GUID + ".cfg";
        ConfigFileFullPath = Path.Combine(Paths.ConfigPath, ConfigFileName);
    }


    /// <summary>
    ///     Sets SaveOnConfigSet to false and returns
    ///     the Value prior to calling this method.
    /// </summary>
    /// <returns></returns>
    public static bool DisableSaveOnConfigSet(this ConfigFile configFile)
    {
        bool val = configFile.SaveOnConfigSet;
        configFile.SaveOnConfigSet = false;
        return val;
    }


    /// <summary>
    ///     Bind a new config entry to the config file and modify description to state whether the config entry is synced or not.
    /// </summary>
    /// <typeparam name="T">Type of the value the config entry holds.</typeparam>
    /// <param name="configFile">Configuration file to bind the config entry to.</param>
    /// <param name="section">Configuration file section to list the config entry in.</param>
    /// <param name="name">Display name of the config entry.</param>
    /// <param name="value">Default value of the config entry.</param>
    /// <param name="description">Plain text description of the config entry to display as hover text in configuration manager.</param>
    /// <param name="acceptVals">Acceptable values for config entry as an AcceptableValueRange, AcceptableValueList, or custom subclass.</param>
    /// <param name="synced">Whether the config entry IsAdminOnly and should be synced with server.</param>
    /// <param name="sectionOrder">Whether to number the section names based on the order they are bound to the config.</param>
    /// <param name="settingOrder">Whether to order the settings in each section based on the order they are bound to the config.</param>
    /// <param name="drawer">Custom setting editor (OnGUI code that replaces the default editor provided by ConfigurationManager).</param>
    /// <param name="configAttributes">Optional config manager attributes for additional user specified functionality. Any optional fields specified by the arguments of BindConfig will be overwritten by the parameters passed to BindConfig.</param>
    /// <returns>ConfigEntry bound to the config file.</returns>
    public static ConfigEntry<T> BindConfigInOrder<T>(
        this ConfigFile configFile,
        string section,
        string name,
        T value,
        string description,
        AcceptableValueBase acceptVals = null,
        bool synced = true,
        bool sectionOrder = true,
        bool settingOrder = true,
        Action<ConfigEntryBase> drawer = null,
        ConfigurationManagerAttributes configAttributes = null
    )
    {
        section = sectionOrder ? GetOrderedSectionName(section) : section;
        int order = settingOrder ? GetSettingOrder(section) : 0;
        configAttributes ??= new ConfigurationManagerAttributes();
        configAttributes.Order = order;
        return configFile.BindConfig(section, name, value, description, acceptVals, synced, drawer, configAttributes);

    }

    /// <summary>
    ///     Bind a new config entry to the config file and modify description to state whether the config entry is synced or not.
    /// </summary>
    /// <typeparam name="T">Type of the value the config entry holds.</typeparam>
    /// <param name="configFile">Configuration file to bind the config entry to.</param>
    /// <param name="section">Configuration file section to list the config entry in.</param>
    /// <param name="name">Display name of the config entry.</param>
    /// <param name="value">Default value of the config entry.</param>
    /// <param name="description">Plain text description of the config entry to display as hover text in configuration manager.</param>
    /// <param name="acceptVals">Acceptable values for config entry as an AcceptableValueRange, AcceptableValueList, or custom subclass.</param>
    /// <param name="synced">Whether the config entry IsAdminOnly and should be synced with server.</param>
    /// <param name="drawer">Custom setting editor (OnGUI code that replaces the default editor provided by ConfigurationManager).</param>
    /// <param name="configAttributes">Optional config manager attributes for additional user specified functionality. Any optional fields specified by the arguments of BindConfig will be overwritten by the parameters passed to BindConfig.</param>
    /// <returns>ConfigEntry bound to the config file.</returns>
    public static ConfigEntry<T> BindConfig<T>(
        this ConfigFile configFile,
        string section,
        string name,
        T value,
        string description,
        AcceptableValueBase acceptVals = null,
        bool synced = true,
        Action<ConfigEntryBase> drawer = null,
        ConfigurationManagerAttributes configAttributes = null
    )
    {
        string extendedDescription = GetExtendedDescription(description, synced);

        configAttributes ??= new ConfigurationManagerAttributes();
        configAttributes.IsAdminOnly = synced;
        if (drawer != null)
        {
            configAttributes.CustomDrawer = drawer;
        }

        ConfigEntry<T> configEntry = configFile.Bind(
            section,
            name,
            value,
            new ConfigDescription(
                extendedDescription,
                acceptVals,
                configAttributes
            )
        );
        return configEntry;
    }

    internal static string GetExtendedDescription(string description, bool synchronizedSetting)
    {
        // these two hardcoded strings should probably be localized
        return description + (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]");
    }

    internal static void SetupWatcher(this ConfigFile configFile)
    {
        var watcher = new FileSystemWatcher(Paths.ConfigPath, ConfigFileName);
        watcher.Changed += configFile.ReloadConfigFile;
        watcher.Created += configFile.ReloadConfigFile;
        watcher.Renamed += configFile.ReloadConfigFile;
        watcher.IncludeSubdirectories = true;
        watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        watcher.EnableRaisingEvents = true;
    }

    internal static void ReloadConfigFile(this ConfigFile configFile, object sender, FileSystemEventArgs eventArgs)
    {

        if (!File.Exists(ConfigFileFullPath))
        {
            return;
        }

        try
        {
            DateTime lastWriteTime = File.GetLastWriteTime(eventArgs.FullPath);
            if (lastRead != lastWriteTime)
            {
                Log.LogInfo("Reloading config file");
                bool saveOnConfigSet = configFile.DisableSaveOnConfigSet(); // turn off saving on config entry set
                configFile.Reload();
                configFile.SaveOnConfigSet = saveOnConfigSet; // reset config saving state

                InvokeOnConfigFileReloaded(); // fire event
                lastRead = lastWriteTime;
            }
        }
        catch
        {
            Log.LogError($"There was an issue loading your {ConfigFileName}");
            Log.LogError("Please check your config entries for spelling and format!");
        }
    }
}
