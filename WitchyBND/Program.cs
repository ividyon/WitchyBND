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
using PPlus;
using WitchyBND.CliModes;
using WitchyLib;
using PARAM = WitchyFormats.FsParam;

namespace WitchyBND;

[SupportedOSPlatform("windows")]
public enum CliMode
{
    Parse,
    Config
}

public enum WitchyErrorType
{
    Generic,
    NoOodle,
    NoAccess,
    Exception
}

public class WitchyError
{
    public string Message { get; set; }
    public WitchyErrorType Type { get; set; } = WitchyErrorType.Generic;
    public string Source { get; set; } = null;
    public short ErrorCode { get; set; } = -1;

    public WitchyError(string message)
    {
        Message = message;
    }

    public WitchyError(string message, string source)
    {
        Message = message;
        Source = source;
    }

    public WitchyError(string message, string source, WitchyErrorType type)
    {
        Message = message;
        Source = source;
        Type = type;
    }

    public WitchyError(string message, string source, WitchyErrorType type, short errorCode)
    {
        Message = message;
        Source = source;
        Type = type;
        ErrorCode = errorCode;
    }

    public WitchyError(string message, string source, short errorCode)
    {
        Message = message;
        Source = source;
        ErrorCode = errorCode;
    }

    public WitchyError(string message, WitchyErrorType type)
    {
        Message = message;
        Type = type;
    }

    public WitchyError(string message, short errorCode)
    {
        Message = message;
        ErrorCode = errorCode;
    }
}

public class WitchyNotice
{
    public string Message { get; set; }
    public string Source { get; set; } = null;

    public WitchyNotice(string message)
    {
        Message = message;
    }

    public WitchyNotice(string message, string source)
    {
        Message = message;
        Source = source;
    }
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

internal static class Program
{
    private static List<WitchyError> AccruedErrors;
    private static List<WitchyNotice> AccruedNotices;
    public static int ProcessedItems = 0;

    [STAThread]
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        PromptPlus.Config.DefaultCulture = new CultureInfo("en-us");

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
                        PromptPlus.WriteLine($"{assembly.GetName().Name} v{assembly.GetName().Version.ToString()}"
                            .PromptPlusEscape());
                        return;
                    }

                    PromptPlus.DoubleDash($"{assembly.GetName().Name} {assembly.GetName().Version}");

                    if (opt.Dcx)
                        Configuration.Dcx = opt.Dcx;
                    if (opt.Bnd)
                        Configuration.Bnd = opt.Bnd;
                    if (opt.ParamDefaultValues != null)
                        Configuration.ParamDefaultValues = opt.ParamDefaultValues.Value;
                    if (opt.Recursive)
                        Configuration.Recursive = opt.Recursive;

                    // Arg-only configuration
                    if (opt.RepackOnly)
                        Configuration.Args.RepackOnly = opt.RepackOnly;
                    if (opt.UnpackOnly)
                        Configuration.Args.UnpackOnly = opt.UnpackOnly;
                    if (opt.Passive)
                        Configuration.Args.Passive = opt.Passive;
                    if (!string.IsNullOrWhiteSpace(opt.Location))
                    {
                        string location = opt.Location;
                        if (opt.Location == "prompt")
                        {
                            if (Configuration.Args.Passive)
                                throw new Exception("Cannot supply both \"passive\" and \"location\" options.");
                            PromptPlus.WriteLine("Prompting user for target directory...");

                            NativeFileDialogSharp.DialogResult dialogResult = NativeFileDialogSharp.Dialog.FolderPicker();

                            if (dialogResult.IsOk && !string.IsNullOrWhiteSpace(dialogResult.Path))
                            {
                                location = dialogResult.Path;
                                PromptPlus.WriteLine($"Target directory set to: {location}");
                                PromptPlus.WriteLine("");
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

                    // Set CLI mode
                    CliMode mode = CliMode.Parse;
                    if (!opt.Paths.Any())
                        mode = CliMode.Config;

                    // Execute
                    switch (mode)
                    {
                        case CliMode.Parse:
                            DisplayConfiguration();
                            ParseMode.CliParseMode(opt);
                            break;
                        case CliMode.Config:
                            ConfigMode.CliConfigMode(opt);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                catch (Exception e)
                {
                    RegisterException(e);
                }

                int pause = Configuration.EndDelay;
                if (Configuration.PauseOnError && AccruedErrors.Count > 0)
                    pause = -1;
                var completedString = ProcessedItems == 1
                    ? "Operation completed on 1 item."
                    : $"Operation completed on {ProcessedItems} items.";
                PromptPlus.WriteLine("");
                PromptPlus.WriteLine(string.Concat(Enumerable.Repeat("-", completedString.Length)));
                PromptPlus.WriteLine(completedString);
                if (ProcessedItems > 0)
                    if (AccruedErrors.Count > 0)
                    {
                        PromptPlus.WriteLine("");
                        PromptPlus.SingleDash("Errors during operation");
                        foreach (WitchyError error in AccruedErrors)
                        {
                            if (error.Source != null)
                                PromptPlus.Error.WriteLine($"{error.Source}: {error.Message}".PromptPlusEscape());
                            else
                                PromptPlus.Error.WriteLine($"{error.Message}".PromptPlusEscape());
                        }
                    }

                if (AccruedNotices.Count > 0)
                {
                    PromptPlus.WriteLine("");
                    PromptPlus.SingleDash("Notices during operation");
                    foreach (WitchyNotice notice in AccruedNotices)
                    {
                        if (notice.Source != null)
                            PromptPlus.Error.WriteLine($"{notice.Source}: {notice.Message}".PromptPlusEscape());
                        else
                            PromptPlus.Error.WriteLine($"{notice.Message}".PromptPlusEscape());
                    }
                }

                if (!Configuration.Args.Passive)
                {
                    PromptPlus.WriteLine("");
                    if (pause == -1)
                    {
                        PromptPlus.WriteLine(Constants.PressAnyKey);
                        PromptPlus.ReadKey();
                        return;
                    }

                    if (pause > 0)
                    {
                        PromptPlus.WriteLine($"Closing in {TimeSpan.FromMilliseconds(pause).TotalSeconds} second(s)...");
                        Thread.Sleep(pause);
                    }
                }
            })
            .WithNotParsed(errors => { DisplayHelp(parserResult, errors); });
    }

    public static void DisplayConfiguration()
    {
        var infoTable = new Dictionary<string, string>()
        {
            { "Specialized BND handling", Configuration.Bnd.ToString() },
            { "DCX decompression only", Configuration.Dcx.ToString() },
            { "Store PARAM field default values", Configuration.ParamDefaultValues.ToString() },
            { "Recursive binder processing", Configuration.Recursive.ToString() },
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

        PromptPlus.SingleDash("Configuration");
        foreach ((string name, string value) in infoTable)
        {
            PromptPlus.WriteLine($"{name.PadLeft(longest)}: {value}");
        }
        PromptPlus.WriteLine("-------------");
        PromptPlus.WriteLine("");
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

        PromptPlus.WriteLine(helpText.ToString().PromptPlusEscape());
    }

    public static void RegisterNotice(string message, bool write = true)
    {
        RegisterNotice(new WitchyNotice(message), write);
    }

    public static void RegisterNotice(WitchyNotice notice, bool write = true)
    {
        AccruedNotices.Add(notice);
        if (write)
        {
            if (notice.Source != null)
                PromptPlus.Error.WriteLine($"{notice.Source}: {notice.Message}".PromptPlusEscape());
            else
                PromptPlus.Error.WriteLine(notice.Message.PromptPlusEscape());
        }
    }

    public static void RegisterException(Exception e, string source = null)
    {
        switch (e)
        {
            case UnsupportedActionException:
                RegisterError(new WitchyError($@"Unsupported user action:
{e.ToString().PromptPlusEscape()}
", source, WitchyErrorType.Exception, 1));
                break;
            default:
                RegisterError(new WitchyError($@"Unhandled exception:
{e.ToString().PromptPlusEscape()}
", source, WitchyErrorType.Exception, 1));
                break;
        }
    }

    public static void RegisterError(string message, bool write = true)
    {
        RegisterError(new WitchyError(message), write);
    }

    public static void RegisterError(WitchyError error, bool write = true)
    {
        AccruedErrors.Add(error);
        if (write)
        {
            if (error.Source != null)
                PromptPlus.Error.WriteLine($"{error.Source}: {error.Message}".PromptPlusEscape());
            else
                PromptPlus.Error.WriteLine(error.Message.PromptPlusEscape());
        }
        if (Configuration.IsTest)
            throw new Exception(error.Message);
    }

    static Program()
    {
        AccruedErrors = new List<WitchyError>();
        AccruedNotices = new List<WitchyNotice>();
    }
}