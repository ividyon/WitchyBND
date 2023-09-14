using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Microsoft.Win32;
using PPlus;
using WitchyBND.Context;
using WitchyLib;

namespace WitchyBND.CliModes;

public class ContextMode
{
    private enum ContextChoices
    {
        [Display(Name = "Register WitchyBND context menu",
            Description = "Add context menu entries for WitchyBND to right-click menus in Explorer.")]
        Register,

        [Display(Name = "Unregister WitchyBND context menu",
            Description = "Remove context menu entries for WitchyBND from right-click menus in Explorer.")]
        Unregister,

        [Display(Name = "Unregister Yabber context menu",
            Description = "Remove context menu entries for Yabber from right-click menus in Explorer.")]
        UnregisterYabber,

        [Display(Name = "Return to configuration menu")]
        Return
    }

    public static void CliContextMode(CliOptions opt)
    {
        PromptPlus.Clear();
        PromptPlus.DoubleDash("WitchyBND context menu options");
        var select = PromptPlus.Select<ContextChoices>("Select an option")
            .Run();
        switch (select.Value)
        {
            case ContextChoices.Register:
                RegisterContext();
                PromptPlus.WriteLine("Successfully registered WitchyBND context menu.");
                break;
            case ContextChoices.Unregister:
                UnregisterContext();
                PromptPlus.WriteLine("Successfully unregistered WitchyBND context menu.");
                break;
            case ContextChoices.UnregisterYabber:
                UnregisterYabberContext();
                PromptPlus.WriteLine("Successfully unregistered Yabber context menu.");
                break;
            case ContextChoices.Return:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        PromptPlus.WriteLine(Constants.PressAnyKeyConfiguration);
        PromptPlus.ReadKey();
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
        ComUtilities.UnregisterComObject(ComUtilities.Target.User, typeof(ContextMenu));
        SendTo.DeleteSendToShortcuts();
    }

    public static void RegisterContext()
    {
        UnregisterContext();
        ComUtilities.RegisterComObject(ComUtilities.Target.User, typeof(ContextMenu));
        SendTo.AddSendToShortcuts();
    }
}