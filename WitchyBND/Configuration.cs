﻿using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace WitchyBND;

public static class Configuration
{
    public class WitchyConfigValues
    {
        public bool Bnd { get; set; }
        public bool Dcx { get; set; }
    }

    private static WitchyConfigValues _values;

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

    static Configuration()
    {
        _values = new WitchyConfigValues();

        IConfigurationRoot config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.user.json", true)
            .Build();
        _values = config.Get<WitchyConfigValues>();
    }

    public static void UpdateConfiguration()
    {
        var configuration = _values;
        // instead of updating appsettings.json file directly I will just write the part I need to update to appsettings.MyOverrides.json
        // .Net Core in turn will read my overrides from appsettings.MyOverrides.json file
        const string overrideFileName = "appsettings.user.json";
        var newConfig = JsonSerializer.Serialize(configuration, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(overrideFileName, newConfig);
    }
}