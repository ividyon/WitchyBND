using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using WitchyLib;

namespace WitchyBND;

public static class Configuration
{
    public static bool IsTest { get; set; }

    public class WitchyConfigValues
    {
        public bool Bnd { get; set; }
        public bool Dcx { get; set; }
        public bool ParamDefaultValues { get; set; }
        public bool Recursive { get; set; }
        public ushort EndDelay { get; set; }
        public bool PauseOnError { get; set; }

        public bool Expert { get; set; }
    }

    public class WitchyArgValues
    {
        public bool UnpackOnly { get; set; }

        public bool RepackOnly { get; set; }

        public bool Passive { get; set; }

        public string Location { get; set; }
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
            .Build();;
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