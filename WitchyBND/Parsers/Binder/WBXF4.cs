using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WBXF4 : WBinderParser
{
    public override string Name => "BXF4";

    public override bool Is(string path, byte[]? _, out ISoulsFile? file)
    {
        file = null;
        return BXF4.IsBHD(path) || BXF4.IsBDT(path);
    }

    public override void Unpack(string srcPath, ISoulsFile? _)
    {
        string bdtPath;
        string bdtName;
        string bhdPath;
        string bhdName;
        string srcDirPath = new DirectoryInfo(srcPath).FullName;
        string nameWithoutExt = Path.GetFileNameWithoutExtension(srcPath);
        string destDir = GetUnpackDestDir(srcPath);

        if (BXF4.IsBHD(srcPath))
        {
            bhdPath = srcPath;
            bhdName = Path.GetFileName(srcPath);

            string bdtExtension = Path.GetExtension(srcPath).Replace("bhd", "bdt");
            bdtName = $"{nameWithoutExt}{bdtExtension}";
            bdtPath = $"{srcDirPath}\\{bdtName}";
        }
        else
        {
            bdtPath = srcPath;
            bdtName = Path.GetFileName(srcPath);

            string bhdExtension = Path.GetExtension(srcPath).Replace("bdt", "bhd");
            bhdName = $"{nameWithoutExt}{bhdExtension}";
            bhdPath = $"{srcDirPath}\\{bhdName}";
        }

        var bxf = BXF4.Read(bhdPath, bdtPath);
        Directory.CreateDirectory(destDir);

        var root = "";
        if (Binder.HasNames(bxf.Format))
        {
            root = WBUtil.FindCommonRootPath(bxf.Files.Select(bxfFile => bxfFile.Name));
        }

        XElement files = WriteBinderFiles(bxf, destDir, root);

        XElement bdtFilename = new XElement("bdt_filename", bdtName);

        var xml =
            new XElement(XmlTag,
                new XElement("bhd_filename", bhdName),
                bdtFilename,
                new XElement("version", bxf.Version),
                new XElement("format", bxf.Format.ToString()),
                new XElement("bigendian", bxf.BigEndian.ToString()),
                new XElement("bitbigendian", bxf.BitBigEndian.ToString()),
                new XElement("unicode", bxf.BitBigEndian.ToString()),
                new XElement("extended", $"0x{bxf.Extended:X2}"),
                new XElement("unk04", bxf.Unk04.ToString()),
                new XElement("unk05", bxf.Unk05.ToString()),
                files);

        if (!string.IsNullOrEmpty(Configuration.Args.Location))
            bdtFilename.AddAfterSelf(new XElement("sourcePath", Path.GetFullPath(Path.GetDirectoryName(srcPath))));

        if (!string.IsNullOrEmpty(root))
            files.AddBeforeSelf(new XElement("root", root));

        var xw = XmlWriter.Create($"{destDir}\\{GetBinderXmlFilename()}", new XmlWriterSettings
        {
            Indent = true
        });
        xml.WriteTo(xw);
        xw.Close();
    }

    public override void Repack(string srcPath)
    {
        var bxf = new BXF4();

        XElement xml = LoadXml(GetBinderXmlPath(srcPath));

        string root = xml.Element("root")?.Value ?? "";

        bxf.Version = xml.Element("version")!.Value;
        bxf.Format = (Binder.Format)Enum.Parse(typeof(Binder.Format), xml.Element("format")!.Value);
        bxf.BigEndian = bool.Parse(xml.Element("bigendian")!.Value);
        bxf.BitBigEndian = bool.Parse(xml.Element("bitbigendian")!.Value);
        bxf.Unicode = bool.Parse(xml.Element("unicode")!.Value);
        bxf.Extended = Convert.ToByte(xml.Element("extended")!.Value, 16);
        bxf.Unk04 = bool.Parse(xml.Element("unk04")!.Value);
        bxf.Unk05 = bool.Parse(xml.Element("unk05")!.Value);

        if (xml.Element("files") != null)
            ReadBinderFiles(bxf, xml.Element("files")!, srcPath, root);

        var bhdDestPath = GetRepackDestPath(srcPath, xml, "bhd_filename");
        var bdtDestPath = GetRepackDestPath(srcPath, xml, "bdt_filename");
        WBUtil.Backup(bhdDestPath);
        WBUtil.Backup(bdtDestPath);
        bxf.Write(bhdDestPath, bdtDestPath);
    }
}