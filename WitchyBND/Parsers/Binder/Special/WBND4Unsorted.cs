using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public abstract class WBND4Unsorted : WUnsortedBinderParser
{
    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        file = null;
        return Configuration.Active.Bnd && EndsInExtension(path) && IsRead<BND4>(path, data, out file);
    }

    public override bool? IsSimple(string path)
    {
        return Configuration.Active.Bnd && EndsInExtension(path);
    }

    public override void Unpack(string srcPath, ISoulsFile? file, bool recursive)
    {
        var bnd = (file as BND4)!;
        string destDir = GetUnpackDestPath(srcPath, recursive);
        Directory.CreateDirectory(destDir);

        var root = "";
        if (Binder.HasNames(bnd.Format))
        {
            root = WBUtil.FindCommonRootPath(bnd.Files.Select(bndFile => bndFile.Name));
        }
        WriteBinderFiles(bnd, destDir, root);

        var xml =
            new XElement(XmlTag,
                new XElement("compression", bnd.Compression.ToString()),
                new XElement("version", bnd.Version),
                new XElement("format", bnd.Format.ToString()),
                new XElement("bigendian", bnd.BigEndian.ToString()),
                new XElement("bitbigendian", bnd.BitBigEndian.ToString()),
                new XElement("unicode", bnd.Unicode.ToString()),
                new XElement("extended", $"0x{bnd.Extended:X2}"),
                new XElement("unk04", bnd.Unk04.ToString()),
                new XElement("unk05", bnd.Unk05.ToString())
                );

        AddLocationToXml(srcPath, recursive, xml);

        if (Version > 0) xml.SetAttributeValue(VersionAttributeName, Version.ToString());

        if (!string.IsNullOrEmpty(root))
            xml.LastNode!.AddAfterSelf(new XElement("root", root));

        using var xw = XmlWriter.Create($"{destDir}\\{GetFolderXmlFilename()}", new XmlWriterSettings
        {
            Indent = true
        });
        xml.WriteTo(xw);
        xw.Close();
    }

    public override void Repack(string srcPath, bool recursive)
    {
        var bnd = new BND4();

        XElement xml = LoadXml(GetFolderXmlPath(srcPath));

        string root = xml.Element("root")?.Value ?? "";

        Enum.TryParse(xml.Element("compression")?.Value ?? "None", out DCX.Type compression);
        bnd.Compression = compression;

        bnd.Version = xml.Element("version")!.Value;
        bnd.Format = (Binder.Format)Enum.Parse(typeof(Binder.Format), xml.Element("format")!.Value);
        bnd.BigEndian = bool.Parse(xml.Element("bigendian")!.Value);
        bnd.BitBigEndian = bool.Parse(xml.Element("bitbigendian")!.Value);
        bnd.Unicode = bool.Parse(xml.Element("unicode")!.Value);
        bnd.Extended = Convert.ToByte(xml.Element("extended")!.Value, 16);
        bnd.Unk04 = bool.Parse(xml.Element("unk04")!.Value);
        bnd.Unk05 = bool.Parse(xml.Element("unk05")!.Value);

        ReadUnsortedBinderFiles(bnd, srcPath, root, recursive);

        var destPath = GetRepackDestPath(srcPath, xml);

        Backup(destPath);

        WarnAboutKrak(compression, bnd.Files.Count);

        bnd.Write(destPath);
    }

    public override string GetUnpackDestPath(string srcPath, bool recursive)
    {
        return $"{base.GetUnpackDestPath(srcPath, recursive)}-w{XmlTag.ToLower()}";
    }
}