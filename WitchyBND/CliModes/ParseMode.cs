﻿using System;
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
        void Callback(string path)
        {
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
                lock (Program.ConsoleWriterLock)
                    PromptPlus.WriteLine($"Decompressing DCX: {fileName.PromptPlusEscape()}...");

                data = DCX.Decompress(path, out DCX.Type compressionVal);
                compression = compressionVal;
            }

            foreach (WFileParser parser in Parsers)
            {
                bool toBreak = Catcher.Catch(() => {
                    ISoulsFile? file;
                    if ((Configuration.Args.UnpackOnly || !Configuration.Args.RepackOnly) && parser.Exists(path) &&
                        parser.Is(path, data, out file))
                    {
                        return Unpack(path, file, compression, parser, recursive, out parsed);
                    }

                    if ((Configuration.Args.RepackOnly || !Configuration.Args.UnpackOnly) &&
                        parser.ExistsUnpacked(path) && parser.IsUnpacked(path))
                    {
                        return Repack(path, parser, recursive, out parsed);
                    }

                    return false;
                }, out error, path);

                if (toBreak)
                    break;
            }

            PrintParseSuccess(path, parsed, error, recursive);
        }

        IEnumerable<string> pathsList = paths.ToList();

        foreach (WFileParser parser in Parsers.Where(p => p.HasPreprocess))
        {
            foreach (string path in pathsList)
            {
                bool toBreak = Catcher.Catch(() => parser.Preprocess(path), out _, path);
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
                    lock (Program.ConsoleWriterLock)
                    {
                        if (recursive)
                            PromptPlus.WriteLine($"Successfully parsed {fileName.PromptPlusEscape()} (recursive).");
                        else
                            PromptPlus.WriteLine($"Successfully parsed {fileName.PromptPlusEscape()}.");
                    }
                }

                Interlocked.Increment(ref Program.ProcessedItems);
                break;
            case false when !error && !recursive:
                lock (Program.ConsoleWriterLock)
                    PromptPlus.Error.WriteLine($"Could not find valid parser for {path.PromptPlusEscape()}.");
                break;
        }
    }

    public static bool Unpack(string path, ISoulsFile? file, DCX.Type compression, WFileParser? parser, bool recursive,
        out bool parsed)
    {
        string fileName = Path.GetFileName(path);

        if (compression > file?.Compression)
            file.Compression = compression;
        lock (Program.ConsoleWriterLock)
        {
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
        }

        parser.Unpack(path, file);
        parsed = true;
        return true;
    }

    public static bool Repack(string path, WFileParser parser, bool recursive, out bool parsed)
    {
        string fileName = Path.GetFileName(path);
        lock (Program.ConsoleWriterLock)
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
        }

        if (!parser.UnpackedFitsVersion(path))
        {
            throw new FriendlyException(
                @"Parser version of unpacked file is outdated. Please repack this file using the WitchyBND version it was originally unpacked with, then unpack it using the newest version.");
        }

        parser.Repack(path);
        parsed = true;
        return true;
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
            new WENFL(),
            //MSBE
            //TAE
        };
    }
}