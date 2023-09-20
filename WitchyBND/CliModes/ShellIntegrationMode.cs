using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Win32;
using PPlus;

namespace WitchyBND.CliModes;

public class ShellIntegrationMode
{
    private enum ShellIntegrationChoices
    {
        [Display(Name = "Register WitchyBND shell integration",
            Description = "Add context menu entries for WitchyBND to right-click menus in Explorer.")]
        Register,

        [Display(Name = "Unregister WitchyBND shell integration",
            Description = "Remove context menu entries for WitchyBND from right-click menus in Explorer.")]
        Unregister,

        [Display(Name = "Unregister old Witchy/Yabber context menu",
            Description = "Remove context menu entries for Yabber from right-click menus in Explorer.")]
        UnregisterYabber,

        [Display(Name = "Return to configuration menu")]
        Return
    }

    public static void CliShellIntegrationMode(CliOptions opt)
    {
        while (true)
        {
            PromptPlus.Clear();
            PromptPlus.DoubleDash("WitchyBND shell integration options");
            var select = PromptPlus.Select<ShellIntegrationChoices>("Select an option")
                .Run();
            switch (select.Value)
            {
                case ShellIntegrationChoices.Register:
                    RegisterContext();
                    PromptPlus.WriteLine("Successfully registered WitchyBND shell integration.");
                    break;
                case ShellIntegrationChoices.Unregister:
                    UnregisterContext();
                    PromptPlus.WriteLine("Successfully unregistered WitchyBND shell integration.");
                    PromptPlus.WriteLine(
                        @"Explorer needs to be restarted to complete the process.
Any open folder windows will be closed, and your taskbar will briefly disappear for a few seconds.");
                    var choice = PromptPlus.Confirm("Proceed with restarting Explorer?").Run();
                    if (choice.Value.IsYesResponseKey())
                    {
                        Shell.RestartExplorer();
                        PromptPlus.WriteLine("Restarted the Explorer process.");
                    }
                    break;
                case ShellIntegrationChoices.UnregisterYabber:
                    UnregisterYabberContext();
                    PromptPlus.WriteLine("Successfully unregistered Yabber context menu.");
                    break;
                case ShellIntegrationChoices.Return:
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            PromptPlus.WriteLine(Constants.PressAnyKey);
            PromptPlus.ReadKey();
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