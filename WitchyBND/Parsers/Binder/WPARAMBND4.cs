﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using PPlus;
using SoulsFormats;
using WitchyBND.CliModes;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WPARAMBND4 : WBinderParser
{

    public override string Name => "PARAM BND4";
    public override bool IncludeInList => false;

    private static bool FilenameIsDS2Regulation(string path)
    {
        // Currently there is no DS2 Paramdex, only DS2S.
        return false;
        // var filename = Path.GetFileName(path);
        // return filename.Contains("enc_regulation") && (filename.EndsWith(".bnd.dcx") || filename.EndsWith(".bnd"));
    }
    private static bool FilenameIsDS2SRegulation(string path)
    {
        var filename = Path.GetFileName(path);
        return filename.Contains("enc_regulation") && (filename.EndsWith(".bnd.dcx") || filename.EndsWith(".bnd"));
    }

    private static bool FilenameIsDS3Regulation(string path)
    {
        var filename = Path.GetFileName(path);
        return filename.StartsWith("Data0") && filename.EndsWith(".bdt");
    }

    private static bool FilenameIsModernRegulation(string path)
    {
        string filename = Path.GetFileName(path);
        return filename.Contains("regulation") && filename.EndsWith(".bin");
    }

    public static bool FilenameIsPARAMBND4(string path)
    {
        return FilenameIsDS2Regulation(path) || FilenameIsDS2SRegulation(path) || FilenameIsDS3Regulation(path) ||
               FilenameIsModernRegulation(path);
    }

    private BND4? GetRegulationWithGameType(string path, out WBUtil.GameType? game)
    {
        if (FilenameIsDS2Regulation(path))
        {
            game = WBUtil.GameType.DS2;
            return WBUtil.DecryptDS2Regulation(path);
        }

        if (FilenameIsDS2SRegulation(path))
        {
            game = WBUtil.GameType.DS2S;
            return WBUtil.DecryptDS2Regulation(path);
        }

        if (FilenameIsDS3Regulation(path))
        {
            game = WBUtil.GameType.DS3;
            return SFUtil.DecryptDS3Regulation(path);
        }

        if (FilenameIsModernRegulation(path))
        {
            var binder = WBUtil.DecryptRegulationBin(path, out WBUtil.GameType myGame);
            game = myGame;
            return binder;
        }

        game = null;
        return null;
    }

    public override bool HasPreprocess => true;

    public List<string> PreprocessedPaths = new();
    public override bool Preprocess(string srcPath)
    {
        if (!Directory.Exists(srcPath)) return false;

        string xmlPath = GetBinderXmlPath(srcPath, "bnd4");
        if (!File.Exists(xmlPath)) return false;

        if (PreprocessedPaths.Contains(srcPath)) return false;

        var doc = XDocument.Load(GetBinderXmlPath(srcPath, "bnd4"));
        if (doc.Root == null || doc.Root.Name != "bnd4") return false;
        XElement xml = doc.Root;

        var gameElement = xml.Element("game");
        if (gameElement == null) return false;
        Enum.TryParse(gameElement.Value, out WBUtil.GameType game);

        var versionElement = xml.Element("version");
        if (versionElement == null) return false;
        var regVer = Convert.ToUInt64(versionElement.Value);

        if (!WPARAM.Games.ContainsKey(srcPath))
            WPARAM.Games[srcPath] = (game, regVer);

        WPARAM.PopulateParamdex(game);

        PreprocessedPaths.Add(srcPath);
        return true;
    }

    public override bool Is(string path, byte[]? _, out ISoulsFile? file)
    {
        file = null;
        return FilenameIsPARAMBND4(path);
    }

    public override bool IsUnpacked(string path)
    {
        if (!Directory.Exists(path)) return false;

        string xmlPath = Path.Combine(path, GetBinderXmlFilename("bnd4"));
        if (!File.Exists(xmlPath)) return false;

        var doc = XDocument.Load(xmlPath);
        return doc.Root != null && doc.Root.Name.ToString().ToLower() == "bnd4" && doc.Root.Element("game") != null;
    }

    public override void Unpack(string srcPath, ISoulsFile? _)
    {
        var bnd = GetRegulationWithGameType(srcPath, out WBUtil.GameType? game);
        switch (game)
        {
            case WBUtil.GameType.DS2:
            case WBUtil.GameType.DS2S:
            case WBUtil.GameType.DS3:
            case WBUtil.GameType.ER:
            case WBUtil.GameType.SDT:
            case WBUtil.GameType.AC6:
                ParseMode.Parsers.OfType<WBND4>().First().Unpack(srcPath, bnd, game);
                break;
            default:
                throw new InvalidDataException("Could not identify game type of regulation file.");
        }
    }

    public override void Repack(string srcPath)
    {
        if (!WPARAM.WarnAboutParams()) return;

        var bndParser = ParseMode.Parsers.OfType<WBND4>().First();
        var xmlPath = GetBinderXmlPath(srcPath, "bnd4");
        var doc = XDocument.Load(xmlPath);
        if (doc.Root == null) throw new XmlException("XML has no root");
        XElement xml = doc.Root;

        var gameElement = xml.Element("game");
        if (gameElement == null) throw new XmlException("XML has no Game element");
        Enum.TryParse(gameElement.Value, out WBUtil.GameType game);

        var versionElement = xml.Element("version");
        if (versionElement == null) throw new XmlException("XML has no Version element");
        var regVer = Convert.ToUInt64(versionElement.Value);

        ulong? latestVer = WBUtil.GetLatestKnownRegulationVersion(game);
        if (latestVer < regVer)
        {
            throw new RegulationOutOfBoundsException(@"Regulation version exceeds latest known Paramdex regulation version.");
        }

        var destPath = bndParser.GetRepackDestPath(srcPath, xml);

        // Sanity check PARAMs
        XElement? filesElement = xml.Element("files");
        if (filesElement == null) throw new XmlException("XML has no Files element");
        var files = filesElement.Elements("file").Where(f => f.Element("path") != null && f.Element("path")!.Value.EndsWith(".param")).ToList();
        if (files.Any())
        {
            var paramParser = ParseMode.Parsers.OfType<WPARAM>().First();

            var filePaths = files.Select(file => {
                var path = file.Element("path");
                if (path == null) throw new XmlException($"File element {files.ToList().IndexOf(file)} has no path.");
                return path.Value;
            });

            foreach (string filePath in filePaths)
            {
                try
                {
                    paramParser.Unpack(Path.Combine(srcPath, filePath), null, true);
                }
                catch (Exception e)
                {
                    throw new MalformedBinderException($"The regulation binder is malformed: {Path.GetFileNameWithoutExtension(filePath)} has thrown an exception during read.", e);
                }
            }
        }

        switch (game)
        {
            case WBUtil.GameType.DS2:
            case WBUtil.GameType.DS2S:
                if (!Configuration.Args.Passive)
                {
                    PromptPlus.WriteLine(
                        "DS2 files cannot be re-encrypted, yet, so re-packing this folder might ruin your encrypted bnd.");
                    var confirm = PromptPlus.Confirm("Proceed to repack BND (without encryption)?")
                        .Run();
                    if (confirm.IsAborted || confirm.Value.IsNoResponseKey())
                    {
                        return;
                    }
                }

                bndParser.Repack(srcPath);
                break;
            case WBUtil.GameType.DS3:
                bndParser.Repack(srcPath);
                BND4 ds3Bnd = BND4.Read(destPath);
                SFUtil.EncryptDS3Regulation(destPath, ds3Bnd);
                break;
            case WBUtil.GameType.ER:
            case WBUtil.GameType.SDT:
            case WBUtil.GameType.AC6:
                bndParser.Repack(srcPath);
                BND4 regBnd = BND4.Read(destPath);
                WBUtil.EncryptRegulationBin(destPath, game, regBnd);
                break;
            default:
                throw new InvalidDataException("Could not identify game type of regulation file.");
        }
    }
}