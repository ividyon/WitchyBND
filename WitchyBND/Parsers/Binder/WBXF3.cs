using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WBXF3 : WBinderParser
{
    public override string Name => "BXF3";

    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        file = null;
        return BXF3.IsHeader(path) || BXF3.IsData(path);
    }

    public override bool? IsSimple(string path)
    {
        return null;
    }

    public override void Unpack(string srcPath, ISoulsFile? _, bool recursive)
    {
        string bdtPath;
        string bdtName;
        string bhdPath;
        string bhdName;
        string srcDirPath = Path.GetDirectoryName(srcPath);
        string nameWithoutExt = Path.GetFileNameWithoutExtension(srcPath);
        string destDir = GetUnpackDestPath(srcPath, recursive);

        if (BXF3.IsHeader(srcPath))
        {
            bhdPath = srcPath;
            bhdName = Path.GetFileName(srcPath);

            string bdtExtension = Path.GetExtension(srcPath).Replace("bhd", "bdt");
            bdtName = $"{nameWithoutExt}{bdtExtension}";
            bdtPath = Path.Combine(srcDirPath, bdtName);
        }
        else
        {
            bdtPath = srcPath;
            bdtName = Path.GetFileName(srcPath);

            string bhdExtension = Path.GetExtension(srcPath).Replace("bdt", "bhd");
            bhdName = $"{nameWithoutExt}{bhdExtension}";
            bhdPath = Path.Combine(srcDirPath, bhdName);
        }

        var bxf = BXF3.Read(bhdPath, bdtPath);
        Directory.CreateDirectory(destDir);

        var root = "";
        if (Binder.HasNames(bxf.Format))
        {
            root = WBUtil.FindCommonBndRootPath(bxf.Files.Select(bxfFile => bxfFile.Name));
        }

        XElement files = WriteBinderFiles(bxf, destDir, root);

        var bdtFilename = new XElement("bdt_filename", bdtName);

        var xml = PrepareXmlManifest(srcPath, recursive, true, null, out XDocument xDoc, root);
        xml.Add(
                new XElement("bhd_filename", bhdName),
                bdtFilename,
                new XElement("version", bxf.Version),
                new XElement("format", bxf.Format.ToString()),
                new XElement("bigendian", bxf.BigEndian.ToString()),
                new XElement("bitbigendian", bxf.BitBigEndian.ToString()),
                files);

        WriteXmlManifest(xDoc, srcPath, recursive);
    }

    public override void Repack(string srcPath, bool recursive)
    {
        var bxf = new BXF3();

        XElement xml = LoadXml(GetFolderXmlPath(srcPath));

        string root = xml.Element("root")?.Value ?? "";

        bxf.Version = xml.Element("version")!.Value;
        bxf.Format = (Binder.Format)Enum.Parse(typeof(Binder.Format), xml.Element("format")!.Value);
        bxf.BigEndian = bool.Parse(xml.Element("bigendian")!.Value);
        bxf.BitBigEndian = bool.Parse(xml.Element("bitbigendian")!.Value);

        if (xml.Element("files") != null)
            ReadBinderFiles(bxf, xml.Element("files")!, srcPath, root, recursive);

        var bhdDestPath = GetRepackDestPath(srcPath, xml, "bhd_filename");
        var bdtDestPath = GetRepackDestPath(srcPath, xml, "bdt_filename");
        Backup(bhdDestPath);
        Backup(bdtDestPath);
        bxf.Write(bhdDestPath, bdtDestPath);
    }
}