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

public class WPARAMBND3 : WBinderParser
{

    public override string Name => "PARAM BND3";

    public override bool IncludeInList => false;

    private static bool IsPTDEParamBND(BND3 bnd)
    {
        return bnd.Files.FirstOrDefault(f => f.Name.Contains("default_AIStandardInfoBank.param"), null) != null;
    }

    private static bool IsDSRParamBND(BND3 bnd)
    {
        return IsPTDEParamBND(bnd) && bnd.Files.FirstOrDefault(f => f.Name.EndsWith("LevelSyncParam.param"), null) != null;
    }

    private static bool IsAC4Regulation(BND3 bnd)
    {
        return bnd.Files.FirstOrDefault(f => f.Name.EndsWith("comment_uk.fmg"), null) != null;
    }

    private static bool IsACFARegulation(BND3 bnd)
    {
        return bnd.Files.FirstOrDefault(f => f.Name.EndsWith("comment_es.fmg"), null) != null;
    }

    private static bool IsACFABoot(BND3 bnd)
    {
        return bnd.Files.FirstOrDefault(f => f.Name.EndsWith("AC45_Allsound.mgs"), null) != null;
    }

    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        if (!IsRead<BND3>(path, data, out file))
        {
            return false;
        }
        BND3 bnd = (file as BND3)!;
        return IsDSRParamBND(bnd) || IsPTDEParamBND(bnd) || IsAC4Regulation(bnd) || IsACFARegulation(bnd) || IsACFABoot(bnd);
    }

    public override bool IsUnpacked(string path)
    {
        if (!Directory.Exists(path)) return false;

        string xmlPath = Path.Combine(path, GetBinderXmlFilename("bnd3"));
        if (!File.Exists(xmlPath)) return false;

        var doc = XDocument.Load(xmlPath);
        return doc.Root != null && doc.Root.Name == "bnd3" && doc.Root.Element("game") != null;
    }

    public override void Unpack(string srcPath, ISoulsFile? file)
    {
        BND3 bnd = (file as BND3)!;
        WBUtil.GameType game;
        if (IsPTDEParamBND(bnd))
            game = WBUtil.GameType.DS1;
        else if (IsDSRParamBND(bnd))
            game = WBUtil.GameType.DS1R;
        else if (IsAC4Regulation(bnd))
            game = WBUtil.GameType.AC4;
        else if (IsACFARegulation(bnd) || IsACFABoot(bnd))
            game = WBUtil.GameType.ACFA;
        else
            throw new InvalidDataException($"Could not determine param type for {Path.GetFileName(srcPath)}.");

        ParseMode.Parsers.OfType<WBND3>().First().Unpack(srcPath, file, game);
    }

    public override void Repack(string srcPath)
    {
        ParseMode.Parsers.OfType<WBND3>().First().Repack(srcPath);
    }
}