using System;
using System.IO;
using WitchyFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WMTD : WXMLParser
{

    public override string Name => "MTD";
    public override bool Is(string path)
    {
        return MTD.Is(path);
    }

    public override void Unpack(string srcPath)
    {
        MTD mtd = MTD.Read(srcPath);
        string targetFile = GetUnpackDestPath(srcPath);

        if (File.Exists(targetFile)) WBUtil.Backup(targetFile);

        WBUtil.XmlSerialize<MTD>(mtd, targetFile);
    }

    public override void Repack(string srcPath)
    {
        string outPath;
        if (srcPath.EndsWith(".mtd.xml"))
            outPath = srcPath.Replace(".mtd.xml", ".mtd");
        else if (srcPath.EndsWith(".mtd.dcx.xml"))
            outPath = srcPath.Replace(".mtd.dcx.xml", ".mtd.dcx");
        else
            throw new InvalidOperationException("Invalid MTD xml filename.");

        if (File.Exists(outPath)) WBUtil.Backup(outPath);

        WBUtil.XmlDeserialize<MTD>(srcPath).TryWriteSoulsFile(outPath);
    }
}