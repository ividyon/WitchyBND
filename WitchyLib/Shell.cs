using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace WitchyLib;

public static class Shell
{
    static Shell()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        Root = Registry.CurrentUser;
    }

    private static readonly string ExePath = Path.GetDirectoryName(AppContext.BaseDirectory)!;
    const int WmUser = 0x0400; //http://msdn.microsoft.com/en-us/library/windows/desktop/ms644931(v=vs.85).aspx

    private static readonly RegistryKey Root;
    public static readonly string WitchyPath = Path.Combine(ExePath, "WitchyBND.exe");

    private const string ClsidRegistryKey = @"Software\Classes\CLSID";
    private const string ClassesRegistryKey = @"Software\Classes";
    private const string ContextMenuHandlerRegistryKey = @"Software\Classes\*\shellex\ContextMenuHandlers";
    private const string DirContextMenuHandlerRegistryKey = @"Software\Classes\Directory\shellex\ContextMenuHandlers";
    private const string ComplexMenuGuid = "{cce90c57-0a92-4cb7-8e9b-0cfa92138ae9}";
    private const string ComplexMenuFullName = "WitchyBND.Shell.WitchyContextMenu";

    public static bool ComplexContextMenuIsRegistered(string path = null)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        if (path != null)
            path = $"file:///{path.Replace(Path.DirectorySeparatorChar, '/').TrimEnd('/')}/WitchyBND.Shell.dll";
        var probeKey = string.Join(Path.DirectorySeparatorChar, [ClsidRegistryKey, ComplexMenuGuid, "InprocServer32"]);
        RegistryKey key = Root.OpenSubKey(probeKey);
        return path != null ? (string)key?.GetValue("CodeBase") == path : key != null;
    }

    public static void RegisterComplexContextMenu()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        var assemblyPath = $"file:///{ExePath.Replace(Path.DirectorySeparatorChar, '/').TrimEnd('/')}/WitchyBND.Shell.dll";
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
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
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
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        RestartExplorer restartExplorer = new RestartExplorer();
        restartExplorer.Execute(() => {
        });
    }

    public static void AddToPathVariable()
    {
        RemoveFromPathVariable();
        var name = "PATH";
        var scope = EnvironmentVariableTarget.User;
        var oldValue = Environment.GetEnvironmentVariable(name, scope);
        var newValue  = oldValue + $";{ExePath}";
        Environment.SetEnvironmentVariable(name, newValue, scope);
    }

    public static void RemoveFromPathVariable()
    {
        var name = "PATH";
        var scope = EnvironmentVariableTarget.User;
        var oldValue = Environment.GetEnvironmentVariable(name, scope);
        var newValue = oldValue.Replace($";{ExePath}", "");
        Environment.SetEnvironmentVariable(name, newValue, scope);
    }

    private static RegistryKey EnsureSubKey(RegistryKey root, string name)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
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
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
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
    public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
}