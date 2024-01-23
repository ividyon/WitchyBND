using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;
using PPlus;
using WitchyLib;

namespace WitchyBND;

public static class Shell
{
    const int WM_USER = 0x0400; //http://msdn.microsoft.com/en-us/library/windows/desktop/ms644931(v=vs.85).aspx

    private static RegistryKey Root = Registry.CurrentUser;
    private static string ClassesKey = @"Software\Classes";
    private static string ProgIdQuick = "WitchyBND.A";
    private static string ProgId = "WitchyBND.B";
    public static readonly string WitchyPath = Path.Combine(WBUtil.GetExeLocation() ?? ".", "WitchyBND.exe");

    private const string ClsidRegistryKey = @"Software\Classes\CLSID";
    private const string ClassesRegistryKey = @"Software\Classes";
    private const string ContextMenuHandlerRegistryKey = @"Software\Classes\*\shellex\ContextMenuHandlers";
    private const string DirContextMenuHandlerRegistryKey = @"Software\Classes\Directory\shellex\ContextMenuHandlers";
    private const string ComplexMenuGuid = "{cce90c57-0a92-4cb7-8e9b-0cfa92138ae9}";
    private const string ComplexMenuFullName = "WitchyBND.Shell.WitchyContextMenu";

    public static void RegisterSimpleContextMenu()
    {
        using (RegistryKey key = EnsureSubKey(ClassesKey, "*", "shell", ProgIdQuick))
        {
            key.SetValue("Icon", Path.Combine(WBUtil.GetExeLocation(), "WitchyBND.exe"));
            key.SetValue("MUIVerb", "WitchyBND");
            using (RegistryKey commandKey = EnsureSubKey(ClassesKey, "*", "shell", ProgIdQuick, "command"))
            {
                commandKey.SetValue(null, $"\"{WitchyPath}\" %1");
            }
        }
        using (RegistryKey key = EnsureSubKey(ClassesKey, "*", "shell", ProgId ))
        {
            key.SetValue("Icon", Path.Combine(WBUtil.GetExeLocation(), "WitchyBND.exe"));
            key.SetValue("MUIVerb", "WitchyBND...");
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

    public static void UnregisterSimpleContextMenu()
    {
        using (RegistryKey key = Root.OpenSubKey(Path.Combine(ClassesKey, "*", "shell"), true))
        {
            if (key != null)
            {
                key.DeleteSubKeyTree(ProgIdQuick, false);
                key.DeleteSubKeyTree(ProgId, false);
            }
        }
    }

    public static void RegisterComplexContextMenu()
    {
        var assemblyPath = $"file:///{WBUtil.GetExeLocation().Replace(Path.DirectorySeparatorChar, '/').TrimEnd('/')}/WitchyBND.Shell.dll";
        var runtimeVersion = "v4.0.30319";
        var version = "1.0.0.0";
        var assemblyFullName = $"WitchyBND.Shell, Version={version}, Culture=neutral, PublicKeyToken=1eff254c75ae9a19";
        var progId = ComplexMenuFullName;

            using (RegistryKey key = EnsureSubKey(ClassesRegistryKey, ComplexMenuFullName))
            {
                key.SetValue(null, ComplexMenuFullName);
            }
            using (RegistryKey key = EnsureSubKey(ClassesRegistryKey, ComplexMenuFullName, "CLSID"))
            {
                key.SetValue(null, ComplexMenuGuid);
            }

            using (RegistryKey key = EnsureSubKey(ContextMenuHandlerRegistryKey, ComplexMenuFullName.Split('.').Last()))
            {
                key.SetValue(null, ComplexMenuGuid);
            }
            using (RegistryKey key = EnsureSubKey(DirContextMenuHandlerRegistryKey, ComplexMenuFullName.Split('.').Last()))
            {
                key.SetValue(null, ComplexMenuGuid);
            }

            using (RegistryKey key = EnsureSubKey(ClsidRegistryKey, ComplexMenuGuid, "InprocServer32"))
            {
                key.SetValue(null, "mscoree.dll");
                key.SetValue("Assembly", assemblyFullName);
                key.SetValue("Class", ComplexMenuFullName);
                key.SetValue("ThreadingModel", "Both");
                key.SetValue("CodeBase", assemblyPath);

                key.SetValue("RuntimeVersion", runtimeVersion);
            }

            using (RegistryKey key = EnsureSubKey(ClsidRegistryKey, ComplexMenuGuid,
                           "InprocServer32", version))
            {
                key.SetValue("Assembly", assemblyFullName);
                key.SetValue("Class", ComplexMenuFullName);
                key.SetValue("ThreadingModel", "Both");
                key.SetValue("CodeBase", assemblyPath);
            }

            using (RegistryKey key = EnsureSubKey(ClsidRegistryKey, ComplexMenuGuid))
            {
                key.SetValue(null, ComplexMenuFullName);
                // cf http://stackoverflow.com/questions/2070999/is-the-implemented-categories-key-needed-when-registering-a-managed-com-compon
                using (RegistryKey cats = EnsureSubKey(key,
                           @"Implemented Categories\{62C8FE65-4EBB-45e7-B440-6E39B2CDBF29}"))
                {
                    // do nothing special
                }
                if (!string.IsNullOrEmpty(progId))
                {
                    using (RegistryKey progIdKey = EnsureSubKey(key, "ProgId"))
                    {
                        progIdKey.SetValue(null, progId);
                    }
                }
            }

            using (RegistryKey key = EnsureSubKey(ClsidRegistryKey, ComplexMenuGuid, "ProgId"))
            {
                key.SetValue(null, ComplexMenuFullName);
            }

            // Tell explorer the file association has been changed
            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
    }


    public static void UnregisterComplexContextMenu()
    {
        using (RegistryKey key = Root.OpenSubKey(ClassesRegistryKey, true))
        {
            if (key != null)
                key.DeleteSubKeyTree(ComplexMenuFullName, false);
        }

        using (RegistryKey key = Root.OpenSubKey(ClsidRegistryKey, true))
        {
            if (key != null)
                key.DeleteSubKeyTree(ComplexMenuGuid, false);
        }

        using (RegistryKey key = Root.OpenSubKey(ContextMenuHandlerRegistryKey, true))
        {
            if (key != null)
                key.DeleteSubKeyTree(ComplexMenuFullName.Split('.').Last(), false);
        }

        using (RegistryKey key = Root.OpenSubKey(DirContextMenuHandlerRegistryKey, true))
        {
            if (key != null)
                key.DeleteSubKeyTree(ComplexMenuFullName.Split('.').Last(), false);
        }

        // Tell explorer the file association has been changed
        SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
    }

    public static void RestartExplorer()
    {
        RestartExplorer restartExplorer = new RestartExplorer();
        restartExplorer.Execute(() => {
            PromptPlus.WriteLine("Explorer process stopped.");
        });
    }

    public static void AddToPathVariable()
    {
        RemoveFromPathVariable();
        var name = "PATH";
        var scope = EnvironmentVariableTarget.User;
        var oldValue = Environment.GetEnvironmentVariable(name, scope);
        var newValue  = oldValue + @$";{WBUtil.GetExeLocation()}\\";
        Environment.SetEnvironmentVariable(name, newValue, scope);
    }

    public static void RemoveFromPathVariable()
    {
        var name = "PATH";
        var scope = EnvironmentVariableTarget.User;
        var oldValue = Environment.GetEnvironmentVariable(name, scope);
        var newValue = oldValue.Replace(@$";{WBUtil.GetExeLocation()}\\", "");
        Environment.SetEnvironmentVariable(name, newValue, scope);
    }

    private static RegistryKey EnsureSubKey(RegistryKey root, string name)
    {
        RegistryKey key = root.OpenSubKey(name, true);
        if (key != null)
            return key;

        string parentName = Path.GetDirectoryName(name);
        if (string.IsNullOrEmpty(parentName))
            return root.CreateSubKey(name);

        using (RegistryKey parentKey = EnsureSubKey(root, parentName))
        {
            return parentKey.CreateSubKey(Path.GetFileName(name));
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

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool PostMessage(IntPtr hWnd, [MarshalAs(UnmanagedType.U4)] uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
}