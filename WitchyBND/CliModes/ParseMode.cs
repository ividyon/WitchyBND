using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SoulsFormats;
using WitchyBND.Parsers;
using WitchyBND.Services;
using WitchyLib;
using ServiceProvider = WitchyBND.Services.ServiceProvider;

namespace WitchyBND.CliModes;

public static class ParseMode
{
    private static readonly IErrorService errorService;
    private static List<WFileParser> _parsers;

    public static List<WFileParser> GetParsers(bool recursive)
    {
        return !recursive ? _parsers : _parsers.Where(p => p.AppliesRecursively).ToList();
    }

    public static List<WFileParser> GetPreprocessors(bool recursive)
    {
        return GetParsers(recursive).Where(p => p.HasPreprocess).ToList();
    }

    public static T GetParser<T>() where T : WFileParser
    {
        return _parsers.OfType<T>().First();
    }

    private static readonly IOutputService output;

    static ParseMode()
    {
        output = ServiceProvider.GetService<IOutputService>();
        errorService = ServiceProvider.GetService<IErrorService>();

        _parsers = new List<WFileParser>
        {
            new WDCX(true),
            new WGFX(),
            new WHKX(),
            new WFLVER(),
            new WLUA(),
            new WPARAMBND3(),
            new WPARAMBND4(),
            new WMATBINBND(),
            new WMTDBND(),
            new WFFXBNDModern(),
            new WANIBND4(),
            new WBND3(),
            new WBND4(),
            new WBXF3(),
            new WBXF4(),
            new WFFXDLSE(),
            new WFMG(),
            // new WGPARAM(),
            new WLUAGNL(),
            new WLUAINFO(),
            new WTPF(),
            new WZERO3(),
            new WFXR1(),
            new WFXR3(),
            new WMATBIN(),
            new WMTD(),
            new WPARAM(),
            new WMQB(),
            new WDBSUB(),
            new WENFL(),
            new WTAEFolder(),
            new WTAEFile(),
            new WMSBEFolder(),
            new WAIP(),
            new WDCX(false)
        };
    }

    internal static void CliParseMode(CliOptions opt)
    {
        var paths = opt.Paths.ToList();

        paths = OSPath.ProcessPathGlobs(paths).ToList();

        ParseFiles(paths, false);
    }

    public static void ParseFiles(IEnumerable<string> paths, bool recursive)
    {
        var parsers = GetParsers(recursive);

        IEnumerable<string> pathsList = paths.ToList();

        Dictionary<string, (WFileParser, ISoulsFile)> preprocessedFiles = new();

        if (!recursive)
        {
            output.WriteLine($"Preprocessing...");
            foreach (string path in pathsList.Except(preprocessedFiles.Select(p => p.Key)))
            {
                foreach (WFileParser parser in GetPreprocessors(recursive))
                {
                    bool toBreak = errorService.Catch(() => parser.Preprocess(path, recursive, ref preprocessedFiles),
                        out _, path);
                    if (toBreak)
                        break;
                }
            }
            output.WriteLine($"Preprocessing complete.");
        }

        if (Configuration.Active.Parallel)
            Parallel.ForEach(pathsList, Callback);
        else
            pathsList.ToList().ForEach(Callback);
        return;

        void Callback(string path)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                if (!recursive)
                    errorService.RegisterNotice($"Path {path} does not exist.");
                return;
            }

            var parsed = false;
            var error = false;

            byte[] data = null;

            parsed = errorService.Catch(() => {
                var innerParsed = false;
                var innerError = false;
                DCX.CompressionInfo compression = new DCX.NoCompressionInfo();
                ISoulsFile? file = null;
                if (preprocessedFiles.ContainsKey(path))
                {
                    parsers = [preprocessedFiles[path].Item1];
                    file = preprocessedFiles[path].Item2;
                    compression = file.Compression;
                }

                foreach (WFileParser parser in parsers)
                {
                    bool innerMatch = false;

                    innerParsed = errorService.Catch(() => {
                        ISoulsFile? parsedFile = null;
                        if ((Configuration.Active.UnpackOnly || !Configuration.Active.RepackOnly) &&
                            (file != null ||
                                (parser.Exists(path) && parser.Is(path, data, out parsedFile))))
                        {
                            file ??= parsedFile;
                            innerMatch = true;
                            Unpack(path, file, compression, parser, recursive);
                            return true;
                        }

                        if ((Configuration.Active.RepackOnly || !Configuration.Active.UnpackOnly) &&
                            parser.ExistsUnpacked(path) && parser.IsUnpacked(path))
                        {
                            innerMatch = true;
                            Repack(path, parser, recursive);
                            return true;
                        }

                        return false;
                    }, out innerError, path);

                    if (innerMatch)
                        break;
                }

                // // If no other parser present but file is a DCX, at least un-DCX it
                // if (!innerParsed && !error && isDcx && !Configuration.Active.RepackOnly)
                // {
                //     WDCX dcxParser = _parsers.OfType<WDCX>().First();
                //     innerParsed = errorService.Catch(() => {
                //         Unpack(path, null, compression, dcxParser, false);
                //         return true;
                //     }, out error, path);
                // }

                return innerParsed;
            }, out error, path);

            PrintParseSuccess(path, parsed, error, recursive);
        }
    }

    public static void PrintParseSuccess(string path, bool parsed, bool error, bool recursive)
    {
        switch (parsed)
        {
            case true:
                if (Configuration.Active.Parallel)
                {
                    string fileName = OSPath.GetFileName(path);
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

    public static void Unpack(string path, ISoulsFile? file, DCX.CompressionInfo compression, WFileParser? parser, bool recursive)
    {
        string fileName = OSPath.GetFileName(path);

        if (compression.Type > file?.Compression.Type)
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

        parser.Unpack(path, file, recursive);
    }

    public static void Repack(string path, WFileParser parser, bool recursive)
    {
        string fileName = OSPath.GetFileName(path);

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

        parser.Repack(path, recursive);
    }
}