﻿using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.IO;
using Newtonsoft.Json;

namespace Yabber
{
    static class YMSBE
    {
        public static void Unpack(this MSBE msb, string sourceFile)
        {
            string targetFile = $"{sourceFile}.json";

            if (File.Exists(targetFile)) YBUtil.Backup(targetFile);

            File.WriteAllText(targetFile, YBUtil.JsonSerialize(msb));
        }

        public static void Repack(string sourceFile)
        {
            string outPath;
            if (sourceFile.EndsWith(".msb.json"))
                outPath = sourceFile.Replace(".msb.json", ".msb");
            else if (sourceFile.EndsWith(".msb.dcx.json"))
                outPath = sourceFile.Replace(".msb.dcx.json", ".msb.dcx");
            else
                throw new InvalidOperationException("Invalid MSBE json filename.");

            if (File.Exists(outPath)) YBUtil.Backup(outPath);

            YBUtil.JsonDeserialize<MSBE>(File.ReadAllText(sourceFile)).Write(outPath);
        }
    }
}