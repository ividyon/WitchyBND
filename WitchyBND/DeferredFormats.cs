using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using PPlus;
using WitchyLib;

namespace WitchyBND;

public enum DeferFormat
{
    [Display(Name = "Lua/HKS")] Lua,
    [Display(Name = "HKX")] Hkx,
    // [Display(Name = "ESD")] Esd,
    // [Display(Name="FLVER")] Flver
}

public class DeferFormatConfiguration
{

    public string Path { get; set; }
    public string Arguments { get; set; }

    public DeferFormatConfiguration(string path, string? args = null)
    {
        args ??= "$path";
        Path = path;
        Arguments = args;
    }

    public DeferFormatConfiguration()
    {
    }
}

public class DeferToolPathException : Exception
{
    public DeferFormat Format;

    public DeferToolPathException(DeferFormat format)
    {
        Format = format;
    }
}

public class DeferToolExecutionException : Exception
{
    public DeferFormat Format;
    public int ExitCode;
    public string Error;

    public DeferToolExecutionException(DeferFormat format, int exitCode, string error)
    {
        Format = format;
        ExitCode = exitCode;
        Error = error;
    }
}

public static class DeferredFormatHandling
{

    public static Dictionary<DeferFormat, List<(string, string)>> DefaultDeferToolArguments = new()
    {
        {
            DeferFormat.Lua, new()
            {
                ("DSLuaDecompiler", "-o \"$dirname\\$filename.dec.$fileext\" \"$path\"")
            }
        }
    };

    public static string BuildArgs(DeferFormat format, string srcPath)
    {
        var conf = Configuration.DeferTools[format];
        var dirname = System.IO.Path.GetDirectoryName(srcPath);
        var filename = System.IO.Path.GetFileNameWithoutExtension(srcPath);
        var fileext = System.IO.Path.GetExtension(srcPath);
        return conf.Arguments.Replace("$dirname", dirname).Replace("$filename", filename).Replace("$fileext", fileext);
    }

    public static int CallDeferredTool(DeferFormat format, string srcPath, out string output, out string error)
    {
        var conf = Configuration.DeferTools[format];
        var args = BuildArgs(format, srcPath);
        return ProcessHandling.RunProcess(conf.Path, out output,
            out error, args, System.IO.Path.GetDirectoryName(srcPath));
    }

    public static void Process(DeferFormat format, string srcPath)
    {
        var process = DeferredFormatHandling.CallDeferredTool(format, srcPath, out string output, out string error);
        lock (Program.ConsoleWriterLock)
        {
            PromptPlus.WriteLine(output);
        }
        if (process != 0)
        {
            throw new DeferToolExecutionException(format, process, error);
        }
    }
}