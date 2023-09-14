using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace WitchyBND.Context
{
    /// <summary>
    /// Written by Simon Mourier
    /// https://stackoverflow.com/a/35789844/2275217
    /// </summary>
    public static class ComUtilities
    {
        private const string ClsidRegistryKey = @"Software\Classes\CLSID";

        public enum Target
        {
            Machine, // registers or unregisters a .NET COM object in HKEY_LOCAL_MACHINE, for all users, needs proper rights
            User // registers or unregisters a .NET COM object in HKEY_CURRENT_USER to avoid UAC prompts
        }

        public static void RegisterComObject(Target target, Type type, string assemblyPath = null)
        {
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

            var root = target == Target.User ? Registry.CurrentUser : Registry.LocalMachine;

            using (RegistryKey key = EnsureSubKey(root,
                       Path.Combine(ClsidRegistryKey, type.GUID.ToString("B"), "InprocServer32")))
            {
                key.SetValue(null, "mscoree.dll");
                key.SetValue("Assembly", type.Assembly.FullName);
                key.SetValue("Class", type.FullName);
                key.SetValue("ThreadingModel", "Both");
                key.SetValue("CodeBase", assemblyPath);

                key.SetValue("RuntimeVersion", runtimeVersion);
            }

            using (RegistryKey key = EnsureSubKey(root, Path.Combine(ClsidRegistryKey, type.GUID.ToString("B"))))
            {
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
        }

        public static void UnregisterComObject(Target target, Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var root = target == Target.User ? Registry.CurrentUser : Registry.LocalMachine;
            using (RegistryKey key = root.OpenSubKey(ClsidRegistryKey, true))
            {
                if (key == null)
                    return;

                key.DeleteSubKeyTree(type.GUID.ToString("B"), false);
            }
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
    }
}