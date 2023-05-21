using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.IO;

namespace Yabber
{
    static class YMATBIN
    {
        public static void Unpack(this MATBIN matbin, string sourceFile)
        {
            string targetFile = $"{sourceFile}.xml";

            if (File.Exists(targetFile)) YBUtil.Backup(targetFile);

            YBUtil.XmlSerialize<MATBIN>(matbin, targetFile);
        }

        public static void Repack(string sourceFile)
        {
            string outPath;
            if (sourceFile.EndsWith(".matbin.xml"))
                outPath = sourceFile.Replace(".matbin.xml", ".matbin");
            else if (sourceFile.EndsWith(".matbin.dcx.xml"))
                outPath = sourceFile.Replace(".matbin.dcx.xml", ".matbin.dcx");
            else
                throw new InvalidOperationException("Invalid MATBIN xml filename.");

            if (File.Exists(outPath)) YBUtil.Backup(outPath);

            YBUtil.XmlDeserialize<MATBIN>(sourceFile).Write(outPath);
        }
    }
}
