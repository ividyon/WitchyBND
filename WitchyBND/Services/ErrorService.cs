using System;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using SoulsFormats.Exceptions;
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
        public void CriticalError(string message);
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
                output.WriteError($"{notice.Source}: {notice.Message}".PromptPlusEscape());
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
                if (error.Source != null)
                    output.WriteError($"{error.Source}: {error.Message}".PromptPlusEscape());
                else
                    output.WriteError(error.Message.PromptPlusEscape());
            }

            if (Configuration.IsTest)
                throw new Exception(error.Message);
        }

        public void CriticalError(string message)
        {
            output.WriteError(message.PromptPlusEscape());
            output.WriteLine("The application will now shut down.");
            output.KeyPress("Press any key to continue...").Run();
            Environment.Exit(0);
        }


        public void PrintIssues()
        {
            if (AccruedErrors.Count > 0)
            {
                output.WriteLine("");
                output.SingleDash("Errors during operation");
                foreach (WitchyError error in AccruedErrors)
                {
                    output.WriteError(
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
                        output.WriteError($"{notice.Source}: {notice.Message}"
                            .PromptPlusEscape());
                    else
                        output.WriteError($"{notice.Message}".PromptPlusEscape());
                }
            }
        }

        public bool Catch(Func<bool> callback, out bool error, string? source = null)
        {
            error = false;
            try
            {
                return callback();
            }
            catch (ProcessUserInputException e) when (!Configuration.IsTest && !Configuration.IsDebug)
            {
                error = true;
                RegisterError(new WitchyError($@"The external process ""{e.ProcessName}"" was waiting for user input.

Process output:
{e.Output}

Process error output:
{e.Error}", source,
                    WitchyErrorType.Generic));
            }
            catch (GameUnsupportedException e) when (!Configuration.IsTest && !Configuration.IsDebug)
            {
                error = true;
                RegisterError(new WitchyError($"The parser does not support the game {e.Game}.", source,
                    WitchyErrorType.Generic));
            }
            catch (DeferToolExecutionException e) when (!Configuration.IsTest && !Configuration.IsDebug)
            {
                error = true;
                RegisterError(new WitchyError(
                    @$"The {e.Format.GetAttribute<DisplayAttribute>().Name} tool located at ""{Configuration.Active.DeferTools[e.Format].Path}"" exited with code {e.ExitCode} with the following error message:

{e.Error}", source, WitchyErrorType.Generic));
            }
            catch (DeferToolPathException e) when (!Configuration.IsTest && !Configuration.IsDebug)
            {
                error = true;
                RegisterError(new WitchyError(
                    $"No tool is configured for the deferred format \"{e.Format.GetAttribute<DisplayAttribute>().Name}\". Please configure it in WitchyBND before attempting to process files of this type.",
                    source, WitchyErrorType.Generic));
            }
            catch (Exception e) when (!Configuration.IsTest && !Configuration.IsDebug && e.Message.Contains("oo2core_6_win64.dll") ||
                                      e.Message.Contains("oo2core_8_win64.dll") || e is NoOodleFoundException)
            {
                error = true;
                if (Configuration.IsTest)
                    throw;

                RegisterError(new WitchyError(
                    "ERROR: Oodle DLL not found. Please copy oo2core_6_win64.dll or oo2core_8_win64.dll from the game directory to WitchyBND's directory.",
                    WitchyErrorType.NoOodle));
            }
            catch (UnauthorizedAccessException) when (!Configuration.IsTest && !Configuration.IsDebug)
            {
                error = true;

                RegisterError(new WitchyError(
                    "WitchyBND had no access to perform this action; perhaps try Administrator Mode?", source,
                    WitchyErrorType.NoAccess));
            }
            catch (FriendlyException e) when (!Configuration.IsTest)
            {
                error = true;

                RegisterError(new WitchyError(e.Message, source));
            }
            catch (Exception e) when (!Configuration.IsTest && !Configuration.IsDebug)
            {
                error = true;

                RegisterException(e, source);
            }

            return false;
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
        public string? Source { get; set; }
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