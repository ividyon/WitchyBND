using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PPlus;
using SoulsFormats;
using WitchyBND.Parsers;
using WitchyBND.Services;
using WitchyLib;
using ServiceProvider = WitchyBND.Services.ServiceProvider;

namespace WitchyBND.CliModes;

public static class ParseMode
{
    private static readonly IErrorService errorService;
    public static List<WFileParser> Parsers;
    private static readonly IOutputService output;

    static ParseMode()
    {
        output = ServiceProvider.GetService<IOutputService>();
        errorService = ServiceProvider.GetService<IErrorService>();

        Parsers = new List<WFileParser>
        {
            new WDCX(),
            new WPARAMBND3(),
            new WPARAMBND4(),
            new WMATBINBND(),
            new WMTDBND(),
            new WFFXBNDModern(),
            new WBND3(),
            new WBND4(),
            new WBXF3(),
            new WBXF4(),
            new WFFXDLSE(),
            new WFMG(),
            new WGPARAM(),
            new WLUAGNL(),
            new WLUAINFO(),
            new WTPF(),
            new WZERO3(),
            new WFXR3(),
            new WMATBIN(),
            new WMTD(),
            new WPARAM(),
            new WMQB(),
            new WDBSUB(),
            new WENFL(),
            new WLUA(),
            // new WHKX(),
            new WTAEFolder(),
            new WTAEFile(),
            new WMSBEFolder(),
        };
    }
    internal static void CliParseMode(CliOptions opt)
    {
        var paths = opt.Paths.ToList();

        paths = WBUtil.ProcessPathGlobs(paths).ToList();

        ParseFiles(paths);
    }

    public static void ParseFiles(IEnumerable<string> paths, bool recursive = false)
    {
        void Callback(string path)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                if (!recursive)
                    errorService.RegisterNotice($"Path {path} does not exist.");
                return;
            }

            string fileName = Path.GetFileName(path);
            var parsed = false;
            var error = false;

            byte[] data = null;
            bool isDcx = false;
            parsed = errorService.Catch(() => {
                var innerParsed = false;
                DCX.Type compression = DCX.Type.None;
                if (File.Exists(path) && DCX.Is(path))
                {
                    if (!WPARAMBND4.FilenameIsPARAMBND4(path))
                    {
                        output.WriteLine($"Decompressing DCX: {fileName.PromptPlusEscape()}...");

                        isDcx = true;
                        data = DCX.Decompress(path, out DCX.Type compressionVal);
                        compression = compressionVal;
                    }
                }

                foreach (WFileParser parser in Parsers)
                {
                    innerParsed = errorService.Catch(() => {
                        ISoulsFile? file;
                        if ((Configuration.Args.UnpackOnly || !Configuration.Args.RepackOnly) && parser.Exists(path) &&
                            parser.Is(path, data, out file))
                        {
                            Unpack(path, file, compression, parser, recursive);
                            return true;
                        }

                        if ((Configuration.Args.RepackOnly || !Configuration.Args.UnpackOnly) &&
                            parser.ExistsUnpacked(path) && parser.IsUnpacked(path))
                        {
                            Repack(path, parser, recursive);
                            return true;
                        }

                        return false;
                    }, out error, path);

                    if (innerParsed)
                        break;
                }

                // If no other parser present but file is a DCX, at least un-DCX it
                if (!innerParsed && isDcx && !Configuration.Args.RepackOnly)
                {
                    WDCX dcxParser = Parsers.OfType<WDCX>().First();
                    innerParsed = errorService.Catch(() => {
                        Unpack(path, null, compression, dcxParser, false);
                        return true;
                    }, out error, path);
                }

                return innerParsed;
            }, out error, path);

            PrintParseSuccess(path, parsed, error, recursive);
        }

        IEnumerable<string> pathsList = paths.ToList();

        foreach (WFileParser parser in Parsers.Where(p => p.HasPreprocess))
        {
            foreach (string path in pathsList)
            {
                bool toBreak = errorService.Catch(() => parser.Preprocess(path), out _, path);
                if (toBreak)
                    break;
            }
        }

        if (Configuration.Parallel)
            Parallel.ForEach(pathsList, Callback);
        else
            pathsList.ToList().ForEach(Callback);
    }

    public static void PrintParseSuccess(string path, bool parsed, bool error, bool recursive)
    {
        switch (parsed)
        {
            case true:
                if (Configuration.Parallel)
                {
                    string fileName = Path.GetFileName(path);
                    if (recursive)
                        output.WriteLine($"Successfully parsed {fileName.PromptPlusEscape()} (recursive).");
                    else
                        output.WriteLine($"Successfully parsed {fileName.PromptPlusEscape()}.");
                }

                Interlocked.Increment(ref Program.ProcessedItems);
                break;
            case false:
                if (!error)
                {
                    // output.WriteError($"Could not find valid parser for {path.PromptPlusEscape()}.");
                }
                break;
        }
    }

    public static void Unpack(string path, ISoulsFile? file, DCX.Type compression, WFileParser? parser, bool recursive)
    {
        string fileName = Path.GetFileName(path);

        if (compression > file?.Compression)
            file.Compression = compression;

        switch (parser.Verb)
        {
            case WFileParserVerb.Serialize:
                output.WriteLine(recursive
                    ? $"Serializing {parser.Name} (recursive): {fileName.PromptPlusEscape()}..."
                    : $"Serializing {parser.Name}: {fileName.PromptPlusEscape()}...");
                break;
            case WFileParserVerb.Unpack:
                output.WriteLine(recursive
                    ? $"Unpacking {parser.Name} (recursive): {fileName.PromptPlusEscape()}..."
                    : $"Unpacking {parser.Name}: {fileName.PromptPlusEscape()}...");
                break;
            case WFileParserVerb.None:
            default:
                break;
        }

        parser.Unpack(path, file);
    }

    public static void Repack(string path, WFileParser parser, bool recursive)
    {
        string fileName = Path.GetFileName(path);

        switch (parser.Verb)
        {
            case WFileParserVerb.Serialize:
                output.WriteLine(recursive
                    ? $"Deserializing {parser.Name} (recursive): {fileName.PromptPlusEscape()}..."
                    : $"Deserializing {parser.Name}: {fileName.PromptPlusEscape()}...");
                break;
            case WFileParserVerb.Unpack:
                output.WriteLine(recursive
                    ? $"Repacking {parser.Name} (recursive): {fileName.PromptPlusEscape()}..."
                    : $"Repacking {parser.Name}: {fileName.PromptPlusEscape()}...");
                break;
            case WFileParserVerb.None:
            default:
                break;
        }

        if (!parser.UnpackedFitsVersion(path))
        {
            throw new FriendlyException(
                @"Parser version of unpacked file is outdated. Please repack this file using the WitchyBND version it was originally unpacked with, then unpack it using the newest version.");
        }

        parser.Repack(path);
    }
}