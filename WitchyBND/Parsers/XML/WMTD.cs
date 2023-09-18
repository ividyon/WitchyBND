using System;
using System.IO;
using System.Xml.Linq;
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

    public override bool IsUnpacked(string path)
    {
        if (Path.GetExtension(path) != ".xml")
            return false;

        var doc = XDocument.Load(path);
        return doc.Root != null && doc.Root.Name == Name;
    }

    public override void Unpack(string srcPath)
    {
        MTD mtd = MTD.Read(srcPath);
        string targetFile = GetUnpackDestPath(srcPath);

        if (File.Exists(targetFile)) WBUtil.Backup(targetFile);

        WBUtil.XmlSerialize<MTD>(mtd, targetFile);
        AddLocationToXml(targetFile);
    }

    public override void Repack(string srcPath)
    {
        XElement xml = LoadXml(srcPath);
        string outPath = GetRepackDestPath(srcPath, xml);

        if (File.Exists(outPath)) WBUtil.Backup(outPath);

        WBUtil.XmlDeserialize<MTD>(srcPath).TryWriteSoulsFile(outPath);
    }
}