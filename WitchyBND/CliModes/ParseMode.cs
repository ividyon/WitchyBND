﻿using System;
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

        bool error = false;
        int errorcode = 0;

        paths = WBUtil.ProcessPathGlobs(paths);

        try
        {
            ParseFiles(paths);
        }
        catch (DllNotFoundException ex) when (ex.Message.Contains("oo2core_6_win64.dll"))
        {
            PromptPlus.Error.WriteLine(
                "ERROR: oo2core_6_win64.dll not found. Please copy this library from the game directory to WitchyBND's directory.");
            errorcode = 3;
            error = true;
        }
        catch (UnauthorizedAccessException e)
        {
            Console.Error.WriteLine("WitchyBND had no access to perform this action; perhaps try Administrator Mode?\n");
            throw;
        }
        catch (FriendlyException ex)
        {
            Console.WriteLine();
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            errorcode = 4;
            error = true;
        }

        if (!error) return;

        Console.WriteLine("One or more errors were encountered and displayed above.\nPress any key to exit.");
        Console.ReadKey();
        Environment.Exit(errorcode);
    }

    private static void ParseFiles(List<string> paths)
    {
        foreach (string path in paths)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                Program.WriteError($"Path {path} does not exist.");
                continue;
            }

            string fileName = Path.GetFileName(path);
            var parsed = false;

            foreach (WFileParser parser in Parsers)
            {
                if (parser.Exists(path) && parser.Is(path))
                {
                    switch (parser.Verb)
                    {
                        case WFileParserVerb.Serialize:
                            PromptPlus.WriteLine($"Serializing {parser.Name}: {fileName}...");
                            break;
                        case WFileParserVerb.Unpack:
                        default:
                            PromptPlus.WriteLine($"Unpacking {parser.Name}: {fileName}...");
                            break;
                    }
                    parser.Unpack(path);
                    parsed = true;
                    break;
                }
                if (parser.ExistsUnpacked(path) && parser.IsUnpacked(path))
                {
                    switch (parser.Verb)
                    {
                        case WFileParserVerb.Serialize:
                            PromptPlus.WriteLine($"Deserializing {parser.Name}: {fileName}...");
                            break;
                        case WFileParserVerb.Unpack:
                        default:
                            PromptPlus.WriteLine($"Repacking {parser.Name}: {fileName}...");
                            break;
                    }
                    parser.Repack(path);
                    parsed = true;
                    break;
                }
            }

            if (!parsed)
            {
                Program.WriteError($"Could not find valid parser for \"{path}\"");
            }
        }
    }

    static ParseMode()
    {
        Parsers = new List<WFileParser>
        {
            new Parsers.WDCX(),
            //Regulation file
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
            //PARAM
            //MSBE
            //TAE
        };
    }
}