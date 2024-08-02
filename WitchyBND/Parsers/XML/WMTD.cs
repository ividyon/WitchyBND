using System.IO;
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

    public override bool? IsSimple(string path)
    {
        string filename = Path.GetFileName(path);
        return filename.EndsWith(".mtd") || filename.EndsWith(".mtd.dcx");
    }

    public override void Unpack(string srcPath, ISoulsFile? file, bool recursive)
    {
        MTD mtd = MTD.Read(srcPath);
        string targetFile = GetUnpackDestPath(srcPath, recursive);

        if (File.Exists(targetFile)) WBUtil.Backup(targetFile);

        WBUtil.XmlSerialize<MTD>(mtd, targetFile);
        AddLocationToXml(targetFile, srcPath, recursive);
    }

    public override void Repack(string srcPath, bool recursive)
    {
        XElement xml = LoadXml(srcPath);
        string outPath = GetRepackDestPath(srcPath, xml);

        if (File.Exists(outPath)) WBUtil.Backup(outPath);

        WBUtil.XmlDeserialize<MTD>(srcPath).TryWriteSoulsFile(outPath);
    }
}