using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WitchyBND.Services;

public sealed record ProcessRequest(
    string FileName,
    IReadOnlyList<string> Arguments,
    string? WorkingDirectory = null,
    IReadOnlyDictionary<string, string?>? Environment = null,
    TimeSpan? Timeout = null);

public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);

public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken = default);
}

public sealed class ProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FileName))
            throw new ArgumentException("A process executable is required.", nameof(request));

        using var process = new Process
        {
            StartInfo = CreateStartInfo(request),
            EnableRaisingEvents = true
        };

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        process.OutputDataReceived += (_, eventArgs) => AppendLine(stdout, eventArgs.Data);
        process.ErrorDataReceived += (_, eventArgs) => AppendLine(stderr, eventArgs.Data);

        if (!process.Start())
            throw new InvalidOperationException($"Failed to start process '{request.FileName}'.");

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeoutSource = request.Timeout is { } timeout
            ? new CancellationTokenSource(timeout)
            : new CancellationTokenSource();
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);

        try
        {
            await process.WaitForExitAsync(linkedSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && timeoutSource.IsCancellationRequested)
        {
            KillProcessTree(process);
            throw new TimeoutException($"Process '{request.FileName}' exceeded its timeout.");
        }
        catch (OperationCanceledException)
        {
            KillProcessTree(process);
            throw;
        }

        return new ProcessResult(process.ExitCode, stdout.ToString(), stderr.ToString());
    }

    private static ProcessStartInfo CreateStartInfo(ProcessRequest request)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = request.FileName,
            WorkingDirectory = request.WorkingDirectory ?? Environment.CurrentDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (string argument in request.Arguments)
            startInfo.ArgumentList.Add(argument);

        if (request.Environment != null)
        {
            foreach ((string name, string? value) in request.Environment)
                startInfo.Environment[name] = value;
        }

        return startInfo;
    }

    private static void AppendLine(StringBuilder builder, string? value)
    {
        if (value != null)
            builder.AppendLine(value);
    }

    private static void KillProcessTree(Process process)
    {
        if (process.HasExited)
            return;

        try
        {
            process.Kill(true);
            process.WaitForExit();
        }
        catch (InvalidOperationException)
        {
            // The process exited between the state check and kill request.
        }
    }
}
