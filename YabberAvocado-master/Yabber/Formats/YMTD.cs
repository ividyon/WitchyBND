using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.IO;

namespace Yabber
{
    static class YMTD
    {
        public static void Unpack(this MTD mtd, string sourceFile)
        {
            string targetFile = $"{sourceFile}.xml";

            if (File.Exists(targetFile)) YBUtil.Backup(targetFile);

            YBUtil.XmlSerialize<MTD>(mtd, targetFile);
        }

        public static void Repack(string sourceFile)
        {
            string outPath;
            if (sourceFile.EndsWith(".mtd.xml"))
                outPath = sourceFile.Replace(".mtd.xml", ".mtd");
            else if (sourceFile.EndsWith(".mtd.dcx.xml"))
                outPath = sourceFile.Replace(".mtd.dcx.xml", ".mtd.dcx");
            else
                throw new InvalidOperationException("Invalid MTD xml filename.");

            if (File.Exists(outPath)) YBUtil.Backup(outPath);

            YBUtil.XmlDeserialize<MTD>(sourceFile).Write(outPath);
        }
    }
}
