using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PPlus;
using SoulsFormats;
using WitchyBND.Parsers;
using WitchyLib;

namespace WitchyBND.CliModes;

public static class ParseMode
{
    public static List<WFileParser> Parsers;

    internal static void CliParseMode(CliOptions opt)
    {
        var paths = opt.Paths.ToList();

        paths = WBUtil.ProcessPathGlobs(paths);

        ParseFiles(paths);
    }

    public static void ParseFiles(IEnumerable<string> paths, bool recursive = false)
    {
        void Callback(string path) {
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                if (!recursive)
                    Program.RegisterNotice($"Path {path} does not exist.");
                return;
            }

            string fileName = Path.GetFileName(path);
            var parsed = false;
            var error = false;

            byte[] data = null;
            DCX.Type compression = DCX.Type.None;
            if (File.Exists(path) && DCX.Is(path) && !WPARAMBND4.FilenameIsPARAMBND4(path))
            {
                PromptPlus.WriteLine($"Decompressing DCX: {fileName.PromptPlusEscape()}...");
                data = DCX.Decompress(path, out DCX.Type compressionVal);
                compression = compressionVal;
            }

            foreach (WFileParser parser in Parsers)
            {
                try
                {
                    ISoulsFile? file;
                    if ((Configuration.Args.UnpackOnly || !Configuration.Args.RepackOnly) && parser.Exists(path) &&
                        parser.Is(path, data, out file))
                    {
                        if (compression > file?.Compression)
                            file.Compression = compression;
                        switch (parser.Verb)
                        {
                            case WFileParserVerb.Serialize:
                                PromptPlus.WriteLine(recursive
                                    ? $"Serializing {parser.Name} (recursive): {fileName.PromptPlusEscape()}..."
                                    : $"Serializing {parser.Name}: {fileName.PromptPlusEscape()}...");
                                break;
                            case WFileParserVerb.Unpack:
                                PromptPlus.WriteLine(recursive
                                    ? $"Unpacking {parser.Name} (recursive): {fileName.PromptPlusEscape()}..."
                                    : $"Unpacking {parser.Name}: {fileName.PromptPlusEscape()}...");
                                break;
                            case WFileParserVerb.None:
                            default:
                                break;
                        }

                        parser.Unpack(path, file);
                        parsed = true;
                        break;
                    }

                    if ((Configuration.Args.RepackOnly || !Configuration.Args.UnpackOnly) &&
                        parser.ExistsUnpacked(path) && parser.IsUnpacked(path))
                    {
                        switch (parser.Verb)
                        {
                            case WFileParserVerb.Serialize:
                                PromptPlus.WriteLine(recursive
                                    ? $"Deserializing {parser.Name} (recursive): {fileName.PromptPlusEscape()}..."
                                    : $"Deserializing {parser.Name}: {fileName.PromptPlusEscape()}...");
                                break;
                            case WFileParserVerb.Unpack:
                                PromptPlus.WriteLine(recursive
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
                        parsed = true;
                        break;
                    }
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
                        "WitchyBND had no access to perform this action; perhaps try Administrator Mode?", path,
                        WitchyErrorType.NoAccess));
                    error = true;
                }
                catch (FriendlyException e)
                {
                    if (Configuration.IsTest)
                        throw;

                    Program.RegisterError(new WitchyError(e.Message, path));
                    error = true;
                }
                catch (Exception e)
                {
                    if (Configuration.IsTest || Configuration.IsDebug)
                        throw;

                    Program.RegisterException(e, path);
                    error = true;
                }
            }

            switch (parsed)
            {
                case true:
                    Interlocked.Increment(ref Program.ProcessedItems);
                    break;
                case false when !error && !recursive:
                    PromptPlus.Error.WriteLine($"Could not find valid parser for {path}.");
                    break;
            }
        }

        IEnumerable<string> pathsList = paths.ToList();

        foreach (WFileParser parser in Parsers.Where(p => p.HasPreprocess))
        {
            foreach (string path in pathsList)
            {
                parser.Preprocess(path);
            }
        }

        if (Configuration.Parallel)
            Parallel.ForEach(pathsList, Callback);
        else
            pathsList.ToList().ForEach(Callback);
    }

    static ParseMode()
    {
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
            //MSBE
            //TAE
        };
    }
}