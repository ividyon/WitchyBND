using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using WitchyLib;

namespace WitchyBND;

public static class Shellx
{
    private static RegistryKey Root = Registry.CurrentUser;
    private static string ClassesKey = @"Software\Classes";
    private static string ProgId = typeof(Shellx).FullName;
    public static readonly string WitchyPath = Path.Combine(WBUtil.GetExeLocation() ?? ".", "WitchyBND.exe");

    public static void RegisterContextMenu()
    {
        using (RegistryKey key = EnsureSubKey(ClassesKey, "*", "shell", ProgId ))
        {
            key.SetValue("Icon", Path.Combine(WBUtil.GetExeLocation(), "WitchyBND.exe"));
            key.SetValue("MUIVerb", "WitchyBND");
            key.SetValue("ExtendedSubCommandsKey", @$"*\shell\{ProgId}");
        }
        using (RegistryKey key = EnsureSubKey(ClassesKey, "*", "shell", ProgId, "Shell", "ProcessHere" ))
        {
            key.SetValue("MUIVerb", "Process here");
            using (RegistryKey commandKey = EnsureSubKey(ClassesKey, "*", "shell", ProgId, "Shell",
                       "ProcessHere", "command"))
            {
                commandKey.SetValue(null, $"\"{WitchyPath}\" %1");
            }
        }
        using (RegistryKey key = EnsureSubKey(ClassesKey, "*", "shell", ProgId, "Shell", "ProcessHereRecursive" ))
        {
            key.SetValue("MUIVerb", "Process here (Recursive)");
            using (RegistryKey commandKey = EnsureSubKey(ClassesKey, "*", "shell", ProgId, "Shell",
                       "ProcessHereRecursive", "command"))
            {
                commandKey.SetValue(null, $"\"{WitchyPath}\" --recursive %1");
            }
        }
        using (RegistryKey key = EnsureSubKey(ClassesKey, "*", "shell", ProgId, "Shell", "ProcessHereDCX" ))
        {
            key.SetValue("MUIVerb", "Process here (DCX)");
            using (RegistryKey commandKey = EnsureSubKey(ClassesKey, "*", "shell", ProgId, "Shell",
                       "ProcessHereDCX", "command"))
            {
                commandKey.SetValue(null, $"\"{WitchyPath}\" --dcx %1");
            }
        }
        using (RegistryKey key = EnsureSubKey(ClassesKey, "*", "shell", ProgId, "Shell", "ProcessTo" ))
        {
            key.SetValue("CommandFlags", 0x20);
            key.SetValue("MUIVerb", "Process to...");
            using (RegistryKey commandKey = EnsureSubKey(ClassesKey, "*", "shell", ProgId, "Shell",
                       "ProcessTo", "command"))
            {
                commandKey.SetValue(null, $"\"{WitchyPath}\" --location prompt %1");
            }
        }
        using (RegistryKey key = EnsureSubKey(ClassesKey, "*", "shell", ProgId, "Shell", "ProcessToRecursive" ))
        {
            key.SetValue("MUIVerb", "Process to... (Recursive)");
            using (RegistryKey commandKey = EnsureSubKey(ClassesKey, "*", "shell", ProgId, "Shell",
                       "ProcessToRecursive", "command"))
            {
                commandKey.SetValue(null, $"\"{WitchyPath}\" --recursive --location prompt %1");
            }
        }
        using (RegistryKey key = EnsureSubKey(ClassesKey, "*", "shell", ProgId, "Shell", "ProcessToDcx" ))
        {
            key.SetValue("MUIVerb", "Process to... (DCX)");
            using (RegistryKey commandKey = EnsureSubKey(ClassesKey, "*", "shell", ProgId, "Shell",
                       "ProcessToDcx", "command"))
            {
                commandKey.SetValue(null, $"\"{WitchyPath}\" --dcx --location prompt %1");
            }
        }
    }

    public static void UnregisterContextMenu()
    {
        using (RegistryKey key = Root.OpenSubKey(Path.Combine(ClassesKey, "*", "shell"), true))
        {
            if (key != null)
                key.DeleteSubKeyTree(ProgId, false);
        }
    }


    private static RegistryKey EnsureSubKey(params string[] name)
    {
        var joinedName = string.Join(Path.DirectorySeparatorChar, name);
        RegistryKey key = Root.OpenSubKey(joinedName, true);
        if (key != null)
            return key;

        string parentName = Path.GetDirectoryName(joinedName);
        if (string.IsNullOrEmpty(parentName))
            return Root.CreateSubKey(joinedName);

        using (RegistryKey parentKey = EnsureSubKey(parentName))
        {
            return parentKey.CreateSubKey(Path.GetFileName(joinedName));
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
}