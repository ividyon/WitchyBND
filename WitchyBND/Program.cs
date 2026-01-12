using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using CommandLine;
using CommandLine.Text;
using NativeFileDialogSharp;
using SoulsFormats;
using WitchyBND.CliModes;
using WitchyBND.Services;
using WitchyLib;
using Oodle = SoulsOodleLib.Oodle;

namespace WitchyBND;

[SupportedOSPlatform("windows")]
internal static class Program
{
    public static int ProcessedItems = 0;

    private static IErrorService _errorService;
    private static IUpdateService _updateService;
    private static IOutputService _output;


    static Program()
    {
    }

    [STAThread]
    static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

        Assembly assembly = Assembly.GetExecutingAssembly();
        // AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;

        var parser = new Parser(with => {
            // with.AutoHelp = false;
            // with.AutoVersion = false;
        });

        RuntimeHelpers.RunClassConstructor(typeof(Configuration).TypeHandle);
        ServiceProvider.InitializeProvider();
        _errorService = ServiceProvider.GetService<IErrorService>();
        _updateService = ServiceProvider.GetService<IUpdateService>();
        _output = ServiceProvider.GetService<IOutputService>();

        var parserResult = parser.ParseArguments<CliOptions>(args);
        parserResult.WithParsed(opt => {
                try
                {
                    if (opt.Silent)
                    {
                        Configuration.Active.Silent = opt.Silent;
                        Configuration.Active.Passive = opt.Silent;
                    }

                    // Override configuration
                    if (opt.Help)
                    {
                        DisplayHelp(parserResult);
                        return;
                    }

                    if (opt.Version)
                    {
                        var assembly = Assembly.GetExecutingAssembly();
                        _output.WriteLine($"{assembly.GetName().Name} v{assembly.GetName().Version.ToString()}"
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
                            Configuration.Active.Dcx = opt.Dcx;
                        if (opt.BasicBnd)
                            Configuration.Active.Bnd = false;
                        else if (opt.SpecializedBnd)
                            Configuration.Active.Bnd = true;
                        if (opt.Recursive)
                            Configuration.Active.Recursive = opt.Recursive;
                        if (opt.Parallel)
                            Configuration.Active.Parallel = opt.Parallel;
                        if (opt.SingleThread)
                            Configuration.Active.Parallel = false;
                        if (opt.Flexible)
                            Configuration.Active.Flexible = opt.Flexible;

                        // Arg-only configuration
                        if (opt.RepackOnly)
                            Configuration.Active.RepackOnly = opt.RepackOnly;
                        if (opt.UnpackOnly)
                            Configuration.Active.UnpackOnly = opt.UnpackOnly;
                        if (opt.Passive)
                            Configuration.Active.Passive = opt.Passive;

                        if (Configuration.Active.Flexible)
                            BinaryReaderEx.IsFlexible = true;

                        TAE.ValidateEventBank = false;
                    }

                    _output.DoubleDash($"{assembly.GetName().Name} {assembly.GetName().Version}");

                    if (!string.IsNullOrWhiteSpace(opt.Location))
                    {
                        string location = opt.Location;
                        if (opt.Location == "prompt")
                        {
                            if (Configuration.Active.Passive)
                                throw new Exception("Cannot supply both \"passive\" and \"location\" options.");
                            _output.WriteLine("Prompting user for target directory...");

                            DialogResult dialogResult =
                                Dialog.FolderPicker();

                            if (dialogResult.IsOk && !string.IsNullOrWhiteSpace(dialogResult.Path))
                            {
                                location = dialogResult.Path;
                                _output.WriteLine($"Target directory set to: {location}");
                                _output.WriteLine("");
                            }
                            else
                            {
                                return;
                            }
                        }

                        if (!string.IsNullOrEmpty(location))
                        {
                            location = OSPath.GetFullPath(location);

                            if (!Directory.Exists(location))
                            {
                                Directory.CreateDirectory(location);
                            }

                            Configuration.Active.Location = location;
                        }
                    }

                    if (!Configuration.Active.Passive)
                        _updateService.CheckForUpdates(args);
                    _updateService.PostUpdateActions();

                    // Execute
                    switch (mode)
                    {
                        case CliMode.Parse:
                            PrintConfiguration(mode);

                            Stopwatch watch = new Stopwatch();

                            watch.Start();
                            Oodle.GrabOodle(_ => { }, false, true);
                            ParseMode.CliParseMode(opt);
                            watch.Stop();

                            int pause = Configuration.Active.EndDelay;
                            if (Configuration.Active.PauseOnError && _errorService.AccruedErrorCount > 0)
                                pause = -1;
                            var completedString = ProcessedItems == 1
                                ? $"Operation completed on 1 item in {watch.Elapsed:hh\\:mm\\:ss}."
                                : $"Operation completed on {ProcessedItems} items in {watch.Elapsed:hh\\:mm\\:ss}.";
                            _output.WriteLine("");
                            _output.WriteLine(string.Concat(Enumerable.Repeat("-", completedString.Length)));
                            _output.WriteLine(completedString);

                            PrintIssues();
                            PrintFinale(pause);
                            break;
                        case CliMode.Watch:
                            PrintConfiguration(mode);
                            Oodle.GrabOodle(_ => { }, false, true);
                            WatcherMode.CliWatcherMode(opt);
                            PrintIssues();
                            PrintFinale();
                            break;
                        case CliMode.Config:
                            ConfigMode.CliConfigMode(opt);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                catch (Exception e) when (!Configuration.IsTest && !Configuration.IsDebug)
                {
                    _errorService.RegisterException(e);
                    PrintIssues();
                    PrintFinale(-1);
                }
            })
            .WithNotParsed(errors => { DisplayHelp(parserResult, errors); });
        if (_errorService.HasErrors)
            return -1;
        return 0;
    }

    // private static Assembly? CurrentDomainOnAssemblyResolve(object? sender, ResolveEventArgs args)
    // {
    //     string dllName = args.Name.Contains(',') ? args.Name.Substring(0, args.Name.IndexOf(',')) : args.Name.Replace(".dll","");
    //
    //     dllName = dllName.Replace(".", "_").Replace("-", "_");
    //
    //     if (dllName.EndsWith("_resources")) return null;
    //
    //     System.Resources.ResourceManager rm = new System.Resources.ResourceManager(AppDomain.CurrentDomain.Asse + ".Properties.Resources", System.Reflection.Assembly.GetExecutingAssembly());
    //
    //     byte[] bytes = (byte[])rm.GetObject(dllName);
    //
    //     return System.Reflection.Assembly.Load(bytes);
    // }

    private static void PrintIssues()
    {
        if (ProcessedItems > 5)
        {
            _errorService.PrintIssues();
        }
    }

    private static void PrintFinale(int? pause = null)
    {
        pause ??= Configuration.Active.EndDelay;
        if (!Configuration.Active.Passive)
        {
            _output.WriteLine("");
            if (pause == -1)
            {
                _output.KeyPress(Constants.PressAnyKey).Run();
                return;
            }

            if (pause > 0)
            {
                _output.WriteLine(
                    $"Closing in {TimeSpan.FromMilliseconds(pause.Value).TotalSeconds} second(s)...");
                Thread.Sleep(pause.Value);
            }
        }
    }

    private static void PrintConfiguration(CliMode mode)
    {
        var infoTable = new Dictionary<string, (string, string?)>
        {
            { "Selected mode", (mode.ToString(), null) },
            { "Specialized BND handling", (Configuration.Active.Bnd.ToString(), Configuration.Default.Bnd.ToString()) },
            { "Store PARAM field default values", (Configuration.ParamDefaultValues.ToString(), true.ToString()) },
            { "Recursive binder processing", (Configuration.Active.Recursive.ToString(), Configuration.Default.Recursive.ToString()) },
            { "Parallel processing", (Configuration.Active.Parallel.ToString(), Configuration.Default.Parallel.ToString()) },
            { "Flexible extraction", (Configuration.Active.Flexible.ToString(), Configuration.Default.Flexible.ToString()) }
        };

        if (Configuration.Active.Passive)
            infoTable.Add("Passive", (Configuration.Active.Passive.ToString(), false.ToString()));
        if (!string.IsNullOrEmpty(Configuration.Active.Location))
            infoTable.Add("Location", (Configuration.Active.Location, string.Empty));
        if (Configuration.Active.RepackOnly)
            infoTable.Add("Repack only", (Configuration.Active.RepackOnly.ToString(), false.ToString()));
        if (Configuration.Active.UnpackOnly)
            infoTable.Add("Unpack only", (Configuration.Active.UnpackOnly.ToString(), false.ToString()));

        var longest = infoTable.Keys.MaxBy(s => s.Length).Length;

        _output.SingleDash("Configuration");
        foreach ((string name, (string, string?) value) in infoTable)
        {
            string valueOutput = value.Item1;
            if (OperatingSystem.IsWindows() && !string.IsNullOrWhiteSpace(value.Item2) && value.Item1 != value.Item2)
                valueOutput = $"[yellow]{valueOutput}[/]";
            _output.WriteLine($"{name.PadLeft(longest)}: {valueOutput}");
        }

        _output.WriteLine("-------------");
        _output.WriteLine("");
    }

    public static void DisplayHelp(ParserResult<CliOptions> result = null, IEnumerable<Error> errors = null)
    {
        DisplayHelp<CliOptions>(result, errors);
    }

    public static void DisplayHelp<T>(ParserResult<T> result = null, IEnumerable<Error> errors = null)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var versionInfo = FileVersionInfo.GetVersionInfo(OSPath.Combine(AppContext.BaseDirectory, "WitchyBND.exe"));
        var companyName = versionInfo.CompanyName;

        if (result == null)
            result = new Parser(with => { }).ParseArguments<T>(new[] { "--help" });

        HelpText? helpText = HelpText.AutoBuild(result, h => {
            h.AutoHelp = false;
            h.AutoVersion = false;
            h.AdditionalNewLineAfterOption = false;
            h.Heading = $"{assembly.GetName().Name} v{assembly.GetName().Version}"; //change header
            h.Copyright = $"Copyright (c) 2023 {companyName}"; //change copyright text
            return HelpText.DefaultParsingErrorsHandler(result, h);
        }, e => e);

        _output.WriteLine(helpText.ToString().PromptPlusEscape());
    }
}