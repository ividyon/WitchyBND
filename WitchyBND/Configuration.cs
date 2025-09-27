using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using WitchyBND.Parsers;
using WitchyLib;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace WitchyBND;

public interface IStoredConfig
{
    public bool Bnd { get; set; }
    public bool Dcx { get; set; }
    public float ParamDefaultValueThreshold { get; set; }

    public WPARAM.CellStyle ParamCellStyle { get; set; }
    public bool Recursive { get; set; }
    public ushort EndDelay { get; set; }
    public bool PauseOnError { get; set; }
    public bool Parallel { get; set; }
    public bool Expert { get; set; }
    public bool Offline { get; set; }

    public bool TaeFolder { get; set; }
    public Dictionary<DeferFormat, DeferConfig> DeferTools { get; set; }

    public bool Flexible { get; set; }

    public WBUtil.BackupMethod BackupMethod { get; set; }
}

public interface IStoredOnlyConfig
{
    public DateTime LastUpdateCheck { get; set; }

    public Version SkipUpdateVersion { get; set; }

    public Version LastLaunchedVersion { get; set; }
}

public interface ITempConfig
{
    public bool UnpackOnly { get; set; }
    public bool RepackOnly { get; set; }
    public bool Passive { get; set; }

    public bool Silent { get; set; }
    public string? Location { get; set; }
}

public static class Configuration
{
    public static bool IsTest { get; set; }

    public static bool IsDebug => Debugger.IsAttached;

    public class StoredConfig : IStoredConfig, IStoredOnlyConfig
    {
        public bool Bnd { get; set; }
        public bool Dcx { get; set; }
        public float ParamDefaultValueThreshold { get; set; }

        public WPARAM.CellStyle ParamCellStyle { get; set; }
        public bool Recursive { get; set; }
        public ushort EndDelay { get; set; }
        public bool PauseOnError { get; set; }
        public bool Parallel { get; set; }
        public bool Expert { get; set; }
        public bool Offline { get; set; }

        public bool TaeFolder { get; set; }
        public Dictionary<DeferFormat, DeferConfig> DeferTools { get; set; } = new();

        public bool Flexible { get; set; }

        public DateTime LastUpdateCheck { get; set; }

        public Version SkipUpdateVersion { get; set; }

        public Version LastLaunchedVersion { get; set; }

        public WBUtil.BackupMethod BackupMethod { get; set; }

        public bool GitBackup { get; set; }
    }

    public class ActiveConfig : IStoredConfig, ITempConfig
    {
        public bool Bnd { get; set; }
        public bool Dcx { get; set; }
        public float ParamDefaultValueThreshold { get; set; }
        public WPARAM.CellStyle ParamCellStyle { get; set; }
        public bool Recursive { get; set; }
        public ushort EndDelay { get; set; }
        public bool PauseOnError { get; set; }
        public bool Parallel { get; set; }
        public bool Expert { get; set; }
        public bool Offline { get; set; }
        public bool TaeFolder { get; set; }
        public Dictionary<DeferFormat, DeferConfig> DeferTools { get; set; }
        public bool Flexible { get; set; }

        // The following are args-only or otherwise temporary
        public bool UnpackOnly { get; set; }
        public bool RepackOnly { get; set; }
        public bool Passive { get; set; }
        public bool Silent { get; set; }
        public string? Location { get; set; }
        public WBUtil.BackupMethod BackupMethod { get; set; }

        public bool GitBackup { get; set; }
    }

    public static StoredConfig Stored;
    public static StoredConfig Default;
    public static ActiveConfig Active;

    public static string AppDataDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WitchyBND");

    public static OSPlatform Platform { get; set; }
    public static bool ParamDefaultValues => Active.ParamDefaultValueThreshold > 0f;

    public static void SwapOutConfig(IConfigurationRoot config)
    {
        Stored = config.Get<StoredConfig>();
        ActivateStoredConfiguration();
    }

    static Configuration()
    {
        Active = new ActiveConfig();

        if (!Directory.Exists(AppDataDirectory))
            Directory.CreateDirectory(AppDataDirectory);

        Platform = OSPlatform.Windows;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Platform = OSPlatform.Linux;

        LoadConfiguration();
    }

    public static void LoadConfiguration()
    {
        var builder = new ConfigurationBuilder();
        bool breakOut = false;
        string userSettingsPath = Path.Combine(AppDataDirectory, "appsettings.user.json");
        string defaultSettingsPath = WBUtil.GetExeLocation("appsettings.json");
        string debugSettingsPath = WBUtil.GetExeLocation("appsettings.debug.json");
        string overrideSettingsPath = WBUtil.GetExeLocation("appsettings.override.json");
        while (!breakOut)
        {
            try
            {
                IConfigurationRoot config;
                builder.AddJsonFile(defaultSettingsPath, true);

                var defaultConfig = builder.Build();
                Default = defaultConfig.Get<StoredConfig>() ?? new StoredConfig();
                if (IsDebug || IsTest)
                    builder.AddJsonFile(debugSettingsPath, true);
                else
                    builder.AddJsonFile(userSettingsPath, true);

                builder.AddJsonFile(overrideSettingsPath, true);

                breakOut = true;
                config = builder.Build();
                Stored = config.Get<StoredConfig>() ?? new StoredConfig();
                ActivateStoredConfiguration();
            }
            catch (JsonReaderException e)
            {
                if (IsDebug || IsTest)
                {
                    if (File.Exists(debugSettingsPath))
                        File.Delete(debugSettingsPath);
                }
                else
                {
                    if (File.Exists(userSettingsPath))
                        File.Delete(userSettingsPath);
                }
                breakOut = false;
            }
        }
    }

    public static void SaveConfiguration()
    {
        var newStored = JsonSerializer.Serialize(Stored, new JsonSerializerOptions { WriteIndented = true });
        if (IsDebug || IsTest)
            File.WriteAllText(WBUtil.GetExeLocation("appsettings.debug.json"), newStored);
        else
            File.WriteAllText(Path.Combine(AppDataDirectory, "appsettings.user.json"), newStored);
    }

    public static void ActivateStoredConfiguration(StoredConfig? stored = null)
    {
        stored ??= Stored;
        Active.Bnd = stored.Bnd;
        Active.Dcx = stored.Dcx;
        Active.ParamDefaultValueThreshold = stored.ParamDefaultValueThreshold;
        Active.ParamCellStyle = stored.ParamCellStyle;
        Active.Recursive = stored.Recursive;
        Active.EndDelay = stored.EndDelay;
        Active.PauseOnError = stored.PauseOnError;
        Active.Parallel = stored.Parallel;
        Active.Expert = stored.Expert;
        Active.Offline = stored.Offline;
        Active.TaeFolder = stored.TaeFolder;
        Active.DeferTools = stored.DeferTools;
        Active.Flexible = stored.Flexible;
        Active.BackupMethod = stored.BackupMethod;
        Active.GitBackup = stored.GitBackup;
    }
}

public enum CliMode
{
    Parse,
    Config,
    Watch
}

public class CliOptions
{
    // [Option('v', "verbose", Group = "verbosity", Default = false, HelpText = "Set output to verbose messages.")]
    // public bool Verbose { get; set; }
    //
    // [Option('q', "quiet", Group = "verbosity", Default = false,
    //     HelpText = "Set output to quiet, reporting only errors.")]
    // public bool Quiet { get; set; }

    [Option('c', "recursive", HelpText = "Attempt to process files contained within binders recursively.")]
    public bool Recursive { get; set; }

    [Option('e', "parallel", HelpText = "Runs operations parallelized")]
    public bool Parallel { get; set; }

    [Option('t', "singlethread", HelpText = "Runs operations single-threaded")]
    public bool SingleThread { get; set; }

    [Option('m', "mode", HelpText = "Toggle the mode to use. Options are \"Parse\", \"Watch\" and \"Config\".",
        Default = CliMode.Parse)]
    public CliMode Mode { get; set; }

    [Option('p', "passive",
        HelpText =
            "Will not prompt the user for any input or cause any delays. Suited for automatic execution in scripts.")]
    public bool Passive { get; set; }

    [Option('s', "silent",
        HelpText = "Will not print any console output. Inherently sets 'passive' to true.")]
    public bool Silent { get; set; }

    [Option('l', "location",
        HelpText = "Specifies a path to unpack binders to. Enter \"prompt\" to open a folder dialog instead.")]
    public string Location { get; set; }

    [Option('d', "dcx", HelpText = "Simply decompress DCX files instead of unpacking their content.")]
    public bool Dcx { get; set; }

    [Option('b', "bnd",
        HelpText = "Perform basic unpacking of BND instead of using special Witchy methods, where present")]
    public bool BasicBnd { get; set; }

    [Option('a', "special",
        HelpText = "Force special Witchy methods of unpacking BND, where present")]
    public bool SpecializedBnd { get; set; }

    [Option('f', "flexible",
        HelpText = "Ignore assertions and other issues while unpacking, to counteract file tampering")]
    public bool Flexible { get; set; }

    [Option('r', "repack", HelpText = "Only perform repack processing, no unpacking.", SetName = "pack")]
    public bool RepackOnly { get; set; }

    [Option('u', "unpack", HelpText = "Only perform unpack processing, no repacking.", SetName = "pack")]
    public bool UnpackOnly { get; set; }

    [Option('h', "help", HelpText = "Display this help screen.")]
    public bool Help { get; set; }

    [Option('v', "version", HelpText = "Display version information.")]
    public bool Version { get; set; }

    [Value(0, HelpText = "The paths that should be parsed by Witchy.")]
    public IEnumerable<string> Paths { get; set; }
}