using System;
using System.Diagnostics;
using System.IO;
using PPlus;
using WitchyBND;

namespace WitchyLib;

public static class ProcessHandling
{

    public static int RunProcess(string exePath, out string output, out string error, string args = "", string? workingDir = null)
    {
        Process process = new Process();
        // Configure the process using the StartInfo properties.
        process.StartInfo.FileName = Path.GetFullPath(exePath);
        process.StartInfo.WorkingDirectory = workingDir ?? Path.GetDirectoryName(exePath)!;
        process.StartInfo.Arguments = args;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.Start();
        process.WaitForExit();
        output = process.StandardOutput.ReadToEnd();
        error = process.StandardError.ReadToEnd();
        return process.ExitCode;
    }
    public static int RunProcess(string exePath, string args = "", string? workingDir = null)
    {
        return RunProcess(exePath, out string _, out string _, args, workingDir);
    }
}