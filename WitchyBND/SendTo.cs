using System;
using System.IO;
using ShellLink;

namespace WitchyBND;

public static class SendTo
{
    private static readonly string sendToPath = Environment.GetFolderPath(Environment.SpecialFolder.SendTo);
    private static readonly string witchyPath = Path.Combine(AppContext.BaseDirectory, "WitchyBND.exe");
    private static readonly string shortcutPath = Path.Combine(sendToPath, "WitchyBND.lnk");
    private static readonly string shortcutPathRecursive = Path.Combine(sendToPath, "WitchyBND (Recursive).lnk");
    private static readonly string shortcutPathDcx = Path.Combine(sendToPath, "WitchyBND (DCX).lnk");
    private static readonly string shortcutPathTo = Path.Combine(sendToPath, "WitchyBND to....lnk");
    private static readonly string shortcutPathRecursiveTo = Path.Combine(sendToPath, "WitchyBND to... (Recursive).lnk");
    private static readonly string shortcutPathDcxTo = Path.Combine(sendToPath, "WitchyBND to... (DCX).lnk");

    public static void AddSendToShortcuts()
    {
        DeleteSendToShortcuts();
        Shortcut.CreateShortcut(witchyPath, "", Path.GetDirectoryName(witchyPath))
            .WriteToFile(shortcutPath);
        Shortcut.CreateShortcut(witchyPath, "--recursive", Path.GetDirectoryName(witchyPath), witchyPath, 0)
            .WriteToFile(shortcutPathRecursive);
        Shortcut.CreateShortcut(witchyPath, "--dcx", Path.GetDirectoryName(witchyPath), witchyPath, 0)
            .WriteToFile(shortcutPathDcx);
        Shortcut.CreateShortcut(witchyPath, "--location prompt", Path.GetDirectoryName(witchyPath))
            .WriteToFile(shortcutPathTo);
        Shortcut.CreateShortcut(witchyPath, "--location prompt --recursive", Path.GetDirectoryName(witchyPath), witchyPath, 0)
            .WriteToFile(shortcutPathRecursiveTo);
        Shortcut.CreateShortcut(witchyPath, "--location prompt --dcx", Path.GetDirectoryName(witchyPath), witchyPath, 0)
            .WriteToFile(shortcutPathDcxTo);
    }

    public static void DeleteSendToShortcuts()
    {
        if (File.Exists(shortcutPathDcx))
            File.Delete(shortcutPathDcx);
        if (File.Exists(shortcutPathDcxTo))
            File.Delete(shortcutPathDcxTo);
        if (File.Exists(shortcutPathRecursiveTo))
            File.Delete(shortcutPathRecursiveTo);
        if (File.Exists(shortcutPathRecursive))
            File.Delete(shortcutPathRecursive);
        if (File.Exists(shortcutPathTo))
            File.Delete(shortcutPathTo);
        if (File.Exists(shortcutPath))
            File.Delete(shortcutPath);
    }
}