using System;
using System.IO;
using PPlus;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND;

public static class Catcher
{
    public static bool Catch(Func<bool> callback, out bool error, string? source = null)
    {
        bool outcome = false;
        try
        {
            outcome = callback();
        }
        catch (NoOodleFoundException)
        {
            if (Configuration.IsTest)
                throw;

            Program.RegisterError(new WitchyError(
                "ERROR: Oodle DLL not found. Please copy oo2core_6_win64.dll or oo2core_8_win64.dll from the game directory to WitchyBND's directory.",
                WitchyErrorType.NoOodle));
            error = true;
        }
        catch (Exception e) when (e.Message.Contains("oo2core_6_win64.dll") ||
                                  e.Message.Contains("oo2core_8_win64.dll") || e is NoOodleFoundException)
        {
            if (Configuration.IsTest)
                throw;

            Program.RegisterError(new WitchyError(
                "ERROR: Oodle DLL not found. Please copy oo2core_6_win64.dll or oo2core_8_win64.dll from the game directory to WitchyBND's directory.",
                WitchyErrorType.NoOodle));
            error = true;
        }
        catch (UnauthorizedAccessException)
        {
            if (Configuration.IsTest)
                throw;

            Program.RegisterError(new WitchyError(
                "WitchyBND had no access to perform this action; perhaps try Administrator Mode?", source,
                WitchyErrorType.NoAccess));
            error = true;
        }
        catch (IOException)
        {
            Program.RegisterError(new WitchyError("WitchyBND could not operate on the file as it was being used by another process.", source, WitchyErrorType.InUse));
            error = true;
        }
        catch (FriendlyException e)
        {
            if (Configuration.IsTest)
                throw;

            Program.RegisterError(new WitchyError(e.Message, source));
            error = true;
        }
        catch (Exception e)
        {
            if (Configuration.IsTest || Configuration.IsDebug)
                throw;

            Program.RegisterException(e, source);
            error = true;
        }
        error = false;
        return outcome;
    }
}