using System;
using System.IO;
using System.Xml.Linq;
using WitchyFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WMATBIN : WXMLParser
{

    public override string Name => "MATBIN";
    public override bool Is(string path)
    {
        return MATBIN.Is(path);
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
        MATBIN matbin = MATBIN.Read(srcPath);
        string targetFile = GetUnpackDestPath(srcPath);

        if (File.Exists(targetFile)) WBUtil.Backup(targetFile);

        WBUtil.XmlSerialize<MATBIN>(matbin, targetFile);
    }

    public override void Repack(string srcPath)
    {
        string outPath;
        if (srcPath.EndsWith(".matbin.xml"))
            outPath = srcPath.Replace(".matbin.xml", ".matbin");
        else if (srcPath.EndsWith(".matbin.dcx.xml"))
            outPath = srcPath.Replace(".matbin.dcx.xml", ".matbin.dcx");
        else
            throw new InvalidOperationException("Invalid MATBIN xml filename.");

        if (File.Exists(outPath)) WBUtil.Backup(outPath);

        WBUtil.XmlDeserialize<MATBIN>(srcPath).TryWriteSoulsFile(outPath);
    }
}