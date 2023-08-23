using System;
using System.IO;
using WitchyLib;
using WitchyFormats;

namespace WitchyBND
{
    static class WMATBIN
    {
        public static bool Unpack(this MATBIN matbin, string sourceFile)
        {
            string targetFile = $"{sourceFile}.xml";

            if (File.Exists(targetFile)) WBUtil.Backup(targetFile);

            WBUtil.XmlSerialize<MATBIN>(matbin, targetFile);

            return false;
        }

        public static bool Repack(string sourceFile)
        {
            string outPath;
            if (sourceFile.EndsWith(".matbin.xml"))
                outPath = sourceFile.Replace(".matbin.xml", ".matbin");
            else if (sourceFile.EndsWith(".matbin.dcx.xml"))
                outPath = sourceFile.Replace(".matbin.dcx.xml", ".matbin.dcx");
            else
                throw new InvalidOperationException("Invalid MATBIN xml filename.");

            if (File.Exists(outPath)) WBUtil.Backup(outPath);

            WBUtil.XmlDeserialize<MATBIN>(sourceFile).Write(outPath);

            return false;
        }
    }
}