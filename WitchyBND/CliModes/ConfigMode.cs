using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using PPlus;
using PPlus.Controls;
using WitchyBND.Parsers;
using WitchyBND.Services;
using WitchyLib;

namespace WitchyBND.CliModes;

public static class ConfigMode
{
    private static readonly IOutputService output;

    static ConfigMode()
    {
        output = ServiceProvider.GetService<IOutputService>();
    }

    private enum ConfigMenuItem
    {
        [Display(Name = "Use specialized BND handlers",
            Description =
                "Enable to extract certain BND types into a custom, user-friendly structure. Disable for standard handling.")]
        ToggleBnd,

        [Display(Name = "DCX decompression only",
            Description = "Enable to exclusively decompress DCX files and leave their contents intact.")]
        ToggleDcx,

        [Display(Name = "Recursive binder processing",
            Description =
                "Enable to recursively attempt to process any file inside unpacked binders. Very performance-intensive.")]
        ToggleRecursive,

        [Display(Name = "Parallel processing",
            Description = "Enable to perform WitchyBND operations in a parallelized, multi-threaded manner.")]
        ToggleParallel,

        [Display(Name = "Pause on error",
            Description =
                "Enable to pause the program and require a key press (unless in Passive mode) if it finishes with errors.")]
        TogglePauseOnError,

        [Display(Name = "Offline mode",
            Description = "Disables any internet connectivity features, such as the update version check.")]
        ToggleOfflineMode,

        [Display(Name = "Unpack TAE as folder",
            Description =
                "Enable to unpack TAEs as a folder of XMLs, one for each animation. Disabling will unpack the TAE into a single file.")]
        ToggleTaeFolder,

        [Display(Name = "Flexible decompression", Description = "Foregoes some stricter format checks to detect tampering with the compressed data, aka. \"\"\"encryption technology\"\"\" DRM.")]
        ToggleFlexible,

        [Display(Name = "PARAM field default threshold",
            Description =
                @"If the same field value is present in more than (this amount) of rows, it will be marked as ""default value"" for that field. Enter 0 to disable. Higher thresholds increase XML output size.")]
        ParamDefaultThreshold,

        [Display(Name = "Set PARAM field style")]
        ParamCellStyle,

        [Display(Name = "Configure deferred tools")]
        DeferredFormats,

        [Display(Name = "Configure end delay")]
        ConfigureDelay,

        [Display(Name = "Configure Windows integration")]
        Windows,

        [Display(Name = "View available formats")]
        Formats,
        [Display(Name = "View help screen")] Help,
        [Display(Name = "Exit")] Exit
    }

    public static void CliConfigMode(CliOptions opt)
    {
        output.WriteLine(@"Welcome to WitchyBND!

Launching the program without specifying any files will open this configuration screen.
If you want to unpack or repack a file or directory, you can either:

  a) Drag and drop it into the WitchyBND executable.
  b) Configure the context menu in the following screen,
     then right-click the file or directory and select
     ""WitchyBND"" in the context menu.

Press any key to continue to the configuration screen...");

        if (Configuration.Args.Passive)
            return;

        PromptPlus.KeyPress().Run();
        while (true)
        {
            output.Clear();
            output.DoubleDash("Configuration menu");
            output.WriteLine("This menu is paged; scroll down to find more options.");
            var select = output.Select<ConfigMenuItem>("Choose an option")
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
                        case ConfigMenuItem.TogglePauseOnError:
                            toggled = Configuration.PauseOnError;
                            break;
                        case ConfigMenuItem.ToggleOfflineMode:
                            toggled = Configuration.Offline;
                            break;
                        case ConfigMenuItem.ToggleTaeFolder:
                            toggled = Configuration.TaeFolder;
                            break;
                        case ConfigMenuItem.ToggleFlexible:
                            toggled = Configuration.Flexible;
                            break;
                        case ConfigMenuItem.ParamDefaultThreshold:
                            var val = Configuration.ParamDefaultValueThreshold > 0f
                                ? Configuration.ParamDefaultValueThreshold.ToString()
                                : "Disabled";
                            return $"{name} ({val})";
                        case ConfigMenuItem.ParamCellStyle:
                            return $"{name} ({Configuration.ParamCellStyle.ToString()})";
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
                output.WriteLine("Successfully updated the configuration.");
                output.KeyPress(Constants.PressAnyKeyConfiguration).Run();
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
                case ConfigMenuItem.TogglePauseOnError:
                    Configuration.PauseOnError = !Configuration.PauseOnError;
                    UpdateConfig();
                    break;
                case ConfigMenuItem.ToggleOfflineMode:
                    Configuration.PauseOnError = !Configuration.PauseOnError;
                    UpdateConfig();
                    break;
                case ConfigMenuItem.ToggleTaeFolder:
                    Configuration.TaeFolder = !Configuration.TaeFolder;
                    UpdateConfig();
                    break;
                case ConfigMenuItem.ToggleFlexible:
                    Configuration.Flexible = !Configuration.Flexible;
                    UpdateConfig();
                    break;
                case ConfigMenuItem.ParamDefaultThreshold:
                    while (true)
                    {
                        var thresholdSelect = output.Input("Input new threshold (between 0.0 and 1.0")
                            .AddValidators(PromptValidators.IsTypeFloat())
                            .Run();
                        if (thresholdSelect.IsAborted) break;
                        var threshold = float.Parse(thresholdSelect.Value);
                        if (threshold < 0f)
                        {
                            output.WriteError("Number cannot be less than 0.");
                            output.KeyPress().Run();
                            continue;
                        }

                        if (threshold > 1f)
                        {
                            output.WriteError("Number cannot be more than 1.");
                            output.KeyPress().Run();
                            continue;
                        }

                        Configuration.ParamDefaultValueThreshold = threshold;
                        UpdateConfig();
                        break;
                    }

                    break;
                case ConfigMenuItem.ParamCellStyle:
                    var cellSelect = output.Select<WPARAM.CellStyle>("Select PARAM field style").Run();
                    if (!cellSelect.IsAborted)
                    {
                        Configuration.ParamCellStyle = cellSelect.Value;
                        UpdateConfig();
                    }
                    break;
                case ConfigMenuItem.DeferredFormats:
                    DeferredFormatMode.Run(opt);
                    break;
                case ConfigMenuItem.ConfigureDelay:
                    var input = output.Input("Input new delay (in milliseconds)")
                        .AcceptInput(char.IsNumber)
                        .AddValidators(PromptValidators.IsTypeUInt16("Input is not within valid range"))
                        .ValidateOnDemand()
                        .Run();
                    if (!input.IsAborted)
                    {
                        Configuration.EndDelay = Convert.ToUInt16(input.Value);
                        UpdateConfig();
                    }
                    break;
                case ConfigMenuItem.Windows:
                    IntegrationMode.CliShellIntegrationMode(opt);
                    break;
                case ConfigMenuItem.Formats:
                    output.WriteLine(
                        $"WitchyBND supports the following formats:\n{string.Join(", ", ParseMode.Parsers.Where(p => p.IncludeInList).Select(p => p.Name))}");
                    output.KeyPress(Constants.PressAnyKeyConfiguration).Run();
                    break;
                case ConfigMenuItem.Help:
                    Program.DisplayHelp();
                    output.KeyPress(Constants.PressAnyKeyConfiguration).Run();
                    break;
                case ConfigMenuItem.Exit:
                    return;
                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}