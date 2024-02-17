using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using CommandLine;
using CommandLine.Text;
using WitchyBND.CliModes;
using WitchyBND.Services;
using WitchyLib;

namespace WitchyBND;

[SupportedOSPlatform("windows")]
internal static class Program
{
    public static int ProcessedItems = 0;

    private static readonly IErrorService errorService;
    private static readonly IUpdateService updateService;
    private static readonly IOutputService output;


    static Program()
    {
        ServiceProvider.InitializeProvider();
        errorService = ServiceProvider.GetService<IErrorService>();
        updateService = ServiceProvider.GetService<IUpdateService>();
        output = ServiceProvider.GetService<IOutputService>();
    }

    [STAThread]
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

        Assembly assembly = Assembly.GetExecutingAssembly();

        var parser = new Parser(with => {
            // with.AutoHelp = false;
            // with.AutoVersion = false;
        });

        var parserResult = parser.ParseArguments<CliOptions>(args);
        parserResult.WithParsed(opt => {
                try
                {
                    // Override configuration
                    if (opt.Help)
                    {
                        DisplayHelp(parserResult);
                        return;
                    }

                    if (opt.Version)
                    {
                        var assembly = Assembly.GetExecutingAssembly();
                        output.WriteLine($"{assembly.GetName().Name} v{assembly.GetName().Version.ToString()}"
                            .PromptPlusEscape());
                        return;
                    }

                    // Set CLI mode
                    CliMode mode = opt.Mode;
                    if (!opt.Paths.Any())
                        mode = CliMode.Config;

                    if (mode != CliMode.Config)
                    {
                        if (opt.Dcx)
                            Configuration.Dcx = opt.Dcx;
                        if (opt.Bnd)
                            Configuration.Bnd = opt.Bnd;
                        if (opt.ParamDefaultValues != null)
                            Configuration.ParamDefaultValues = opt.ParamDefaultValues.Value;
                        if (opt.Recursive)
                            Configuration.Recursive = opt.Recursive;
                        if (opt.Parallel)
                            Configuration.Parallel = opt.Parallel;

                        // Arg-only configuration
                        if (opt.RepackOnly)
                            Configuration.Args.RepackOnly = opt.RepackOnly;

                        if (opt.UnpackOnly)
                            Configuration.Args.UnpackOnly = opt.UnpackOnly;

                        if (opt.Passive)
                            Configuration.Args.Passive = opt.Passive;

                        if (opt.Silent)
                        {
                            Configuration.Args.Silent = opt.Silent;
                            Configuration.Args.Passive = opt.Silent;
                        }
                    }

                    output.DoubleDash($"{assembly.GetName().Name} {assembly.GetName().Version}");


                    if (!string.IsNullOrWhiteSpace(opt.Location))
                    {
                        string location = opt.Location;
                        if (opt.Location == "prompt")
                        {
                            if (Configuration.Args.Passive)
                                throw new Exception("Cannot supply both \"passive\" and \"location\" options.");
                            output.WriteLine("Prompting user for target directory...");

                            NativeFileDialogSharp.DialogResult dialogResult =
                                NativeFileDialogSharp.Dialog.FolderPicker();

                            if (dialogResult.IsOk && !string.IsNullOrWhiteSpace(dialogResult.Path))
                            {
                                location = dialogResult.Path;
                                output.WriteLine($"Target directory set to: {location}");
                                output.WriteLine("");
                            }
                            else
                            {
                                return;
                            }
                        }

                        if (!string.IsNullOrEmpty(location))
                        {
                            location = Path.GetFullPath(location);

                            if (!Directory.Exists(location))
                            {
                                throw new DirectoryNotFoundException($"Location {location} is invalid.");
                            }

                            Configuration.Args.Location = location;
                        }
                    }

                    // Execute
                    switch (mode)
                    {
                        case CliMode.Parse:
                            SoulsOodleLib.Oodle.GrabOodle(_ => { }, false, true);
                            DisplayConfiguration(mode);
                            updateService.CheckForUpdates();

                            Stopwatch watch = new Stopwatch();

                            watch.Start();
                            ParseMode.CliParseMode(opt);
                            watch.Stop();

                            int pause = Configuration.EndDelay;
                            if (Configuration.PauseOnError && errorService.AccruedErrorCount > 0)
                                pause = -1;
                            var completedString = ProcessedItems == 1
                                ? $"Operation completed on 1 item in {watch.Elapsed:hh\\:mm\\:ss}."
                                : $"Operation completed on {ProcessedItems} items in {watch.Elapsed:hh\\:mm\\:ss}.";
                            output.WriteLine("");
                            output.WriteLine(string.Concat(Enumerable.Repeat("-", completedString.Length)));
                            output.WriteLine(completedString);

                            PrintIssues();
                            PrintFinale(pause);
                            break;
                        case CliMode.Watch:
                            SoulsOodleLib.Oodle.GrabOodle(_ => { }, false, true);
                            DisplayConfiguration(mode);
                            updateService.CheckForUpdates();
                            WatcherMode.CliWatcherMode(opt);
                            PrintIssues();
                            PrintFinale();
                            break;
                        case CliMode.Config:
                            updateService.CheckForUpdates();
                            ConfigMode.CliConfigMode(opt);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                catch (Exception e)
                {
                    if (Configuration.IsTest)
                        throw;

                    errorService.RegisterException(e);
                    PrintIssues();
                    PrintFinale(-1);
                }
            })
            .WithNotParsed(errors => { DisplayHelp(parserResult, errors); });
    }

    private static void PrintIssues()
    {
        if (ProcessedItems > 5)
        {
            errorService.PrintIssues();
        }
    }

    private static void PrintFinale(int? pause = null)
    {
        pause ??= Configuration.EndDelay;
        if (!Configuration.Args.Passive)
        {
            output.WriteLine("");
            if (pause == -1)
            {
                output.KeyPress(Constants.PressAnyKey).Run();
                return;
            }

            if (pause > 0)
            {
                output.WriteLine(
                    $"Closing in {TimeSpan.FromMilliseconds(pause.Value).TotalSeconds} second(s)...");
                Thread.Sleep(pause.Value);
            }
        }
    }

    private static void DisplayConfiguration(CliMode mode)
    {
        var infoTable = new Dictionary<string, string>()
        {
            { "Selected mode", mode.ToString() },
            { "Specialized BND handling", Configuration.Bnd.ToString() },
            { "DCX decompression only", Configuration.Dcx.ToString() },
            { "Store PARAM field default values", Configuration.ParamDefaultValues.ToString() },
            { "Recursive binder processing", Configuration.Recursive.ToString() },
            { "Parallel processing", Configuration.Parallel.ToString() }
        };

        if (Configuration.Args.Passive)
            infoTable.Add("Passive", Configuration.Args.Passive.ToString());
        if (!string.IsNullOrEmpty(Configuration.Args.Location))
            infoTable.Add("Location", Configuration.Args.Location);
        if (Configuration.Args.RepackOnly)
            infoTable.Add("Repack only", Configuration.Args.RepackOnly.ToString());
        if (Configuration.Args.UnpackOnly)
            infoTable.Add("Unpack only", Configuration.Args.UnpackOnly.ToString());

        var longest = infoTable.Keys.MaxBy(s => s.Length).Length;

        output.SingleDash("Configuration");
        foreach ((string name, string value) in infoTable)
        {
            output.WriteLine($"{name.PadLeft(longest)}: {value}");
        }

        output.WriteLine("-------------");
        output.WriteLine("");
    }

    public static void DisplayHelp(ParserResult<CliOptions> result = null, IEnumerable<Error> errors = null)
    {
        DisplayHelp<CliOptions>(result, errors);
    }

    public static void DisplayHelp<T>(ParserResult<T> result = null, IEnumerable<Error> errors = null)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var versionInfo = FileVersionInfo.GetVersionInfo(Path.Combine(AppContext.BaseDirectory, "WitchyBND.exe"));
        var companyName = versionInfo.CompanyName;

        if (result == null)
            result = new Parser(with => { }).ParseArguments<T>(new[] { "--help" });

        var helpText = HelpText.AutoBuild(result, h => {
            h.AutoHelp = false;
            h.AutoVersion = false;
            h.AdditionalNewLineAfterOption = false;
            h.Heading = $"{assembly.GetName().Name} v{assembly.GetName().Version}"; //change header
            h.Copyright = $"Copyright (c) 2023 {companyName}"; //change copyright text
            return HelpText.DefaultParsingErrorsHandler(result, h);
        }, e => e);

        output.WriteLine(helpText.ToString().PromptPlusEscape());
    }
}