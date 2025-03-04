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
    [Display(Name = "FLVER")] Flver,
    [Display(Name = "GFX")] Gfx
}

public class DeferArgs(string name, string unpackArgs = "$path", string? repackArgs = null)
{
    public string Name { get; private set; } = name;
    public string UnpackArgs { get; private set; } = unpackArgs;
    public string? RepackArgs { get; private set; } = repackArgs;
}

public class DeferConfig(string name, string path, string unpackArgs = "$path", string? repackArgs = null) : DeferArgs(name, unpackArgs, repackArgs)
{
    public string Path { get; private set; } = path;
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


    public static Dictionary<DeferFormat, List<DeferArgs>> DefaultDeferToolArguments = new()
    {
        {
            DeferFormat.Lua,
            new()
            {
                new DeferArgs("DSLuaDecompiler", "-o \"$dirname\\$filename.dec$fileext\" \"$path\"", null)
            }
        },
        {
            DeferFormat.Flver,
            new()
            {
                new DeferArgs("SoulsModelTool (ELDEN RING)", "-tofbx -meshnameismatname -transformmesh \"$path\"", null),
                new DeferArgs("SoulsModelTool (ELDEN RING) (Z up)", "-tofbx -meshnameismatname -transformmesh -z_up \"$path\"", null)
            }
        },
        {
            DeferFormat.Gfx,
            new()
            {
                new DeferArgs("JPEXS Free Flash Decompiler (to XML)", "-swf2xml \"$path\" \"$dirname\\$filename.gfx.xml\"", "-xml2swf \"$path\" \"$dirname\\$filename.gfx\"")
            }
        }
    };

    public static string BuildArgs(string args, string srcPath)
    {
        var dirname = Path.GetDirectoryName(srcPath);
        var filename = WBUtil.GetFileNameWithoutAnyExtensions(srcPath);
        var fileext = WBUtil.GetFullExtensions(srcPath);
        return args
            .Replace("$dirname", dirname)
            .Replace("$filename", filename)
            .Replace("$fileext", fileext)
            .Replace("$path", srcPath);
    }
    public static void Unpack(DeferFormat format, string srcPath)
    {
        var conf = Configuration.Active.DeferTools[format];
        var args = BuildArgs(conf.UnpackArgs, srcPath);

        output.WriteLine($"Running deferred tool: \"{conf.Path} {args}\"");
        var process = ProcessHandling.RunProcess(conf.Path, out var text,
            out var error, args, Path.GetDirectoryName(srcPath));
        output.WriteLine(text);

        if (process != 0)
        {
            throw new DeferToolExecutionException(format, process, error);
        }
    }
    public static void Repack(DeferFormat format, string srcPath)
    {
        var conf = Configuration.Active.DeferTools[format];
        if (string.IsNullOrWhiteSpace(conf.RepackArgs)) return;

        var args = BuildArgs(conf.RepackArgs, srcPath);

        output.WriteLine($"Running deferred tool: \"{conf.Path} {args}\"");
        var process = ProcessHandling.RunProcess(conf.Path, out var text,
            out var error, args, Path.GetDirectoryName(srcPath));
        output.WriteLine(text);

        if (process != 0)
        {
            throw new DeferToolExecutionException(format, process, error);
        }
    }
}