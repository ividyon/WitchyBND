using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using PPlus;
using SoulsFormats;
using WitchyBND.CliModes;
using WitchyFormats;
using WitchyLib;

namespace WitchyBND;

public static class Catcher
{
    public static bool Catch(Func<bool> callback, out bool error, string? source = null)
    {
        error = false;
        bool outcome = false;
        try
        {
            outcome = callback();
        }
        catch (GameUnsupportedException e)
        {
            if (Configuration.IsTest)
                throw;
            Program.RegisterError(new WitchyError($"The parser does not support the game {e.Game}.", source, WitchyErrorType.Generic));
            error = true;
        }
        catch (DeferToolExecutionException e)
        {
            if (Configuration.IsTest)
                throw;
            Program.RegisterError(new WitchyError(@$"The {e.Format.GetAttribute<DisplayAttribute>().Name} tool located at ""{Configuration.DeferTools[e.Format].Path}"" exited with code {e.ExitCode} with the following error message:

{e.Error}", source, WitchyErrorType.Generic));
            error = true;
        }
        catch (DeferToolPathException e)
        {
            if (Configuration.IsTest)
                throw;
            Program.RegisterError(new WitchyError($"No tool is configured for the deferred format \"{e.Format.GetAttribute<DisplayAttribute>().Name}\". Please configure it in WitchyBND before attempting to process files of this type.", source, WitchyErrorType.Generic));
            error = true;
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
            if (Configuration.IsTest || Configuration.IsDebug)
                throw;

            Program.RegisterError(new WitchyError(
                "WitchyBND had no access to perform this action; perhaps try Administrator Mode?", source,
                WitchyErrorType.NoAccess));
            error = true;
        }
        catch (IOException e) when (e is not FileNotFoundException)
        {
            if (Configuration.IsDebug) throw;

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
        return outcome;
    }
}
public enum WitchyErrorType
{
    Generic,
    NoOodle,
    NoAccess,
    InUse,
    Exception
}

public class WitchyError
{
    public string Message { get; set; }
    public WitchyErrorType Type { get; set; } = WitchyErrorType.Generic;
    public string? Source { get; set; } = null;
    public short ErrorCode { get; set; } = -1;

    public WitchyError(string message)
    {
        Message = message;
    }

    public WitchyError(string message, string? source)
    {
        Message = message;
        Source = source;
    }

    public WitchyError(string message, string? source, WitchyErrorType type)
    {
        Message = message;
        Source = source;
        Type = type;
    }

    public WitchyError(string message, string? source, WitchyErrorType type, short errorCode)
    {
        Message = message;
        Source = source;
        Type = type;
        ErrorCode = errorCode;
    }

    public WitchyError(string message, string? source, short errorCode)
    {
        Message = message;
        Source = source;
        ErrorCode = errorCode;
    }

    public WitchyError(string message, WitchyErrorType type)
    {
        Message = message;
        Type = type;
    }

    public WitchyError(string message, short errorCode)
    {
        Message = message;
        ErrorCode = errorCode;
    }
}

public class WitchyNotice
{
    public string Message { get; set; }
    public string Source { get; set; } = "Notice";

    public WitchyNotice(string message)
    {
        Message = message;
    }

    public WitchyNotice(string message, string source)
    {
        Message = message;
        Source = source;
    }
}

public class GameUnsupportedException : Exception
{
    public WBUtil.GameType Game;

    public GameUnsupportedException(WBUtil.GameType game, string message) : base(message)
    {
        Game = game;
    }
    public GameUnsupportedException(WBUtil.GameType game) {
        Game = game;
    }
}