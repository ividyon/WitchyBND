using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using PPlus;
using PPlus.Controls;

namespace WitchyBND.CliModes;

public static class ConfigMode
{
    private enum ConfigMenuItemType
    {
        ToggleBnd,
        ToggleDcx,
        ToggleRecursive,
        ToggleParamDefaultValues,
        ConfigureDelay,
        TogglePauseOnError,
        Windows,
        Formats,
        Help,
        Exit
    }

    private class ConfigMenuItem
    {
        public ConfigMenuItemType Type { get; set; }
        public string Label { get; set; }
        public string Description { get; set; } = "";
    }

    private static List<ConfigMenuItem> AssembleConfigMenu()
    {
        return new()
        {
            new ConfigMenuItem
            {
                Type = ConfigMenuItemType.ToggleBnd,
                Label = Configuration.Bnd ? "Toggle \"Use specialized BND handlers\" (Enabled)" : "Toggle \"Use specialized BND handlers\" (Disabled)",
                Description =
                    "Enable to extract certain BND types into a custom, user-friendly structure. Disable for standard handling."
            },
            new ConfigMenuItem
            {
                Type = ConfigMenuItemType.ToggleDcx, Label = Configuration.Dcx ? "Toggle \"DCX decompression only\" (Enabled)" : "Toggle \"DCX decompression only\" (Disabled)",
                Description = "Enable to exclusively decompress DCX files and leave their contents intact."
            },
            new ConfigMenuItem
            {
                Type = ConfigMenuItemType.ToggleRecursive, Label = Configuration.Recursive ? "Toggle \"Recursive binder processing\" (Enabled)" : "Toggle \"Recursive binder processing\" (Disabled)",
                Description = "Enable to recursively attempt to process any file inside unpacked binders. Very performance-intensive."
            },
            new ConfigMenuItem
            {
                Type = ConfigMenuItemType.ToggleParamDefaultValues, Label = Configuration.ParamDefaultValues ? "Toggle \"Store PARAM field default values\" (Enabled)" : "Toggle \"Store PARAM field default values\" (Disabled)",
                Description = @"Enable to separate the default values of PARAM row fields out from the rows.
Disabling vastly increases XML output size."
            },
            new ConfigMenuItem
            {
                Type = ConfigMenuItemType.TogglePauseOnError, Label = Configuration.PauseOnError ? "Toggle \"Pause on Error\" (Enabled)" : "Toggle \"Pause on Error\" (Disabled)",
                Description = @"Enable to pause the program and require a key press (unless in Passive mode) if it finishes with errors."
            },
            new ConfigMenuItem
            {
                Type = ConfigMenuItemType.ConfigureDelay, Label = $"Configure end delay ({Configuration.EndDelay}ms)"
            },
            new ConfigMenuItem { Type = ConfigMenuItemType.Windows, Label = "Configure Windows integration" },
            new ConfigMenuItem { Type = ConfigMenuItemType.Formats, Label = "View available formats" },
            new ConfigMenuItem { Type = ConfigMenuItemType.Help, Label = "View help screen" },
            new ConfigMenuItem { Type = ConfigMenuItemType.Exit, Label = "Exit" },
        };
    }

    public static void CliConfigMode(CliOptions opt)
    {
        PromptPlus.WriteLine(@"Welcome to WitchyBND!

Launching the program without specifying any files will open this configuration screen.
If you want to unpack or repack a file or directory, you can either:

  a) Drag and drop it into the WitchyBND executable.
  b) Configure the context menu in the following screen,
     then right-click the file or directory and select
     ""WitchyBND"" in the context menu.

Press any key to continue to the configuration screen...");

        if (Configuration.Args.Passive)
            return;

        PromptPlus.ReadKey();
        PromptPlus.Clear();
        while (true)
        {
            PromptPlus.DoubleDash("Configuration menu");
            var select = PromptPlus.Select<ConfigMenuItem>("Choose an option")
                .AddItems(AssembleConfigMenu())
                .TextSelector(a => a.Label)
                .ChangeDescription(a => a.Description)
                .Run();

            if (select.IsAborted) return;

            switch (select.Value.Type)
            {
                case ConfigMenuItemType.ToggleBnd:
                    Configuration.Bnd = !Configuration.Bnd;
                    Configuration.UpdateConfiguration();
                    PromptPlus.WriteLine("Successfully updated the configuration.");
                    PromptPlus.WriteLine(Constants.PressAnyKeyConfiguration);
                    PromptPlus.ReadKey();
                    PromptPlus.Clear();
                    break;
                case ConfigMenuItemType.ToggleDcx:
                    Configuration.Dcx = !Configuration.Dcx;
                    Configuration.UpdateConfiguration();
                    PromptPlus.WriteLine("Successfully updated the configuration.");
                    PromptPlus.WriteLine(Constants.PressAnyKeyConfiguration);
                    PromptPlus.ReadKey();
                    PromptPlus.Clear();
                    break;
                case ConfigMenuItemType.ToggleRecursive:
                    Configuration.Recursive = !Configuration.Recursive;
                    Configuration.UpdateConfiguration();
                    PromptPlus.WriteLine("Successfully updated the configuration.");
                    PromptPlus.WriteLine(Constants.PressAnyKeyConfiguration);
                    PromptPlus.ReadKey();
                    PromptPlus.Clear();
                    break;
                case ConfigMenuItemType.ToggleParamDefaultValues:
                    Configuration.ParamDefaultValues = !Configuration.ParamDefaultValues;
                    Configuration.UpdateConfiguration();
                    PromptPlus.WriteLine("Successfully updated the configuration.");
                    PromptPlus.WriteLine(Constants.PressAnyKeyConfiguration);
                    PromptPlus.ReadKey();
                    PromptPlus.Clear();
                    break;
                case ConfigMenuItemType.TogglePauseOnError:
                    Configuration.PauseOnError = !Configuration.PauseOnError;
                    Configuration.UpdateConfiguration();
                    PromptPlus.WriteLine("Successfully updated the configuration.");
                    PromptPlus.WriteLine(Constants.PressAnyKeyConfiguration);
                    PromptPlus.ReadKey();
                    PromptPlus.Clear();
                    break;
                case ConfigMenuItemType.ConfigureDelay:
                    var input = PromptPlus.Input("Input new delay (in milliseconds)")
                        .AcceptInput(char.IsNumber)
                        .AddValidators(PromptValidators.IsTypeUInt16("Input is not within valid range"))
                        .ValidateOnDemand()
                        .Run();
                    if (!input.IsAborted)
                    {
                        Configuration.EndDelay = Convert.ToUInt16(input.Value);
                        Configuration.UpdateConfiguration();
                        PromptPlus.WriteLine("Successfully updated the configuration.");
                        PromptPlus.WriteLine(Constants.PressAnyKeyConfiguration);
                        PromptPlus.ReadKey();
                    }
                    PromptPlus.Clear();
                    break;
                case ConfigMenuItemType.Windows:
                    IntegrationMode.CliShellIntegrationMode(opt);
                    PromptPlus.WriteLine(Constants.PressAnyKeyConfiguration);
                    PromptPlus.Clear();
                    break;
                case ConfigMenuItemType.Formats:
                    PromptPlus.WriteLine(
                        $"WitchyBND supports the following formats:\n{string.Join(", ", ParseMode.Parsers.Where(p => p.IncludeInList).Select(p => p.Name))}");
                    PromptPlus.WriteLine(Constants.PressAnyKeyConfiguration);
                    PromptPlus.ReadKey();
                    PromptPlus.Clear();
                    break;
                case ConfigMenuItemType.Help:
                    Program.DisplayHelp();
                    PromptPlus.WriteLine(Constants.PressAnyKeyConfiguration);
                    PromptPlus.ReadKey();
                    PromptPlus.Clear();
                    break;
                case ConfigMenuItemType.Exit:
                    PromptPlus.Clear();
                    return;
                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}