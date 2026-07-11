using System.Globalization;
using SoulsFormats;

namespace SoulsOodleLib;

public sealed record WineOodleOptions(
    string WineExecutable,
    string HelperExecutable,
    string OodleLibrary,
    OodleVersion Version,
    string WorkingRoot,
    TimeSpan Timeout);

public sealed class WineOodleBackend : IOodleBackend
{
    private readonly WineOodleOptions _options;
    private readonly Func<string, IReadOnlyList<string>, string, TimeSpan, ProcessInvocationResult> _runProcess;

    public WineOodleBackend(
        WineOodleOptions options,
        Func<string, IReadOnlyList<string>, string, TimeSpan, ProcessInvocationResult> runProcess)
    {
        _options = options;
        _runProcess = runProcess;
    }

    public string Name => "Wine + official Oodle";
    public OodleBackendKind Kind => OodleBackendKind.Wine;
    public bool IsAvailable =>
        File.Exists(_options.WineExecutable) &&
        File.Exists(_options.HelperExecutable) &&
        File.Exists(_options.OodleLibrary);

    public byte[] Decompress(byte[] source, long uncompressedSize)
    {
        return Execute(source, "decompress", [uncompressedSize.ToString(CultureInfo.InvariantCulture)]);
    }

    public byte[] Compress(
        byte[] source,
        SoulsFormats.Oodle.OodleLZ_Compressor compressor,
        SoulsFormats.Oodle.OodleLZ_CompressionLevel level)
    {
        return Execute(source, "compress",
        [
            ((int)compressor).ToString(CultureInfo.InvariantCulture),
            ((int)level).ToString(CultureInfo.InvariantCulture)
        ]);
    }

    private byte[] Execute(byte[] source, string operation, IReadOnlyList<string> operationArguments)
    {
        Directory.CreateDirectory(_options.WorkingRoot);
        RestrictDirectory(_options.WorkingRoot);
        string workDirectory = Path.Combine(_options.WorkingRoot, $"operation-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workDirectory);
        RestrictDirectory(workDirectory);
        string inputPath = Path.Combine(workDirectory, "input.bin");
        string outputPath = Path.Combine(workDirectory, "output.bin");

        try
        {
            File.WriteAllBytes(inputPath, source);
            var arguments = new List<string>
            {
                _options.HelperExecutable,
                VersionNumber(_options.Version),
                _options.OodleLibrary,
                operation,
                inputPath,
                outputPath
            };
            arguments.AddRange(operationArguments);

            ProcessInvocationResult result = _runProcess(
                _options.WineExecutable,
                arguments,
                workDirectory,
                _options.Timeout);
            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Wine Oodle helper failed with exit code {result.ExitCode}: {result.StandardError.Trim()}");
            }
            if (!File.Exists(outputPath))
                throw new InvalidDataException("Wine Oodle helper did not produce an output file.");
            return File.ReadAllBytes(outputPath);
        }
        finally
        {
            if (Directory.Exists(workDirectory))
                Directory.Delete(workDirectory, true);
        }
    }

    private static void RestrictDirectory(string path)
    {
        if (OperatingSystem.IsWindows())
            return;
        File.SetUnixFileMode(path,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }

    private static string VersionNumber(OodleVersion version) => version switch
    {
        OodleVersion.Oodle9 => "9",
        OodleVersion.Oodle8 => "8",
        OodleVersion.Oodle6 => "6",
        _ => throw new ArgumentOutOfRangeException(nameof(version))
    };
}

public sealed record ProcessInvocationResult(int ExitCode, string StandardOutput, string StandardError);

public static class WineDiscovery
{
    public static string? Find(
        string? explicitlyConfigured = null,
        Func<string, bool>? fileExists = null,
        Func<string, string?>? getEnvironmentVariable = null)
    {
        fileExists ??= File.Exists;
        getEnvironmentVariable ??= Environment.GetEnvironmentVariable;

        foreach (string? candidate in Candidates(explicitlyConfigured, getEnvironmentVariable))
        {
            if (!string.IsNullOrWhiteSpace(candidate) && fileExists(candidate))
                return Path.GetFullPath(candidate);
        }
        return null;
    }

    private static IEnumerable<string?> Candidates(
        string? explicitlyConfigured,
        Func<string, string?> getEnvironmentVariable)
    {
        yield return explicitlyConfigured;
        yield return getEnvironmentVariable("WITCHY_WINE");
        yield return "/Applications/CrossOver.app/Contents/SharedSupport/CrossOver/bin/wine";

        string? path = getEnvironmentVariable("PATH");
        if (path == null)
            yield break;
        foreach (string directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            yield return Path.Combine(directory, "wine64");
            yield return Path.Combine(directory, "wine");
        }
    }
}
