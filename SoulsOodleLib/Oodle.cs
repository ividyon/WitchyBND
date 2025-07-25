namespace SoulsOodleLib;

public static class Oodle
{
    private static IntPtr _handle = IntPtr.Zero;
    public static IntPtr GrabOodle(Action<string> writeLineFunction, bool useFolderPicker = true, bool copyToAppFolder = false, string? gamePath = null)
    {
        if (_handle != IntPtr.Zero) return _handle;

        var oodlePath9 = $@"{AppContext.BaseDirectory}\oo2core_9_win64.dll";
        var oodlePath8 = $@"{AppContext.BaseDirectory}\oo2core_8_win64.dll";
        var oodlePath6 = $@"{AppContext.BaseDirectory}\oo2core_6_win64.dll";

        if (File.Exists(oodlePath9))
        {
            _handle = Kernel32.LoadLibrary(oodlePath9);
            SoulsFormats.Oodle.Oodle9Ptr = _handle;
            return _handle;
        }
        if (File.Exists(oodlePath8))
        {
            _handle = Kernel32.LoadLibrary(oodlePath8);
            SoulsFormats.Oodle.Oodle8Ptr = _handle;
            return _handle;
        }
        if (File.Exists(oodlePath6))
        {
            _handle = Kernel32.LoadLibrary(oodlePath6);
            SoulsFormats.Oodle.Oodle6Ptr = _handle;
            return _handle;
        }

        if (gamePath == null)
        {
            gamePath = AssetLocator.GetGamePath(new List<AssetLocator.Game>
                {
                    AssetLocator.Game.ArmoredCore6,
                    AssetLocator.Game.EldenRing,
                    AssetLocator.Game.Sekiro,
                    AssetLocator.Game.Nightreign
                },
                writeLineFunction, useFolderPicker);

            if (gamePath == null)
            {
                writeLineFunction("Could not find Oodle compression library (oo2core DLL). Please copy it from your Game folder into the application folder.");
                return IntPtr.Zero;
            }
        }

        var gameOodlePath9 = @$"{gamePath}\oo2core_9_win64.dll";
        var gameOodlePath8 = @$"{gamePath}\oo2core_8_win64.dll";
        var gameOodlePath6 = @$"{gamePath}\oo2core_6_win64.dll";

        if (File.Exists(gameOodlePath9))
        {
            _handle = Kernel32.LoadLibrary(gameOodlePath9);
            if (copyToAppFolder)
                File.Copy(gameOodlePath9, $@"{AppDomain.CurrentDomain.BaseDirectory}\{Path.GetFileName(gameOodlePath9)}", true);
            SoulsFormats.Oodle.Oodle9Ptr = _handle;
            return _handle;
        }

        if (File.Exists(gameOodlePath8))
        {
            _handle = Kernel32.LoadLibrary(gameOodlePath8);
            if (copyToAppFolder)
                File.Copy(gameOodlePath8, $@"{AppDomain.CurrentDomain.BaseDirectory}\{Path.GetFileName(gameOodlePath8)}", true);
            SoulsFormats.Oodle.Oodle8Ptr = _handle;
            return _handle;
        }

        if (File.Exists(gameOodlePath6))
        {
            _handle = Kernel32.LoadLibrary(gameOodlePath6);
            if (copyToAppFolder)
                File.Copy(gameOodlePath6, $@"{AppDomain.CurrentDomain.BaseDirectory}\{Path.GetFileName(gameOodlePath6)}", true);
            SoulsFormats.Oodle.Oodle6Ptr = _handle;
            return _handle;
        }

        return IntPtr.Zero;
    }

    private static void KillOodle(IntPtr? oodleHandle)
    {
        if (oodleHandle != null)
            Kernel32.FreeLibrary(oodleHandle.Value);
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