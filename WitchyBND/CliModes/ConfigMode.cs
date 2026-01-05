using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.InteropServices;
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

        [Display(Name = "Set PARAM field style",
            Description = @"Determines how fields are written and read in PARAM XML serialization.")]
        ParamCellStyle,

        [Display(Name = "Set backup method",
            Description = @"Determines how WitchyBND handles backups.")]
        BackupBehavior,

        [Display(Name = "Perform backups inside Git repository",
            Description = @"Whether or not WitchyBND performs backups for files inside a valid Git repository.")]
        ToggleGitBackup,

        [Display(Name = "Configure deferred tools")]
        DeferredFormats,

        [Display(Name = "Configure end delay")]
        ConfigureDelay,

        [Display(Name = "Configure Windows integration")]
        Windows,

        [Display(Name = "Reset skipped versions")]
        ResetSkip,

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
  b) Configure the context menu under ""Windows integration""
     in the following screen, then right-click the file or
     directory and select ""WitchyBND"" in the context menu.

Press any key to continue to the configuration screen...");

        if (Configuration.Active.Passive)
            return;

        output.KeyPress().Run();
        while (true)
        {
            output.Clear();
            output.DoubleDash("Configuration menu");
            output.WriteLine("This menu is [YELLOW]paged[/]; scroll down to find more options.");
            output.WriteLine();
            var select = output.Select<ConfigMenuItem>("Choose an option")
                .TextSelector(a => {
                    bool? toggled = null;
                    var name = a.GetAttribute<DisplayAttribute>().Name;
                    switch (a)
                    {
                        case ConfigMenuItem.ToggleBnd:
                            toggled = Configuration.Stored.Bnd;
                            break;
                        case ConfigMenuItem.ToggleRecursive:
                            toggled = Configuration.Stored.Recursive;
                            break;
                        case ConfigMenuItem.ToggleParallel:
                            toggled = Configuration.Stored.Parallel;
                            break;
                        case ConfigMenuItem.TogglePauseOnError:
                            toggled = Configuration.Stored.PauseOnError;
                            break;
                        case ConfigMenuItem.ToggleOfflineMode:
                            toggled = Configuration.Stored.Offline;
                            break;
                        case ConfigMenuItem.ToggleTaeFolder:
                            toggled = Configuration.Stored.TaeFolder;
                            break;
                        case ConfigMenuItem.ToggleFlexible:
                            toggled = Configuration.Stored.Flexible;
                            break;
                        case ConfigMenuItem.ToggleGitBackup:
                            toggled = Configuration.Stored.GitBackup;
                            break;
                        case ConfigMenuItem.ParamDefaultThreshold:
                            var val = Configuration.Stored.ParamDefaultValueThreshold > 0f
                                ? Configuration.Stored.ParamDefaultValueThreshold.ToString()
                                : "Disabled";
                            return $"{name} ({val})";
                        case ConfigMenuItem.ParamCellStyle:
                            return $"{name} ({Configuration.Stored.ParamCellStyle.ToString()})";
                        case ConfigMenuItem.BackupBehavior:
                            return $"{name} ({Configuration.Stored.BackupMethod.ToString()})";
                        case ConfigMenuItem.ConfigureDelay:
                            return $"{name} ({Configuration.Stored.EndDelay}ms)";
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

            switch (select.Content)
            {
                case ConfigMenuItem.ToggleBnd:
                    Configuration.Stored.Bnd = !Configuration.Stored.Bnd;
                    updateConfig();
                    break;
                case ConfigMenuItem.ToggleRecursive:
                    Configuration.Stored.Recursive = !Configuration.Stored.Recursive;
                    updateConfig();
                    break;
                case ConfigMenuItem.ToggleParallel:
                    Configuration.Stored.Parallel = !Configuration.Stored.Parallel;
                    updateConfig();
                    break;
                case ConfigMenuItem.TogglePauseOnError:
                    Configuration.Stored.PauseOnError = !Configuration.Stored.PauseOnError;
                    updateConfig();
                    break;
                case ConfigMenuItem.ToggleOfflineMode:
                    Configuration.Stored.Offline = !Configuration.Stored.Offline;
                    updateConfig();
                    break;
                case ConfigMenuItem.ToggleTaeFolder:
                    Configuration.Stored.TaeFolder = !Configuration.Stored.TaeFolder;
                    updateConfig();
                    break;
                case ConfigMenuItem.ToggleFlexible:
                    Configuration.Stored.Flexible = !Configuration.Stored.Flexible;
                    updateConfig();
                    break;
                case ConfigMenuItem.ParamDefaultThreshold:
                    while (true)
                    {
                        var thresholdSelect = output.Input("Input new threshold (between 0.0 and 1.0")
                            // .AddValidators(PromptValidators.IsTypeFloat())
                            .Run();
                        if (thresholdSelect.IsAborted) break;
                        var threshold = float.Parse(thresholdSelect.Content);
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

                        Configuration.Stored.ParamDefaultValueThreshold = threshold;
                        updateConfig();
                        break;
                    }

                    break;
                case ConfigMenuItem.ParamCellStyle:
                    var cellSelect = output.Select<WPARAM.CellStyle>("Select PARAM field style").Run();
                    if (!cellSelect.IsAborted)
                    {
                        Configuration.Stored.ParamCellStyle = cellSelect.Content;
                        updateConfig();
                    }
                    break;
                case ConfigMenuItem.BackupBehavior:
                    var backupSelect = output.Select<WBUtil.BackupMethod>("Select backup method").Run();
                    if (!backupSelect.IsAborted)
                    {
                        Configuration.Stored.BackupMethod = backupSelect.Content;
                        updateConfig();
                    }
                    break;
                case ConfigMenuItem.ToggleGitBackup:
                    Configuration.Stored.GitBackup = !Configuration.Stored.GitBackup;
                    updateConfig();
                    break;
                case ConfigMenuItem.DeferredFormats:
                    DeferredFormatMode.Run(opt);
                    break;
                case ConfigMenuItem.ConfigureDelay:
                    var input = output.Input("Input new delay (in milliseconds)")
                        .AcceptInput(char.IsNumber)
                        // .AddValidators(PromptValidators.IsTypeUInt16("Input is not within valid range"))
                        // .ValidateOnDemand()
                        .Run();
                    if (!input.IsAborted)
                    {
                        Configuration.Stored.EndDelay = Convert.ToUInt16(input.Content);
                        updateConfig();
                    }
                    break;
                case ConfigMenuItem.Windows:
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        IntegrationMode.CliShellIntegrationMode(opt);
                    else
                    {
                        output.WriteLine("This is not supported on your current platform.");
                        output.KeyPress(Constants.PressAnyKeyConfiguration).Run();
                    }
                    break;
                case ConfigMenuItem.ResetSkip:
                    var conf = output.Confirm($"Reset any skipped versions? The updater will once again prompt you for an update, if there is one.").Run();
                    if (!conf.IsAborted)
                        Configuration.Stored.SkipUpdateVersion = new Version(0, 0, 0, 0);
                    updateConfig();
                    if (Configuration.Active.Offline)
                        output.WriteLine(
                            $"Skipped versions reset. You are in Offline mode, so you will not be prompted for updates.");
                    else
                        output.WriteLine(
                            "Skipped versions reset. If an update exists, you will not be prompted the next time you restart the program.");
                    output.KeyPress(Constants.PressAnyKeyConfiguration).Run();
                    break;
                case ConfigMenuItem.Formats:
                    output.WriteLine(
                        $"WitchyBND supports the following formats:\n{string.Join(", ", ParseMode.GetParsers(false).Where(p => p.IncludeInList).Select(p => p.Name))}");
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

            continue;

            void updateConfig()
            {
                Configuration.SaveConfiguration();
                output.WriteLine("Successfully updated the configuration.");
                output.KeyPress(Constants.PressAnyKeyConfiguration).Run();
            }
        }
    }
}