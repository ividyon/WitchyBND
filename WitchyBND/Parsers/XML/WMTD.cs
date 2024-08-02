﻿using System.IO;
using System.Xml.Linq;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WMTD : WXMLParser
{
    public override string Name => "MTD";

    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        return IsRead<MTD>(path, data, out file);
    }

    public override void Unpack(string srcPath, ISoulsFile? file, string? recursiveOriginPath)
    {
        MTD mtd = MTD.Read(srcPath);
        string targetFile = GetUnpackDestPath(srcPath, recursiveOriginPath);

        if (File.Exists(targetFile)) WBUtil.Backup(targetFile);

        WBUtil.XmlSerialize<MTD>(mtd, targetFile);
        AddLocationToXml(targetFile, srcPath);
    }

    public override void Repack(string srcPath, string? recursiveOriginPath)
    {
        XElement xml = LoadXml(srcPath);
        string outPath = GetRepackDestPath(srcPath, xml);

        if (File.Exists(outPath)) WBUtil.Backup(outPath);

        WBUtil.XmlDeserialize<MTD>(srcPath).TryWriteSoulsFile(outPath);
    }
}