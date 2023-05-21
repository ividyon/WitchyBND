using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.IO;

namespace Yabber
{
    static class YFXR3
    {
        public static void Unpack(this FXR3 fxr, string sourceFile)
        {
            string targetFile = $"{sourceFile}.xml";

            if (File.Exists(targetFile)) YBUtil.Backup(targetFile);

            YBUtil.XmlSerialize<FXR3>(fxr, targetFile);
        }

        public static void Repack(string sourceFile)
        {
            string outPath;
            if (sourceFile.EndsWith(".fxr.xml"))
                outPath = sourceFile.Replace(".fxr.xml", ".fxr");
            else if (sourceFile.EndsWith(".fxr.dcx.xml"))
                outPath = sourceFile.Replace(".fxr.dcx.xml", ".fxr.dcx");
            else
                throw new InvalidOperationException("Invalid FXR3 xml filename.");

            if (File.Exists(outPath)) YBUtil.Backup(outPath);

            YBUtil.XmlDeserialize<FXR3>(sourceFile).Write(outPath);
        }
    }
}
