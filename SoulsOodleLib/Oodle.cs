namespace SoulsOodleLib;

public static class Oodle
{
    private static IntPtr? _handle = null;
    public static bool GrabOodle(Action<string> writeLineFunction, bool useFolderPicker = true, bool copyToAppFolder = false, string? gamePath = null)
    {
        if (_handle != null) return true;

        var oodlePath = $@"{AppContext.BaseDirectory}\oo2core_9_win64.dll";
        var oodlePath2 = $@"{AppContext.BaseDirectory}\oo2core_8_win64.dll";
        var oodlePath3 = $@"{AppContext.BaseDirectory}\oo2core_6_win64.dll";
        if (File.Exists(oodlePath))
        {
            _handle = Kernel32.LoadLibrary(oodlePath);
            return true;
        }
        if (File.Exists(oodlePath2))
        {
            _handle = Kernel32.LoadLibrary(oodlePath2);
            return true;
        }
        if (File.Exists(oodlePath3))
        {
            _handle = Kernel32.LoadLibrary(oodlePath3);
            return true;
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
                return false;
            }
        }

        var gameOodlePath = @$"{gamePath}\oo2core_9_win64.dll";
        var gameOodlePath2 = @$"{gamePath}\oo2core_8_win64.dll";
        var gameOodlePath3 = @$"{gamePath}\oo2core_6_win64.dll";

        if (File.Exists(gameOodlePath))
        {
            _handle = Kernel32.LoadLibrary(gameOodlePath);
            if (copyToAppFolder)
                File.Copy(gameOodlePath, $@"{AppDomain.CurrentDomain.BaseDirectory}\{Path.GetFileName(gameOodlePath)}", true);
            return true;
        }

        if (File.Exists(gameOodlePath2))
        {
            _handle = Kernel32.LoadLibrary(gameOodlePath2);
            if (copyToAppFolder)
                File.Copy(gameOodlePath2, $@"{AppDomain.CurrentDomain.BaseDirectory}\{Path.GetFileName(gameOodlePath2)}", true);
            return true;
        }

        if (File.Exists(gameOodlePath3))
        {
            _handle = Kernel32.LoadLibrary(gameOodlePath3);
            if (copyToAppFolder)
                File.Copy(gameOodlePath3, $@"{AppDomain.CurrentDomain.BaseDirectory}\{Path.GetFileName(gameOodlePath2)}", true);
            return true;
        }

        return false;
    }

    private static void KillOodle(IntPtr? oodleHandle)
    {
        if (oodleHandle != null)
            Kernel32.FreeLibrary(oodleHandle.Value);
    }

    public static IntPtr? GetOodleHandle()
    {
        return _handle;
    }

    public static void KillOodle()
    {
        KillOodle(_handle);
    }
}