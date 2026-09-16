using System.Runtime.InteropServices;
using SoulsFormats;
using NativeLibrary = SoulsFormats.NativeLibrary;

namespace SoulsOodleLib;

public static class Oodle
{
    private static IntPtr _handle = IntPtr.Zero;
    public static IntPtr GrabOodle(Action<string> writeLineFunction, bool useFolderPicker = true, bool copyToAppFolder = false, string? gamePath = null)
    {
        if (_handle != IntPtr.Zero) return _handle;

        var isMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        Dictionary<OodleVersion, string> localOodlePaths =
        new() {
            { OodleVersion.Oodle9, Path.Combine(AppContext.BaseDirectory, $"oo2core_9_win64.dll") },
            { OodleVersion.Oodle8, Path.Combine(AppContext.BaseDirectory, $"oo2core_8_win64.dll") },
            { OodleVersion.Oodle6, Path.Combine(AppContext.BaseDirectory, $"oo2core_6_win64.dll") }
        };
        if (isMacOS)
        {
            localOodlePaths = new() {
                { OodleVersion.Oodle9, Path.Combine(AppContext.BaseDirectory, $"liboo2coremac64.2.9.dylib") },
                { OodleVersion.Oodle8, Path.Combine(AppContext.BaseDirectory, $"liboo2coremac64.2.8.dylib") },
                { OodleVersion.Oodle6, Path.Combine(AppContext.BaseDirectory, $"liboo2coremac64.2.6.dylib") },
            };
        }
        else if (isLinux)
        {
            localOodlePaths = new() {
                { OodleVersion.Oodle9, Path.Combine(AppContext.BaseDirectory, $"liboo2corelinux64.so.9") },
                { OodleVersion.Oodle8, Path.Combine(AppContext.BaseDirectory, $"liboo2corelinux64.so.8") },
                { OodleVersion.Oodle6, Path.Combine(AppContext.BaseDirectory, $"liboo2corelinux64.so.6") },
            };
        }

        foreach ((OodleVersion ver, string localOodlePath) in localOodlePaths)
        {
            if (!File.Exists(localOodlePath)) continue;
            _handle = NativeLibrary.LoadLibrary(localOodlePath);
            SoulsFormats.Oodle.OodlePtrs[ver] = _handle;
            return _handle;
        }

        if (gamePath == null)
        {
            gamePath = AssetLocator.GetGamePath(new List<AssetLocator.Game>
                {
                    AssetLocator.Game.Nightreign,
                    AssetLocator.Game.ArmoredCore6,
                    AssetLocator.Game.EldenRing,
                    AssetLocator.Game.Sekiro,
                },
                writeLineFunction, useFolderPicker);

            if (gamePath == null)
            {
                if (isMacOS)
                    writeLineFunction("Could not find Oodle compression library (liboo2coremac64.2.9.dylib). Please provide it in the application folder.");
                else if (isLinux)
                    writeLineFunction("Could not find Oodle compression library (liboo2corelinux64.so.9). Please provide it in the application folder.");
                else
                    writeLineFunction("Could not find Oodle compression library (oo2core_*_win64.dll). Please copy it from your Game folder into the application folder.");
                return IntPtr.Zero;
            }
        }

        Dictionary<OodleVersion, string> gameOodlePaths =
            new() {
                { OodleVersion.Oodle9, Path.Combine(gamePath, $"oo2core_9_win64.dll") },
                { OodleVersion.Oodle8, Path.Combine(gamePath, $"oo2core_8_win64.dll") },
                { OodleVersion.Oodle6, Path.Combine(gamePath, $"oo2core_6_win64.dll") }
            };
        if (isMacOS)
        {
            gameOodlePaths = new() {
                { OodleVersion.Oodle9, Path.Combine(gamePath, $"liboo2coremac64.2.9.dylib") },
                { OodleVersion.Oodle8, Path.Combine(gamePath, $"liboo2coremac64.2.8.dylib") },
                { OodleVersion.Oodle6, Path.Combine(gamePath, $"liboo2coremac64.2.6.dylib") },
            };
        }
        else if (isLinux)
        {
            gameOodlePaths = new() {
                { OodleVersion.Oodle9, Path.Combine(gamePath, $"liboo2corelinux64.so.9") },
                { OodleVersion.Oodle8, Path.Combine(gamePath, $"liboo2corelinux64.so.8") },
                { OodleVersion.Oodle6, Path.Combine(gamePath, $"liboo2corelinux64.so.6") },
            };
        }

        foreach ((OodleVersion ver, string gameOodlePath) in gameOodlePaths)
        {
            if (!File.Exists(gameOodlePath)) continue;
            _handle = NativeLibrary.LoadLibrary(gameOodlePath);
            if (copyToAppFolder)
                File.Copy(gameOodlePath, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(gameOodlePath)), true);
            SoulsFormats.Oodle.OodlePtrs[ver] = _handle;
            return _handle;
        }

        if (isMacOS)
            writeLineFunction("Could not find Oodle compression library (liboo2coremac64.2.9.dylib). Please provide it in the application folder.");
        else if (isLinux)
            writeLineFunction("Could not find Oodle compression library (liboo2corelinux64.so.9). Please provide it in the application folder.");
        else
            writeLineFunction("Could not find Oodle compression library (oo2core_*_win64.dll). Please copy it from your Game folder into the application folder.");
        return IntPtr.Zero;
    }

    public static void KillOodle(IntPtr? oodleHandle)
    {
        if (oodleHandle != null)
            NativeLibrary.FreeLibrary(oodleHandle.Value);
    }

    public static IntPtr GetOodleHandle()
    {
        return _handle;
    }

    public static void KillOodle()
    {
        KillOodle(_handle);
    }
}