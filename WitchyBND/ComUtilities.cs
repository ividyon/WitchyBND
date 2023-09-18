using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace WitchyBND
{
    /// <summary>
    /// Written by Simon Mourier
    /// https://stackoverflow.com/a/35789844/2275217
    /// </summary>
    public static class ComUtilities
    {
        private const string ClsidRegistryKey = @"Software\Classes\CLSID";
        private const string ClassesRegistryKey = @"Software\Classes";
        private const string ContextMenuHandlerRegistryKey = @"Software\Classes\*\shellex\ContextMenuHandlers";
        private const string DirContextMenuHandlerRegistryKey = @"Software\Classes\Directory\shellex\ContextMenuHandlers";

        public enum Target
        {
            Machine, // registers or unregisters a .NET COM object in HKEY_LOCAL_MACHINE, for all users, needs proper rights
            User // registers or unregisters a .NET COM object in HKEY_CURRENT_USER to avoid UAC prompts
        }

        public static void RegisterComObject(Target target, Type type, string assemblyPath = null)
        {
            // ServerRegistrationManager.RegisterServer(new WitchyContextMenu(), RegistrationType.OS64Bit);
            RegisterComObject(target, type, assemblyPath, null);
        }

        public static void RegisterComObject(Target target, Type type, string assemblyPath, string runtimeVersion)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (type.Assembly == null)
                throw new ArgumentException(null, nameof(type));

            // note we don't check if the type is marked as ComVisible, maybe we should

            if (assemblyPath == null)
            {
                assemblyPath = new Uri(type.Assembly.CodeBase).AbsoluteUri;
            }

            if (runtimeVersion == null)
            {
                runtimeVersion = GetRuntimeVersion(type.Assembly);
            }

            var guid = type.GUID.ToString("B");

            var root = target == Target.User ? Registry.CurrentUser : Registry.LocalMachine;

            using (RegistryKey key = EnsureSubKey(root,
                       Path.Combine(ClassesRegistryKey, type.FullName)))
            {
                key.SetValue(null, type.FullName);
            }
            using (RegistryKey key = EnsureSubKey(root,
                       Path.Combine(ClassesRegistryKey, type.FullName, "CLSID")))
            {
                key.SetValue(null, guid);
            }

            using (RegistryKey key = EnsureSubKey(root,
                       Path.Combine(ContextMenuHandlerRegistryKey, type.FullName.Split('.').Last())))
            {
                key.SetValue(null, guid);
            }
            using (RegistryKey key = EnsureSubKey(root,
                       Path.Combine(DirContextMenuHandlerRegistryKey, type.FullName.Split('.').Last())))
            {
                key.SetValue(null, guid);
            }

            using (RegistryKey key = EnsureSubKey(root,
                       Path.Combine(ClsidRegistryKey, guid, "InprocServer32")))
            {
                key.SetValue(null, "mscoree.dll");
                key.SetValue("Assembly", type.Assembly.FullName);
                key.SetValue("Class", type.FullName);
                key.SetValue("ThreadingModel", "Both");
                key.SetValue("CodeBase", assemblyPath);

                key.SetValue("RuntimeVersion", runtimeVersion);
            }

            using (RegistryKey key = EnsureSubKey(root,
                       Path.Combine(ClsidRegistryKey, guid,
                           "InprocServer32", type.Assembly.GetName().Version.ToString())))
            {
                key.SetValue("Assembly", type.Assembly.FullName);
                key.SetValue("Class", type.FullName);
                key.SetValue("ThreadingModel", "Both");
                key.SetValue("CodeBase", assemblyPath);
            }

            using (RegistryKey key = EnsureSubKey(root, Path.Combine(ClsidRegistryKey, guid)))
            {
                key.SetValue(null, type.FullName);
                // cf http://stackoverflow.com/questions/2070999/is-the-implemented-categories-key-needed-when-registering-a-managed-com-compon
                using (RegistryKey cats = EnsureSubKey(key,
                           @"Implemented Categories\{62C8FE65-4EBB-45e7-B440-6E39B2CDBF29}"))
                {
                    // do nothing special
                }

                var att = type.GetCustomAttribute<ProgIdAttribute>();
                if (att != null && !string.IsNullOrEmpty(att.Value))
                {
                    using (RegistryKey progid = EnsureSubKey(key, "ProgId"))
                    {
                        progid.SetValue(null, att.Value);
                    }
                }
            }

            using (RegistryKey key = EnsureSubKey(root, Path.Combine(ClsidRegistryKey, guid, "ProgId")))
            {
                key.SetValue(null, type.FullName);
            }

            // Tell explorer the file association has been changed
            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
        }

        public static void UnregisterComObject(Target target, Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var root = target == Target.User ? Registry.CurrentUser : Registry.LocalMachine;
            var guid = type.GUID.ToString("B");
            using (RegistryKey key = root.OpenSubKey(ClsidRegistryKey, true))
            {
                if (key != null)
                    key.DeleteSubKeyTree(guid, false);
            }

            using (RegistryKey key = root.OpenSubKey(ContextMenuHandlerRegistryKey, true))
            {
                if (key != null)
                    key.DeleteSubKeyTree(type.FullName.Split('.').Last(), false);
            }

            using (RegistryKey key = root.OpenSubKey(DirContextMenuHandlerRegistryKey, true))
            {
                if (key != null)
                    key.DeleteSubKeyTree(type.FullName.Split('.').Last(), false);
            }


            using (RegistryKey key = root.OpenSubKey(ClassesRegistryKey, true))
            {
                if (key != null)
                    key.DeleteSubKeyTree(type.FullName, false);
            }

            // Tell explorer the file association has been changed
            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
        }

        // kind of hack to determine clr version of an assembly
        private static string GetRuntimeVersion(Assembly asm)
        {
            string def = "v4.0.30319"; // use CLR4 as the default
            try
            {
                var mscorlib = asm.GetReferencedAssemblies().FirstOrDefault(a => a.Name == "mscorlib");
                if (mscorlib != null && mscorlib.Version.Major < 4)
                    return "v2.0.50727"; // use CLR2
            }
            catch
            {
                // too bad, assume CLR4
            }

            return def;
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

        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
    }
}