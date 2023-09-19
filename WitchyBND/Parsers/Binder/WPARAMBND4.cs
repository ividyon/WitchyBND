using System;
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

    private bool IsDS2Regulation(string path)
    {
        // Currently there is no DS2 Paramdex, only DS2S.
        return false;
        // var filename = Path.GetFileName(path);
        // return filename.Contains("enc_regulation") && (filename.EndsWith(".bnd.dcx") || filename.EndsWith(".bnd"));
    }
    private bool IsDS2SRegulation(string path)
    {
        var filename = Path.GetFileName(path);
        return filename.Contains("enc_regulation") && (filename.EndsWith(".bnd.dcx") || filename.EndsWith(".bnd"));
    }

    private bool IsDS3Regulation(string path)
    {
        var filename = Path.GetFileName(path);
        return filename.StartsWith("Data0") && filename.EndsWith(".bdt");
    }

    private bool IsModernRegulation(string path)
    {
        string filename = Path.GetFileName(path);
        return filename.Contains("regulation") && filename.EndsWith(".bin");
    }

    private BND4 GetRegulationWithGameType(string path, out WBUtil.GameType? game)
    {
        if (IsDS2Regulation(path))
        {
            game = WBUtil.GameType.DS2;
            return WBUtil.DecryptDS2Regulation(path);
        }

        if (IsDS2SRegulation(path))
        {
            game = WBUtil.GameType.DS2S;
            return WBUtil.DecryptDS2Regulation(path);
        }

        if (IsDS3Regulation(path))
        {
            game = WBUtil.GameType.DS3;
            return SFUtil.DecryptDS3Regulation(path);
        }

        if (IsModernRegulation(path))
        {
            var binder = WBUtil.DecryptRegulationBin(path, out WBUtil.GameType myGame);
            game = myGame;
            return binder;
        }

        game = null;
        return null;
    }

    public override bool Is(string path)
    {
        return IsDS2Regulation(path) || IsDS2SRegulation(path) || IsDS3Regulation(path) || IsModernRegulation(path);
    }

    public override bool IsUnpacked(string path)
    {
        if (!Directory.Exists(path)) return false;

        string xmlPath = Path.Combine(path, GetBinderXmlFilename("bnd4"));
        if (!File.Exists(xmlPath)) return false;

        var doc = XDocument.Load(xmlPath);
        return doc.Root != null && doc.Root.Name == "bnd4" && doc.Root.Element("game") != null;
    }

    public override void Unpack(string srcPath)
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
        var bndParser = ParseMode.Parsers.OfType<WBND4>().First();
        var doc = XDocument.Load(GetBinderXmlPath(srcPath, "bnd4"));
        if (doc.Root == null) throw new XmlException("XML has no root");
        XElement xml = doc.Root;

        var gameElement = xml.Element("game");
        if (gameElement == null) throw new XmlException("XML has no Game element");
        Enum.TryParse(xml.Element("game")!.Value, out WBUtil.GameType game);

        var destPath = bndParser.GetRepackDestPath(srcPath, xml);

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