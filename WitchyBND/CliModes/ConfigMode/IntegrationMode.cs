using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using PromptPlusLibrary;
using WitchyBND.Services;
using WitchyLib;

namespace WitchyBND.CliModes;

public static class IntegrationMode
{
    private static readonly IOutputService output;
    static IntegrationMode()
    {
        output = ServiceProvider.GetService<IOutputService>();
    }
    private enum IntegrationChoices
    {
        [Display(Name = "Register WitchyBND context menu",
            Description = "Add context menu entries for WitchyBND to right-click menus in Explorer.")]
        Register,

        [Display(Name = "Unregister WitchyBND context menu",
            Description = "Remove context menu entries for WitchyBND from right-click menus in Explorer.")]
        Unregister,

        [Display(Name = "Unregister old Witchy/Yabber context menu",
            Description = "Remove context menu entries for Yabber from right-click menus in Explorer.")]
        UnregisterYabber,

        [Display(Name = "Add WitchyBND to PATH environment variable",
            Description = "Allows calling WitchyBND from anywhere in the command prompt, or batch scripts.")]
        AddToPath,

        [Display(Name = "Remove WitchyBND from PATH environment variable",
            Description = "Reverts the above.")]
        RemoveFromPath,
    }
    private enum ServiceIntegrationChoices
    {
        [Display(Name = "Configure WitchyBND service menu",
            Description = "Manage WitchyBND context menu services for right-click menus in Finder.")]
        ConfigureServices,

        [Display(Name = "Configure macOS service menu",
            Description = "Adjust how many items appear in the macOS service menu without a submenu.")]
        ConfigureServiceMenu,
    }

    public static void CliShellIntegrationMode(CliOptions opt)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        while (true)
        {
            output.Clear();
            output.DoubleDash("WitchyBND Windows integration");
            var select = output.Select<IntegrationChoices>("Select an option")
                .ChangeDescription(a => a.GetAttribute<DisplayAttribute>().Description)
                .Run();
            if (select.IsAborted) return;
            switch (select.Content)
            {
                case IntegrationChoices.Register:
                    RegisterContext();
                    output.WriteLine("Successfully registered WitchyBND context menu.");
                    break;
                case IntegrationChoices.Unregister:
                    UnregisterContext();
                    output.WriteLine("Successfully unregistered WitchyBND context menu.");
                    output.WriteLine(
                        @"Explorer needs to be restarted to complete the process.
Your taskbar will briefly disappear for a few seconds. Witchy will try to restore any open Explorer windows.");
                    var choice = output.Confirm("Proceed with restarting the Explorer process?").Run();
                    if (choice.Content.Value.IsYesResponseKey())
                    {
                        output.WriteLine("Restarting the Explorer process...");
                        Shell.RestartExplorer();
                        output.WriteLine("Restarted the Explorer process.");
                    }
                    break;
                case IntegrationChoices.UnregisterYabber:
                    UnregisterYabberContext();
                    output.WriteLine("Successfully unregistered Yabber context menu.");
                    break;
                case IntegrationChoices.AddToPath:
                    Shell.AddToPathVariable();
                    output.WriteLine("Successfully added WitchyBND to PATH variable.");
                    break;
                case IntegrationChoices.RemoveFromPath:
                    Shell.RemoveFromPathVariable();
                    output.WriteLine("Successfully removed WitchyBND from PATH variable.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            output.WriteLine(Constants.PressAnyKey);
            output.ReadKey();
        }
    }

    public static void CliServiceIntegrationMode(CliOptions opt)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return;
        while (true)
        {
            output.Clear();
            output.DoubleDash("WitchyBND macOS integration");
#if !MACOS
            output.WriteLine(@"macOS services are not available for this build of WitchyBND.
Build or install the Microsoft.macOS configuration for right-click integration services.");
            output.WriteLine(Constants.PressAnyKey);
            output.ReadKey();
            return;
#endif
            var readInfo = new System.Diagnostics.ProcessStartInfo("/usr/bin/defaults", ["read", "-g", "NSServicesMinimumItemCountForContextSubmenu"])
                { RedirectStandardOutput = true, RedirectStandardError = true };
            var readDefaults = System.Diagnostics.Process.Start(readInfo);
            readDefaults.WaitForExit();
            var count = readDefaults.ExitCode != 0 ? "default" : $"{Convert.ToInt32(readDefaults.StandardOutput.ReadLine().Trim())-1}";
            var select = output.Select<ServiceIntegrationChoices>("Select an option")
                .TextSelector(a => {
                    var name = a.GetAttribute<DisplayAttribute>().Name;
                    switch (a)
                    {
                        case ServiceIntegrationChoices.ConfigureServiceMenu:
                            return $"{name} ({count})";
                    }
                    return name;
                })
                .ChangeDescription(a => a.GetAttribute<DisplayAttribute>().Description)
                .Run();
            if (select.IsAborted) return;
            switch (select.Content)
            {
                case ServiceIntegrationChoices.ConfigureServices:
                    output.WriteLine(
                        @"WitchyBND services must be enabled in System Settings, at
Keyboard > Keyboard Shortcuts > Services > Files and Folders");
#if MACOS
                    var handler = ObjCRuntime.Class.GetHandle("NSServicesMenuHandler");
                    if (handler != ObjCRuntime.NativeHandle.Zero)
                    {
                        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
                        static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);

                        // see [NSClassFromString(@"NSServicesMenuHandler") fp_methodDescription]
                        IntPtr_objc_msgSend(handler, ObjCRuntime.Selector.GetHandle("_configureServicesMenu:"));
                    }
                    else
                    {
                        AppKit.NSWorkspace.SharedWorkspace.OpenUrl(new Foundation.NSUrl(
                            "x-apple.systempreferences:com.apple.Keyboard-Settings?Shortcuts"));
                    }
                    Foundation.NSRunLoop.Current.RunUntil(Foundation.NSDate.DistantFuture);
#endif
                    break;
                case ServiceIntegrationChoices.ConfigureServiceMenu:
                    output.WriteLine("Input number of items allowed in service menu.");
                    var input = output.Input("Enter nothing to reset to the default.")
                        .AcceptInput(char.IsNumber)
                        .Run();
                    if (!input.IsAborted)
                    {
                        if (input.Content != "") {
                            var info = new System.Diagnostics.ProcessStartInfo("/usr/bin/defaults",
                                ["write", "-g", "NSServicesMinimumItemCountForContextSubmenu", $"{Math.Clamp(Convert.ToInt32(input.Content)+1, 0, 999)}"]);
                            System.Diagnostics.Process.Start(info).WaitForExit();
                        }
                        else if (count != "default")
                        {
                            var info = new System.Diagnostics.ProcessStartInfo("/usr/bin/defaults",
                                ["delete", "-g", "NSServicesMinimumItemCountForContextSubmenu"]);
                            System.Diagnostics.Process.Start(info).WaitForExit();
                        }
                    }
                    output.WriteLine("Done. Restart Finder for the changes to take effect.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            output.WriteLine(Constants.PressAnyKey);
            output.ReadKey();
        }
    }

    public static void UnregisterYabberContext()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        RegistryKey classes = Registry.CurrentUser.OpenSubKey("Software\\Classes", true);
        classes.DeleteSubKeyTree("*\\shell\\yabber", false);
        classes.DeleteSubKeyTree("directory\\shell\\yabber", false);
        classes.DeleteSubKeyTree("*\\shell\\yabberdcx", false);
    }

    public static void UnregisterContext()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        Shell.UnregisterComplexContextMenu();
        SendTo.DeleteSendToShortcuts();
    }

    public static void RegisterContext()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        UnregisterContext();
        Shell.RegisterComplexContextMenu();
        SendTo.AddSendToShortcuts();
    }
}