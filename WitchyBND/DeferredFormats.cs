using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using WitchyBND.Services;
using WitchyLib;

namespace WitchyBND;

public enum DeferFormat
{
    [Display(Name = "Lua/HKS")] Lua,
    [Display(Name = "HKX")] Hkx,
    // [Display(Name = "ESD")] Esd,
    // [Display(Name = "FLVER")] Flver
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
    private static readonly IOutputService output;
    static DeferredFormatHandling()
    {
        output = ServiceProvider.GetService<IOutputService>();
    }


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
        var dirname = Path.GetDirectoryName(srcPath);
        var filename = Path.GetFileNameWithoutExtension(srcPath);
        var fileext = Path.GetExtension(srcPath);
        return conf.Arguments.Replace("$dirname", dirname).Replace("$filename", filename).Replace("$fileext", fileext);
    }

    public static int CallDeferredTool(DeferFormat format, string srcPath, out string output, out string error)
    {
        var conf = Configuration.DeferTools[format];
        var args = BuildArgs(format, srcPath);
        return ProcessHandling.RunProcess(conf.Path, out output,
            out error, args, Path.GetDirectoryName(srcPath));
    }

    public static void Process(DeferFormat format, string srcPath)
    {
        var process = CallDeferredTool(format, srcPath, out string text, out string error);
        output.WriteLine(text);

        if (process != 0)
        {
            throw new DeferToolExecutionException(format, process, error);
        }
    }
}