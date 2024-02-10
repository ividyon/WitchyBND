using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Win32;
using PPlus;
using WitchyBND.Services;

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

    public static void CliShellIntegrationMode(CliOptions opt)
    {
        while (true)
        {
            output.Clear();
            output.DoubleDash("WitchyBND Windows integration");
            var select = output.Select<IntegrationChoices>("Select an option")
                .Run();
            if (select.IsAborted) return;
            switch (select.Value)
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
                    if (choice.Value.IsYesResponseKey())
                    {
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

    public static void UnregisterYabberContext()
    {
        RegistryKey classes = Registry.CurrentUser.OpenSubKey("Software\\Classes", true);
        classes.DeleteSubKeyTree("*\\shell\\yabber", false);
        classes.DeleteSubKeyTree("directory\\shell\\yabber", false);
        classes.DeleteSubKeyTree("*\\shell\\yabberdcx", false);
    }

    public static void UnregisterContext()
    {
        Shell.UnregisterComplexContextMenu();
        SendTo.DeleteSendToShortcuts();
    }

    public static void RegisterContext()
    {
        UnregisterContext();
        Shell.RegisterComplexContextMenu();
        SendTo.AddSendToShortcuts();
    }
}