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
        Context,
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
            new ConfigMenuItem { Type = ConfigMenuItemType.Context, Label = "Configure shell integration" },
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
                case ConfigMenuItemType.Context:
                    ShellIntegrationMode.CliShellIntegrationMode(opt);
                    PromptPlus.WriteLine(Constants.PressAnyKeyConfiguration);
                    PromptPlus.Clear();
                    break;
                case ConfigMenuItemType.Formats:
                    PromptPlus.WriteLine(
                        $"WitchyBND supports the following formats:\n{string.Join(", ", ParseMode.Parsers.Select(p => p.Name))}");
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
                    Environment.Exit(0);
                    break;
                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}