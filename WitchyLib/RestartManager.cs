using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WitchyLib;

/**
 * Code by Vanco Pavlevski
 * https://devindeep.com/restart-explorer-programmatically-with-c/
 * https://www.youtube.com/watch?v=oU0TNwHjkcU
 */
[StructLayout(LayoutKind.Sequential)]
public struct RM_UNIQUE_PROCESS
{
    public int dwProcessId;
    public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
}

[Flags]
public enum RM_SHUTDOWN_TYPE : uint
{
    RmForceShutdown = 0x1,
    RmShutdownOnlyRegistered = 0x10
}

public delegate void RM_WRITE_STATUS_CALLBACK(UInt32 nPercentComplete);

public class Win32Api
{
    [DllImport("rstrtmgr.dll", CharSet = CharSet.Auto)]
    protected static extern int RmStartSession(out IntPtr pSessionHandle, int dwSessionFlags, string strSessionKey);

    [DllImport("rstrtmgr.dll")]
    protected static extern int RmEndSession(IntPtr pSessionHandle);

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Auto)]
    protected static extern int RmRegisterResources(IntPtr pSessionHandle, UInt32 nFiles, string[] rgsFilenames,
        UInt32 nApplications, RM_UNIQUE_PROCESS[] rgApplications, UInt32 nServices, string[] rgsServiceNames);

    [DllImport("rstrtmgr.dll")]
    protected static extern int RmShutdown(IntPtr pSessionHandle, RM_SHUTDOWN_TYPE lActionFlags,
        RM_WRITE_STATUS_CALLBACK fnStatus);

    [DllImport("rstrtmgr.dll")]
    protected static extern int RmRestart(IntPtr pSessionHandle, int dwRestartFlags, RM_WRITE_STATUS_CALLBACK fnStatus);

    [DllImport("kernel32.dll")]
    protected static extern bool GetProcessTimes(IntPtr hProcess, out System.Runtime.InteropServices.ComTypes.FILETIME lpCreationTime,
        out System.Runtime.InteropServices.ComTypes.FILETIME lpExitTime, out System.Runtime.InteropServices.ComTypes.FILETIME lpKernelTime, out System.Runtime.InteropServices.ComTypes.FILETIME lpUserTime);
}

public class RestartExplorer : Win32Api
{
    public event Action<string> ReportProgress;
    public event Action<uint> ReportPercentage;
    public void Execute() => Execute(() => { });

    public void Execute(Action action)
    {
        IntPtr handle;
        string key = Guid.NewGuid().ToString();

        int res = RmStartSession(out handle, 0, key);
        if (res == 0)
        {
            ReportProgress?.Invoke($"Restart Manager session created with ID {key}");

            RM_UNIQUE_PROCESS[] processes = GetProcesses("explorer");
            res = RmRegisterResources(
                handle,
                0, null,
                (uint)processes.Length, processes,
                0, null
            );
            if (res == 0)
            {
                ReportProgress?.Invoke("Successfully registered resources.");

                res = RmShutdown(handle, RM_SHUTDOWN_TYPE.RmForceShutdown,
                    (percent) => ReportPercentage?.Invoke(percent));
                if (res == 0)
                {
                    ReportProgress?.Invoke("Applications stopped successfully.\n");
                    action();

                    res = RmRestart(handle, 0, (percent) => ReportPercentage?.Invoke(percent));
                    if (res == 0)
                        ReportProgress?.Invoke("Applications restarted successfully.");
                }
            }

            res = RmEndSession(handle);
            if (res == 0)
                ReportProgress?.Invoke("Restart Manager session ended.");
        }
    }

    private RM_UNIQUE_PROCESS[] GetProcesses(string name)
    {
        List<RM_UNIQUE_PROCESS> lst = new List<RM_UNIQUE_PROCESS>();
        foreach (Process p in Process.GetProcessesByName(name))
        {
            RM_UNIQUE_PROCESS rp = new RM_UNIQUE_PROCESS();
            rp.dwProcessId = p.Id;
            System.Runtime.InteropServices.ComTypes.FILETIME creationTime, exitTime, kernelTime, userTime;
            GetProcessTimes(p.Handle, out creationTime, out exitTime, out kernelTime, out userTime);
            rp.ProcessStartTime = creationTime;
            lst.Add(rp);
        }

        return lst.ToArray();
    }
}