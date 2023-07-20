using System;
using System.IO;
using WitchyFormats;
using WitchyLib;

namespace WitchyBND;

static class WMTD
{
    public static void Unpack(this MTD mtd, string sourceFile)
    {
        string targetFile = $"{sourceFile}.xml";

        if (File.Exists(targetFile)) WBUtil.Backup(targetFile);

        WBUtil.XmlSerialize<MTD>(mtd, targetFile);
    }

    public static bool Repack(string sourceFile)
    {
        string outPath;
        if (sourceFile.EndsWith(".mtd.xml"))
            outPath = sourceFile.Replace(".mtd.xml", ".mtd");
        else if (sourceFile.EndsWith(".mtd.dcx.xml"))
            outPath = sourceFile.Replace(".mtd.dcx.xml", ".mtd.dcx");
        else
            throw new InvalidOperationException("Invalid MTD xml filename.");

        if (File.Exists(outPath)) WBUtil.Backup(outPath);

        WBUtil.XmlDeserialize<MTD>(sourceFile).Write(outPath);

        return false;
    }
}