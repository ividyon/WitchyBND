using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PPlus;
using WitchyBND;
using WitchyBND.Errors;
using ThreadState = System.Diagnostics.ThreadState;

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
        while (!process.HasExited)
        {
            foreach(ProcessThread thread in process.Threads)
                if (thread.ThreadState == ThreadState.Wait
                    && thread.WaitReason is ThreadWaitReason.UserRequest or ThreadWaitReason.LpcReply)
                {
                    throw new ProcessUserInputException(process);
                }
            Thread.Sleep(200);
        }
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