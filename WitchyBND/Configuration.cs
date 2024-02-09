using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CommandLine;
using Microsoft.Extensions.Configuration;
using WitchyBND.CliModes;
using WitchyBND.Parsers;
using WitchyLib;

namespace WitchyBND;

public static class Configuration
{
    public static bool IsTest { get; set; }

    public static bool IsDebug
    {
        get
        {
#if (DEBUG)
            return true;
#endif
            return false;
        }
    }

    public class WitchyConfigValues
    {
        public bool Bnd { get; set; }
        public bool Dcx { get; set; }
        public bool ParamDefaultValues { get; set; }
        public bool Recursive { get; set; }
        public ushort EndDelay { get; set; }
        public bool PauseOnError { get; set; }
        public bool Parallel { get; set; }
        public bool Expert { get; set; }
        public bool Offline { get; set; }

        public bool TaeFolder { get; set; }
        public Dictionary<DeferFormat, DeferFormatConfiguration> DeferTools { get; set; } = new();

        public DateTime? LastUpdateCheckTime { get; set; }
    }

    public class WitchyArgValues
    {
        public bool UnpackOnly { get; set; }
        public bool RepackOnly { get; set; }
        public bool Passive { get; set; }
        public string? Location { get; set; }
    }

    private static WitchyConfigValues _values;

    public static WitchyArgValues Args;

    public static bool Bnd
    {
        get => _values.Bnd;
        set => _values.Bnd = value;
    }

    public static bool Dcx
    {
        get => _values.Dcx;
        set => _values.Dcx = value;
    }

    public static bool ParamDefaultValues
    {
        get => _values.ParamDefaultValues;
        set => _values.ParamDefaultValues = value;
    }

    public static bool PauseOnError
    {
        get => _values.PauseOnError;
        set => _values.PauseOnError = value;
    }

    public static bool Recursive
    {
        get => _values.Recursive;
        set => _values.Recursive = value;
    }

    public static ushort EndDelay
    {
        get => _values.EndDelay;
        set => _values.EndDelay = value;
    }

    public static bool Expert
    {
        get => _values.Expert;
        set => _values.Expert = value;
    }

    public static bool Parallel
    {
        get => _values.Parallel;
        set => _values.Parallel = value;
    }

    public static bool Offline
    {
        get => _values.Offline;
        set => _values.Offline = value;
    }

    public static bool TaeFolder
    {
        get => _values.TaeFolder;
        set => _values.TaeFolder = value;
    }

    public static Dictionary<DeferFormat, DeferFormatConfiguration> DeferTools
    {
        get => _values.DeferTools;
        set => _values.DeferTools = value;
    }

    public static DateTime? LastUpdateCheckTime
    {
        get => _values.LastUpdateCheckTime;
        set => _values.LastUpdateCheckTime = value;
    }

    public static void ReplaceConfig(IConfigurationRoot config)
    {
        _values = config.Get<WitchyConfigValues>();
    }

    private static string GetConfigLocation(string path)
    {
        return WBUtil.GetExeLocation(path);
    }

    static Configuration()
    {
        _values = new WitchyConfigValues();
        Args = new WitchyArgValues();
        IConfigurationRoot config = new ConfigurationBuilder()
            .AddJsonFile(GetConfigLocation("appsettings.json"), true)
            .AddJsonFile(GetConfigLocation("appsettings.user.json"), true)
            .AddJsonFile(GetConfigLocation("appsettings.override.json"), true)
            .Build();
        ;
        _values = config.Get<WitchyConfigValues>();
    }

    public static void UpdateConfiguration()
    {
        var configuration = _values;
        // instead of updating appsettings.json file directly I will just write the part I need to update to appsettings.MyOverrides.json
        // .Net Core in turn will read my overrides from appsettings.MyOverrides.json file
        const string overrideFileName = "appsettings.user.json";
        var newConfig = JsonSerializer.Serialize(configuration, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(GetConfigLocation(overrideFileName), newConfig);
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

    [Option('m', "mode", HelpText = "Toggle the mode to use. Options are \"Parse\", \"Watch\" and \"Config\".", Default = CliMode.Parse)]
    public CliMode Mode { get; set; }

    [Option('p', "passive",
        HelpText =
            "Will not prompt the user for any input or cause any delays. Suited for automatic execution in scripts.")]
    public bool Passive { get; set; }

    [Option('l', "location",
        HelpText = "Specifies a path to unpack binders to. Enter \"prompt\" to open a folder dialog instead.")]
    public string Location { get; set; }

    [Option('a', "param-default-values",
        HelpText =
            "Whether serialized PARAM will separately store default values for param rows. Provide \"true\" or \"false\".")]
    public bool? ParamDefaultValues { get; set; }

    [Option('d', "dcx", HelpText = "Simply decompress DCX files instead of unpacking their content.")]
    public bool Dcx { get; set; }

    [Option('b', "bnd",
        HelpText = "Perform basic unpacking of BND instead of using special Witchy methods, where present")]
    public bool Bnd { get; set; }

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