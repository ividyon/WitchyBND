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

public class WBND3Regulation : WBinderParser
{

    public override string Name => "Regulation BND3";

    private bool IsPTDERegulation(string path, BND3 bnd = null)
    {
        bnd ??= BND3.Read(path);
        return bnd.Files.FirstOrDefault(f => f.Name.Contains("default_AIStandardInfoBank.param"), null) != null;
    }

    private bool IsDSRRegulation(string path, BND3 bnd = null)
    {
        bnd ??= BND3.Read(path);
        return IsPTDERegulation(path, bnd) && bnd.Files.FirstOrDefault(f => f.Name.EndsWith("LevelSyncParam.param"), null) != null;
    }

    public override bool Is(string path)
    {
        return BND3.Is(path) && (IsDSRRegulation(path) || IsPTDERegulation(path));
    }

    public override bool IsUnpacked(string path)
    {
        if (!Directory.Exists(path)) return false;

        string xmlPath = Path.Combine(path, GetBinderXmlFilename("bnd3"));
        if (!File.Exists(xmlPath)) return false;

        var doc = XDocument.Load(xmlPath);
        return doc.Root != null && doc.Root.Name == "bnd3" && doc.Root.Element("game") != null;
    }

    public override void Unpack(string srcPath)
    {
        var bnd = BND3.Read(srcPath);
        WBUtil.GameType game = WBUtil.GameType.DS1;
        if (IsDSRRegulation(srcPath, bnd))
            game = WBUtil.GameType.DS1R;

        ParseMode.Parsers.OfType<WBND3>().First().Unpack(srcPath, bnd, game);
    }

    public override void Repack(string srcPath)
    {
        ParseMode.Parsers.OfType<WBND3>().First().Repack(srcPath);
    }
}