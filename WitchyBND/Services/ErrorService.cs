using System;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using PPlus;
using SoulsFormats;
using WitchyBND;
using WitchyBND.Errors;
using WitchyLib;

namespace WitchyBND.Services
{

    public interface IErrorService
    {
        public int AccruedErrorCount { get; }
        public int AccruedNoticeCount { get; }

        public void RegisterNotice(string message, bool write = true);
        public void RegisterNotice(WitchyNotice notice, bool write = true);
        public void RegisterException(Exception e, string? source = null);
        public void RegisterError(string message, bool write = true);
        public void RegisterError(WitchyError error, bool write = true);
        public void PrintIssues();

        public bool Catch(Func<bool> callback, out bool error, string? source = null);
    }

    public class ErrorService : IErrorService
    {
        private ConcurrentStack<WitchyError> AccruedErrors;
        private ConcurrentStack<WitchyNotice> AccruedNotices;
        private readonly IOutputService output;

        public int AccruedErrorCount => AccruedErrors.Count;
        public int AccruedNoticeCount => AccruedNotices.Count;

        public ErrorService(IOutputService outputService)
        {
            output = outputService;
            AccruedErrors = new();
            AccruedNotices = new();
        }

        public void RegisterNotice(string message, bool write = true)
        {
            RegisterNotice(new WitchyNotice(message), write);
        }

        public void RegisterNotice(WitchyNotice notice, bool write = true)
        {
            AccruedNotices.Push(notice);
            if (write)
            {
                lock (output.ConsoleWriterLock)
                    output.Error.WriteLine($"{notice.Source}: {notice.Message}".PromptPlusEscape());
            }
        }

        public void RegisterException(Exception e, string? source = null)
        {
            switch (e)
            {
                case UnsupportedActionException:
                    RegisterError(new WitchyError($@"Unsupported user action:
{e.ToString().PromptPlusEscape()}
", source, WitchyErrorType.Exception, 1));
                    break;
                default:
                    RegisterError(new WitchyError($@"Unhandled exception:
{e.ToString().PromptPlusEscape()}
", source, WitchyErrorType.Exception, 1));
                    break;
            }
        }

        public void RegisterError(string message, bool write = true)
        {
            RegisterError(new WitchyError(message), write);
        }

        public void RegisterError(WitchyError error, bool write = true)
        {
            AccruedErrors.Push(error);
            if (write)
            {
                lock (output.ConsoleWriterLock)
                {
                    if (error.Source != null)
                        output.Error.WriteLine($"{error.Source}: {error.Message}".PromptPlusEscape());
                    else
                        output.Error.WriteLine(error.Message.PromptPlusEscape());
                }
            }

            if (Configuration.IsTest)
                throw new Exception(error.Message);
        }


        public void PrintIssues()
        {
            if (AccruedErrors.Count > 0)
            {
                output.WriteLine("");
                output.SingleDash("Errors during operation");
                foreach (WitchyError error in AccruedErrors)
                {
                    output.Error.WriteLine(
                        $"{error.Source}: {error.Message}".PromptPlusEscape());
                }
            }

            if (AccruedNotices.Count > 0)
            {
                output.WriteLine("");
                output.SingleDash("Notices during operation");
                foreach (WitchyNotice notice in AccruedNotices)
                {
                    if (notice.Source != null)
                        output.Error.WriteLine($"{notice.Source}: {notice.Message}"
                            .PromptPlusEscape());
                    else
                        output.Error.WriteLine($"{notice.Message}".PromptPlusEscape());
                }
            }
        }

        public bool Catch(Func<bool> callback, out bool error, string? source = null)
        {
            error = false;
            bool outcome = false;
            try
            {
                outcome = callback();
            }
            catch (ProcessUserInputException e)
            {
                if (Configuration.IsTest)
                    throw;
                RegisterError(new WitchyError($@"The external process ""{e.ProcessName}"" was waiting for user input.

Process output:
{e.Output}

Process error output:
{e.Error}", source,
                    WitchyErrorType.Generic));
                error = true;
            }
            catch (GameUnsupportedException e)
            {
                if (Configuration.IsTest)
                    throw;
                RegisterError(new WitchyError($"The parser does not support the game {e.Game}.", source,
                    WitchyErrorType.Generic));
                error = true;
            }
            catch (DeferToolExecutionException e)
            {
                if (Configuration.IsTest)
                    throw;
                RegisterError(new WitchyError(
                    @$"The {e.Format.GetAttribute<DisplayAttribute>().Name} tool located at ""{Configuration.DeferTools[e.Format].Path}"" exited with code {e.ExitCode} with the following error message:

{e.Error}", source, WitchyErrorType.Generic));
                error = true;
            }
            catch (DeferToolPathException e)
            {
                if (Configuration.IsTest)
                    throw;
                RegisterError(new WitchyError(
                    $"No tool is configured for the deferred format \"{e.Format.GetAttribute<DisplayAttribute>().Name}\". Please configure it in WitchyBND before attempting to process files of this type.",
                    source, WitchyErrorType.Generic));
                error = true;
            }
            catch (NoOodleFoundException)
            {
                if (Configuration.IsTest)
                    throw;

                RegisterError(new WitchyError(
                    "ERROR: Oodle DLL not found. Please copy oo2core_6_win64.dll or oo2core_8_win64.dll from the game directory to WitchyBND's directory.",
                    WitchyErrorType.NoOodle));
                error = true;
            }
            catch (Exception e) when (e.Message.Contains("oo2core_6_win64.dll") ||
                                      e.Message.Contains("oo2core_8_win64.dll") || e is NoOodleFoundException)
            {
                if (Configuration.IsTest)
                    throw;

                RegisterError(new WitchyError(
                    "ERROR: Oodle DLL not found. Please copy oo2core_6_win64.dll or oo2core_8_win64.dll from the game directory to WitchyBND's directory.",
                    WitchyErrorType.NoOodle));
                error = true;
            }
            catch (UnauthorizedAccessException)
            {
                if (Configuration.IsTest || Configuration.IsDebug)
                    throw;

                RegisterError(new WitchyError(
                    "WitchyBND had no access to perform this action; perhaps try Administrator Mode?", source,
                    WitchyErrorType.NoAccess));
                error = true;
            }
            catch (IOException e) when (e is not FileNotFoundException)
            {
                if (Configuration.IsDebug) throw;

                RegisterError(new WitchyError(
                    "WitchyBND could not operate on the file as it was being used by another process.", source,
                    WitchyErrorType.InUse));
                error = true;
            }
            catch (FriendlyException e)
            {
                if (Configuration.IsTest)
                    throw;

                RegisterError(new WitchyError(e.Message, source));
                error = true;
            }
            catch (Exception e)
            {
                if (Configuration.IsTest || Configuration.IsDebug)
                    throw;

                RegisterException(e, source);
                error = true;
            }

            return outcome;
        }
    }
}

namespace WitchyBND.Errors
{
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

    public class ProcessUserInputException : Exception
    {
        public string ProcessName;
        public string Output;
        public string Error;
        public ProcessUserInputException(Process process, string message) : base(message)
        {
            ProcessName = process.ProcessName;
            Output = process.StandardOutput.ReadToEnd();
            Error = process.StandardError.ReadToEnd();
        }
        public ProcessUserInputException(Process process) : base($"The process \"{process.ProcessName}\" requested user input.")
        {
            ProcessName = process.ProcessName;
            Output = process.StandardOutput.ReadToEnd();
            Error = process.StandardError.ReadToEnd();
        }
    }

    public class GameUnsupportedException : Exception
    {
        public WBUtil.GameType Game;

        public GameUnsupportedException(WBUtil.GameType game, string message) : base(message)
        {
            Game = game;
        }

        public GameUnsupportedException(WBUtil.GameType game)
        {
            Game = game;
        }
    }
}