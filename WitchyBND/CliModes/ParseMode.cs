using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using PPlus;
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

    public static void ParseFiles(List<string> paths, bool recursive = false)
    {
        foreach (string path in paths)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                if (!recursive)
                    Program.RegisterNotice($"Path {path} does not exist.");
                continue;
            }

            string fileName = Path.GetFileName(path);
            var parsed = false;
            var error = false;

            foreach (WFileParser parser in Parsers)
            {
                try
                {
                    if ((Configuration.Args.UnpackOnly || !Configuration.Args.RepackOnly) && parser.Exists(path) &&
                        parser.Is(path))
                    {
                        switch (parser.Verb)
                        {
                            case WFileParserVerb.Serialize:
                                PromptPlus.WriteLine(recursive
                                    ? $"Serializing {parser.Name} (recursive): {fileName.PromptPlusEscape()}..."
                                    : $"Serializing {parser.Name}: {fileName.PromptPlusEscape()}...");
                                break;
                            case WFileParserVerb.Unpack:
                            default:
                                PromptPlus.WriteLine(recursive
                                    ? $"Unpacking {parser.Name} (recursive): {fileName.PromptPlusEscape()}..."
                                    : $"Unpacking {parser.Name}: {fileName.PromptPlusEscape()}...");
                                break;
                        }

                        parser.Unpack(path);
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
                            default:
                                PromptPlus.WriteLine(recursive
                                    ? $"Repacking {parser.Name} (recursive): {fileName.PromptPlusEscape()}..."
                                    : $"Repacking {parser.Name}: {fileName.PromptPlusEscape()}...");
                                break;
                        }

                        parser.Repack(path);
                        parsed = true;
                        break;
                    }
                }
                catch (DllNotFoundException e) when (e.Message.Contains("oo2core_6_win64.dll") ||
                                                     e.Message.Contains("oo2core_8_win64.dll"))
                {
                    Program.RegisterError(new WitchyError(
                        "ERROR: Oodle DLL not found. Please copy oo2core_6_win64.dll or oo2core_8_win64.dll from the game directory to WitchyBND's directory.",
                        WitchyErrorType.NoOodle));
                    error = true;
                }
                catch (UnauthorizedAccessException)
                {
                    Program.RegisterError(new WitchyError(
                        "WitchyBND had no access to perform this action; perhaps try Administrator Mode?", path,
                        WitchyErrorType.NoAccess));
                    error = true;
                }
                catch (FriendlyException e)
                {
                    Program.RegisterError(new WitchyError(e.Message, path));
                    error = true;
                }
                catch (Exception e)
                {
                    Program.RegisterException(e, path);
                    error = true;
                }
            }

            switch (parsed)
            {
                case true:
                    Program.ProcessedItems++;
                    break;
                case false when !error && !recursive:
                    Program.RegisterNotice(new WitchyNotice("Could not find valid parser.", path));
                    break;
            }
        }
    }

    static ParseMode()
    {
        Parsers = new List<WFileParser>
        {
            new Parsers.WDCX(),
            new Parsers.WPARAMBND3(),
            new Parsers.WPARAMBND4(),
            new Parsers.WFFXBND(),
            new Parsers.WBND3(),
            new Parsers.WBND4(),
            new Parsers.WBXF3(),
            new Parsers.WBXF4(),
            new Parsers.WFFXDLSE(),
            new Parsers.WFMG(),
            new Parsers.WGPARAM(),
            new Parsers.WLUAGNL(),
            new Parsers.WLUAINFO(),
            new Parsers.WTPF(),
            new Parsers.WZERO3(),
            new Parsers.WFXR3(),
            new Parsers.WMATBIN(),
            new Parsers.WMTD(),
            new Parsers.WPARAM(),
            new Parsers.WMQB(),
            //MSBE
            //TAE
        };
    }
}