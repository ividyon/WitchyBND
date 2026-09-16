using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using SoulsFormats;
using WitchyBND.Services;
#if MACOS
using AppKit;
using Foundation;
#endif

namespace WitchyBND.CliModes;

public static class OodleMode
{
    private static readonly IOutputService output;

    static OodleMode()
    {
        output = ServiceProvider.GetService<IOutputService>();
    }

    private delegate void Oodle_LogHeader();

    // OodleCore_Plugins_SetPrintf doesn't work because C# can't provide a delegate matching t_fp_OodleCore_Plugin_Printf
    private static string GetOodleVersion(IntPtr handle)
    {
        [DllImport("libc")] static extern int dup(int fd);
        [DllImport("libc")] static extern int dup2(int oldfd, int newfd);
        [DllImport("libc")] static extern int close(int fd);
        [DllImport("libc")] static extern int fflush(IntPtr stream);

        var checkVersionPtr = Libdl.GetAddress(handle, "Oodle_LogHeader");
        if (checkVersionPtr == IntPtr.Zero) return "2.9";
        var path = Path.GetTempFileName();
        fflush(IntPtr.Zero);
        var saved = dup(1);
        using (var fs = File.Create(path))
            dup2((int)fs.SafeFileHandle.DangerousGetHandle(), 1);
        try { Marshal.GetDelegateForFunctionPointer<Oodle_LogHeader>(checkVersionPtr)(); }
        finally { fflush(IntPtr.Zero); dup2(saved, 1); close(saved); }
        var version = System.Text.RegularExpressions.Regex.Match(
            File.ReadAllText(path), @"Oodle (\d+\.\d+)\.").Groups[1].ToString();
        File.Delete(path);
        return version;
    }

    public static bool OodleRebundleMode(CliOptions opt)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return false;
        var _handle = SoulsOodleLib.Oodle.GrabOodle(_ => { }, false, false, AppContext.BaseDirectory);
        if (_handle == IntPtr.Zero)
        {
            output.WriteLine("Oodle library not found.");
            var oodlePath = "";
#if MACOS
            nint result = nint.Zero;
            string? path = null;
            NSApplication.SharedApplication.InvokeOnMainThread(() => {
                using var panel = NSOpenPanel.OpenPanel;
                panel.CanChooseFiles = true;
                panel.CanChooseDirectories = false;
                panel.AllowsMultipleSelection = false;
                panel.Message = "Select the macOS Oodle library.";
                panel.AllowedContentTypes = new[] { UniformTypeIdentifiers.UTTypes.UnixExecutable };
                Program.modal = true;
                NSApplication.SharedApplication.Activate();
                result = panel.RunModal();
                Program.modal = false;
                Program.terminal.Activate(NSApplicationActivationOptions.Default);
                path = panel.Url.Path;
            });
            if (result.ToInt64() != (long)NSModalResponse.OK || path == null)
            {
                output.WriteLine("No Oodle library selected.");
                output.KeyPress(Constants.PressAnyKeyConfiguration).Run();
                return false;
            }
            oodlePath = path;
#else
            output.WriteLine("Select the macOS Oodle library.");
            var openDialog = NativeFileDialogSharp.Dialog.FileOpen("dylib");
            if (!openDialog.IsOk || openDialog.Path == "" || !Path.Exists(openDialog.Path))
            {
                output.WriteLine("No Oodle library selected.");
                output.KeyPress(Constants.PressAnyKeyConfiguration).Run();
                return false;
            }
            oodlePath = openDialog.Path;
#endif
#if MACOS
            var info = new ProcessStartInfo("/usr/sbin/pkgutil", "--pkg-info=com.apple.pkg.CLTools_Executables")
                { RedirectStandardOutput = true, RedirectStandardError = true };
            var xcodecheck = Process.Start(info);
            xcodecheck.WaitForExit();
            if (xcodecheck.ExitCode != 0)
            {
                var install = output.Confirm(@"Xcode Command Line tools are required for WitchyBND to consume Oodle.
Install Xcode Command Line tools?").Run();
                if (!install.IsAborted)
                {
                    Process.Start("xcode-select", "--install");
                    output.WriteLine("Once the install is complete, re-try the Oodle setup.");
                    output.KeyPress(Constants.PressAnyKeyConfiguration).Run();
                }
                return false;
            }
#endif
            var handle = Libdl.LoadLibrary(oodlePath);
            bool hasSymbols = new[] { "Oodle26", "Oodle28", "Oodle29" }.Any(o =>
                typeof(Oodle).Assembly.GetType($"SoulsFormats.{o}")
                    .GetMethods(BindingFlags.NonPublic)
                    .Select(m => (method: m, attribute: m.GetCustomAttribute<DllImportAttribute>()))
                    .Where(a => a.attribute != null)
                    .All(a => Libdl.GetAddress(handle, a.attribute!.EntryPoint ?? a.method.Name) != IntPtr.Zero));
            if (!hasSymbols)
            {
                Libdl.FreeLibrary(handle);
                output.WriteLine("The selected file does not contain the required Oodle symbols.");
                output.KeyPress(Constants.PressAnyKeyConfiguration).Run();
                return false;
            }
            Libdl.FreeLibrary(handle);

            var libname = $"liboo2coremac64.{GetOodleVersion(handle)}.dylib";
            File.Copy(oodlePath, Path.Combine(AppContext.BaseDirectory, libname), true);
#if MACOS
            var installname = Process.Start("/usr/bin/install_name_tool", $"-id \"@executable_path/../../{libname}\" {Path.Combine(AppContext.BaseDirectory, libname)}");
            installname.WaitForExit();
            if (installname.ExitCode != 0)
            {
                output.WriteLine("install_name_tool failed. Reverted.");
                File.Delete(Path.Combine(AppContext.BaseDirectory, libname));
                output.KeyPress(Constants.PressAnyKeyConfiguration).Run();
                return false;
            }

            var codesign = Process.Start("/usr/bin/codesign", $"--force -s - \"{NSBundle.MainBundle.BundlePath}\"");
            codesign.WaitForExit();
            if (installname.ExitCode != 0)
            {
                output.WriteLine("codesign failed. Reverted.");
                File.Delete(Path.Combine(AppContext.BaseDirectory, libname));
                output.KeyPress(Constants.PressAnyKeyConfiguration).Run();
                return false;
            }

            Program.relaunch = true;
#endif
            output.WriteLine("Oodle setup complete. WitchyBND will now restart.");
            output.KeyPress(Constants.PressAnyKey).Run();
            return true;
        }
        else
            output.WriteLine("Oodle located, setup is complete.");
        return false;
    }
}

