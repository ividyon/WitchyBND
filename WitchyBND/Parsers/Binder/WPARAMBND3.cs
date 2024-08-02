using System.IO;
using System.Linq;
using System.Xml.Linq;
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
        return bnd.Files.FirstOrDefault(f => f.Name.ToLower().Contains("default_aistandardinfobank.param"), null) != null;
    }

    private static bool IsDSRParamBND(BND3 bnd)
    {
        return IsPTDEParamBND(bnd) && bnd.Files.FirstOrDefault(f => f.Name.ToLower().EndsWith("levelsyncparam.param"), null) != null;
    }

    private static bool IsAC4Regulation(BND3 bnd)
    {
        return bnd.Files.FirstOrDefault(f => f.Name.ToLower().EndsWith("comment_uk.fmg"), null) != null;
    }

    private static bool IsACFARegulation(BND3 bnd)
    {
        return bnd.Files.FirstOrDefault(f => f.Name.ToLower().EndsWith("comment_es.fmg"), null) != null;
    }

    private static bool IsACFABoot(BND3 bnd)
    {
        return bnd.Files.FirstOrDefault(f => f.Name.ToLower().EndsWith("ac45_allsound.mgs"), null) != null;
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

        string xmlPath = Path.Combine(path, GetFolderXmlFilename("bnd3"));
        if (!File.Exists(xmlPath)) return false;

        var doc = XDocument.Load(xmlPath);
        return doc.Root != null && doc.Root.Name == "bnd3" && doc.Root.Element("game") != null;
    }

    public override void Unpack(string srcPath, ISoulsFile? file, bool recursive)
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

        ParseMode.GetParser<WBND3>().Unpack(srcPath, file, recursive, game);
    }

    public override void Repack(string srcPath, bool recursive)
    {
        ParseMode.GetParser<WBND3>().Repack(srcPath, recursive);
    }
}