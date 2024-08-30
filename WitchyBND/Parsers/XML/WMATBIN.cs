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

    public override bool? IsSimple(string path)
    {
        string filename = Path.GetFileName(path).ToLower();
        return filename.EndsWith(".matbin") || filename.EndsWith(".matbin.dcx");
    }

    public override bool IsUnpacked(string path)
    {
        if (Path.GetExtension(path) != ".xml")
            return false;

        var doc = XDocument.Load(path);
        return doc.Root != null && doc.Root.Name == Name;
    }

    public override void Unpack(string srcPath, ISoulsFile? file, bool recursive)
    {
        var matbin = (file as MATBIN)!;
        string targetFile = GetUnpackDestPath(srcPath, recursive);

        if (File.Exists(targetFile)) Backup(targetFile);

        WBUtil.XmlSerialize<MATBIN>(matbin, targetFile);
        AddLocationToXml(targetFile, srcPath, recursive);
    }

    public override void Repack(string srcPath, bool recursive)
    {
        XElement xml = LoadXml(srcPath);
        string outPath = GetRepackDestPath(srcPath, xml);

        if (File.Exists(outPath)) Backup(outPath);

        WBUtil.XmlDeserialize<MATBIN>(srcPath).TryWriteSoulsFile(outPath);
    }
}