using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using SoulsFormats;
using SoulsOodleLib;

namespace WitchyBND.Services;

public sealed record OodleBackendSettings(string? Library, string? Wine, string Helper);

public static partial class OodleBackendConfigurator
{
    public static void ConfigureMacOS(CliOptions options, IProcessRunner? processRunner = null)
    {
        IOodleBackend? nativeCompression = null;
        if (options.NativeOozCompression)
        {
            string nativeLibrary = Path.GetFullPath(FirstNonEmpty(
                options.NativeOozLibrary,
                Environment.GetEnvironmentVariable("WITCHY_NATIVE_OOZ_LIBRARY"),
                Path.Combine(AppContext.BaseDirectory, "libwitchy_ooz.dylib"))!);
            if (!File.Exists(nativeLibrary))
                throw new FileNotFoundException("The experimental native ooz compression library was not found.", nativeLibrary);
            nativeCompression = new NativeOozCompressionBackend(nativeLibrary);
        }

        OodleBackendSettings settings = ResolveSettings(options);
        string? library = settings.Library;
        if (library == null)
        {
            OodleBackendRegistry.Current = nativeCompression;
            return;
        }

        library = Path.GetFullPath(library);
        string? wine = WineDiscovery.Find(settings.Wine);
        if (wine == null)
        {
            throw new FileNotFoundException(
                "Wine or CrossOver was not found. Install one or pass --wine /path/to/wine.");
        }

        string helper = Path.GetFullPath(settings.Helper);
        if (!File.Exists(helper))
            throw new FileNotFoundException("The Witchy Oodle Wine helper was not found.", helper);
        if (!File.Exists(library))
            throw new FileNotFoundException("The configured Oodle library was not found.", library);

        processRunner ??= new ProcessRunner();
        var backendOptions = new WineOodleOptions(
            wine,
            helper,
            library,
            ParseVersion(library),
            Path.Combine(Configuration.AppDataDirectory, "WineOodle"),
            TimeSpan.FromMinutes(5));
        IOodleBackend wineBackend = new WineOodleBackend(
            backendOptions,
            (fileName, arguments, workingDirectory, timeout) =>
            {
                ProcessResult result = processRunner.RunAsync(new ProcessRequest(
                        fileName,
                        arguments,
                        workingDirectory,
                        new Dictionary<string, string?> { ["WINEDEBUG"] = "-all" },
                        timeout))
                    .GetAwaiter()
                    .GetResult();
                return new ProcessInvocationResult(
                    result.ExitCode,
                    result.StandardOutput,
                    result.StandardError);
            });
        OodleBackendRegistry.Current = nativeCompression == null
            ? wineBackend
            : new SplitOodleBackend(wineBackend, nativeCompression);
    }

    public static OodleBackendSettings ResolveSettings(
        CliOptions options,
        Configuration.ActiveConfig? active = null,
        Func<string, string?>? getEnvironmentVariable = null)
    {
        active ??= Configuration.Active;
        getEnvironmentVariable ??= Environment.GetEnvironmentVariable;
        return new OodleBackendSettings(
            FirstNonEmpty(options.OodleLibrary, getEnvironmentVariable("WITCHY_OODLE_LIBRARY"), active.OodleLibrary),
            FirstNonEmpty(options.WineExecutable, getEnvironmentVariable("WITCHY_WINE"), active.WineExecutable),
            FirstNonEmpty(options.OodleHelper, getEnvironmentVariable("WITCHY_OODLE_HELPER"), active.OodleHelper,
                Path.Combine(AppContext.BaseDirectory, "Tools", "WitchyOodleHelper.exe"))!);
    }

    public static OodleVersion ParseVersion(string libraryPath)
    {
        Match match = OodleVersionPattern().Match(Path.GetFileName(libraryPath));
        return match.Success ? match.Groups[1].Value switch
        {
            "9" => OodleVersion.Oodle9,
            "8" => OodleVersion.Oodle8,
            "6" => OodleVersion.Oodle6,
            _ => throw new InvalidDataException("Unsupported Oodle version.")
        } : throw new InvalidDataException(
            "Oodle DLL filename must include version 6, 8, or 9 (for example oo2core_6_win64.dll).");
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (string? value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }
        return null;
    }

    [GeneratedRegex(@"oo2core_([689])_win64\.dll$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OodleVersionPattern();
}
