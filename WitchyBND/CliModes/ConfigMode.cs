using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using PPlus;
using PPlus.Controls;
using WitchyLib;

namespace WitchyBND.CliModes;

public static class ConfigMode
{
    private enum ConfigMenuItem
    {
        [Display(Name = "Use specialized BND handlers",
            Description = "Enable to extract certain BND types into a custom, user-friendly structure. Disable for standard handling.")]
        ToggleBnd,
        [Display(Name = "DCX decompression only",
        Description = "Enable to exclusively decompress DCX files and leave their contents intact.")]
        ToggleDcx,
        [Display(Name = "Recursive binder processing",
            Description = "Enable to recursively attempt to process any file inside unpacked binders. Very performance-intensive.")]
        ToggleRecursive,
        [Display(Name = "Parallel processing",
            Description = "Enable to perform WitchyBND operations in a parallelized, multi-threaded manner.")]
        ToggleParallel,
        [Display(Name = "Store PARAM field default values",
            Description = @"Enable to separate the default values of PARAM row fields out from the rows.
Disabling vastly increases XML output size.")]
        ToggleParamDefaultValues,
        [Display(Name = "Pause on error",
            Description = "Enable to pause the program and require a key press (unless in Passive mode) if it finishes with errors.")]
        TogglePauseOnError,
        [Display(Name = "Offline mode",
            Description = "Disables any internet connectivity features, such as the update version check.")]
        ToggleOfflineMode,
        [Display(Name = "Configure deferred tools")]
        DeferredFormats,
        [Display(Name = "Configure end delay")]
        ConfigureDelay,
        [Display(Name = "Configure Windows integration")]
        Windows,
        [Display(Name = "View available formats")]
        Formats,
        [Display(Name = "View help screen")]
        Help,
        [Display(Name = "Exit")]
        Exit
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
            PromptPlus.WriteLine("This menu is paged; scroll down to find more options.");
            var select = PromptPlus.Select<ConfigMenuItem>("Choose an option")
                .TextSelector(a => {
                    bool? toggled = null;
                    var name = a.GetAttribute<DisplayAttribute>().Name;
                    switch (a)
                    {
                        case ConfigMenuItem.ToggleBnd:
                            toggled = Configuration.Bnd;
                            break;
                        case ConfigMenuItem.ToggleDcx:
                            toggled = Configuration.Dcx;
                            break;
                        case ConfigMenuItem.ToggleRecursive:
                            toggled = Configuration.Recursive;
                            break;
                        case ConfigMenuItem.ToggleParallel:
                            toggled = Configuration.Parallel;
                            break;
                        case ConfigMenuItem.ToggleParamDefaultValues:
                            toggled = Configuration.ParamDefaultValues;
                            break;
                        case ConfigMenuItem.TogglePauseOnError:
                            toggled = Configuration.PauseOnError;
                            break;
                        case ConfigMenuItem.ToggleOfflineMode:
                            toggled = Configuration.Offline;
                            break;
                        case ConfigMenuItem.ConfigureDelay:
                            return $"{name} ({Configuration.EndDelay}ms)";
                    }
                    if (toggled != null)
                    {
                        return $"Toggle \"{name}\" " + (toggled.Value ? "(Enabled)" : "(Disabled)");
                    }
                    return name;
                })
                .ChangeDescription(a => a.GetAttribute<DisplayAttribute>().Description)
                .Run();

            if (select.IsAborted) return;

            void UpdateConfig()
            {
                Configuration.UpdateConfiguration();
                PromptPlus.WriteLine("Successfully updated the configuration.");
                PromptPlus.KeyPress(Constants.PressAnyKeyConfiguration).Run();
                PromptPlus.Clear();
            }

            switch (select.Value)
            {
                case ConfigMenuItem.ToggleBnd:
                    Configuration.Bnd = !Configuration.Bnd;
                    UpdateConfig();
                    break;
                case ConfigMenuItem.ToggleDcx:
                    Configuration.Dcx = !Configuration.Dcx;
                    UpdateConfig();
                    break;
                case ConfigMenuItem.ToggleRecursive:
                    Configuration.Recursive = !Configuration.Recursive;
                    UpdateConfig();
                    break;
                case ConfigMenuItem.ToggleParallel:
                    Configuration.Parallel = !Configuration.Parallel;
                    UpdateConfig();
                    break;
                case ConfigMenuItem.ToggleParamDefaultValues:
                    Configuration.ParamDefaultValues = !Configuration.ParamDefaultValues;
                    UpdateConfig();
                    break;
                case ConfigMenuItem.TogglePauseOnError:
                    Configuration.PauseOnError = !Configuration.PauseOnError;
                    UpdateConfig();
                    break;
                case ConfigMenuItem.ToggleOfflineMode:
                    Configuration.PauseOnError = !Configuration.PauseOnError;
                    UpdateConfig();
                    break;
                case ConfigMenuItem.DeferredFormats:
                    DeferredFormatMode.Run(opt);
                    PromptPlus.Clear();
                    break;
                case ConfigMenuItem.ConfigureDelay:
                    var input = PromptPlus.Input("Input new delay (in milliseconds)")
                        .AcceptInput(char.IsNumber)
                        .AddValidators(PromptValidators.IsTypeUInt16("Input is not within valid range"))
                        .ValidateOnDemand()
                        .Run();
                    if (!input.IsAborted)
                    {
                        Configuration.EndDelay = Convert.ToUInt16(input.Value);
                        Configuration.UpdateConfiguration();
                        UpdateConfig();
                    }
                    else
                        PromptPlus.Clear();
                    break;
                case ConfigMenuItem.Windows:
                    IntegrationMode.CliShellIntegrationMode(opt);
                    PromptPlus.Clear();
                    break;
                case ConfigMenuItem.Formats:
                    PromptPlus.WriteLine(
                        $"WitchyBND supports the following formats:\n{string.Join(", ", ParseMode.Parsers.Where(p => p.IncludeInList).Select(p => p.Name))}");
                    PromptPlus.KeyPress(Constants.PressAnyKeyConfiguration).Run();
                    PromptPlus.Clear();
                    break;
                case ConfigMenuItem.Help:
                    Program.DisplayHelp();
                    PromptPlus.KeyPress(Constants.PressAnyKeyConfiguration).Run();
                    PromptPlus.Clear();
                    break;
                case ConfigMenuItem.Exit:
                    PromptPlus.Clear();
                    return;
                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}