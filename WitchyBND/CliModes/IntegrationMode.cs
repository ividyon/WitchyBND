using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Win32;
using PPlus;

namespace WitchyBND.CliModes;

public class IntegrationMode
{
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

        [Display(Name = "Return to configuration menu")]
        Return
    }

    public static void CliShellIntegrationMode(CliOptions opt)
    {
        while (true)
        {
            PromptPlus.Clear();
            PromptPlus.DoubleDash("WitchyBND Windows integration");
            var select = PromptPlus.Select<IntegrationChoices>("Select an option")
                .Run();
            if (select.IsAborted) return;
            switch (select.Value)
            {
                case IntegrationChoices.Register:
                    RegisterContext();
                    PromptPlus.WriteLine("Successfully registered WitchyBND context menu.");
                    break;
                case IntegrationChoices.Unregister:
                    UnregisterContext();
                    PromptPlus.WriteLine("Successfully unregistered WitchyBND context menu.");
                    PromptPlus.WriteLine(
                        @"Explorer needs to be restarted to complete the process.
Your taskbar will briefly disappear for a few seconds. Witchy will try to restore any open Explorer windows.");
                    var choice = PromptPlus.Confirm("Proceed with restarting the Explorer process?").Run();
                    if (choice.Value.IsYesResponseKey())
                    {
                        Shell.RestartExplorer();
                        PromptPlus.WriteLine("Restarted the Explorer process.");
                    }
                    break;
                case IntegrationChoices.UnregisterYabber:
                    UnregisterYabberContext();
                    PromptPlus.WriteLine("Successfully unregistered Yabber context menu.");
                    break;
                case IntegrationChoices.AddToPath:
                    Shell.AddToPathVariable();
                    PromptPlus.WriteLine("Successfully added WitchyBND to PATH variable.");
                    break;
                case IntegrationChoices.RemoveFromPath:
                    Shell.RemoveFromPathVariable();
                    PromptPlus.WriteLine("Successfully removed WitchyBND from PATH variable.");
                    break;
                case IntegrationChoices.Return:
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