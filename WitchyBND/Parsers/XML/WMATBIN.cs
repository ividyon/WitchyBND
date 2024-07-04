using System.IO;
using System.Xml.Linq;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WMATBIN : WXMLParser
{

    public override string Name => "MATBIN";
    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        return IsRead<MATBIN>(path, data, out file);
    }

    public override bool IsUnpacked(string path)
    {
        if (Path.GetExtension(path) != ".xml")
            return false;

        var doc = XDocument.Load(path);
        return doc.Root != null && doc.Root.Name == Name;
    }

    public override void Unpack(string srcPath, ISoulsFile? file)
    {
        var matbin = (file as MATBIN)!;
        string targetFile = GetUnpackDestPath(srcPath);

        if (File.Exists(targetFile)) WBUtil.Backup(targetFile);

        WBUtil.XmlSerialize<MATBIN>(matbin, targetFile);
        AddLocationToXml(targetFile, srcPath);
    }

    public override void Repack(string srcPath)
    {
        XElement xml = LoadXml(srcPath);
        string outPath = GetRepackDestPath(srcPath, xml);

        if (File.Exists(outPath)) WBUtil.Backup(outPath);

        WBUtil.XmlDeserialize<MATBIN>(srcPath).TryWriteSoulsFile(outPath);
    }
}