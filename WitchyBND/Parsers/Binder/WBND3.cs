using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WBND3 : WBinderParser
{
    public override string Name => "BND3";

    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        return IsRead<BND3>(path, data, out file);
    }


    public override void Unpack(string srcPath, ISoulsFile? file)
    {
        Unpack(srcPath, file, null);
    }

    public void Unpack(string srcPath, ISoulsFile? file, WBUtil.GameType? game)
    {
        BND3 bnd = (file as BND3)!;
        string srcName = Path.GetFileName(srcPath);
        string destDir = GetUnpackDestPath(srcPath);
        Directory.CreateDirectory(destDir);

        var root = "";
        if (Binder.HasNames(bnd.Format))
        {
            root = WBUtil.FindCommonRootPath(bnd.Files.Select(bndFile => bndFile.Name));
        }

        XElement files = WriteBinderFiles(bnd, destDir, root);

        var filename = new XElement("filename", srcName);

        var xml =
            new XElement(XmlTag,
                filename,
                new XElement("compression", bnd.Compression.ToString()),
                new XElement("version", bnd.Version),
                new XElement("format", bnd.Format.ToString()),
                new XElement("bigendian", bnd.BigEndian.ToString()),
                new XElement("bitbigendian", bnd.BitBigEndian.ToString()),
                new XElement("unk18", bnd.Unk18.ToString()),
                files);

        if (!string.IsNullOrEmpty(Configuration.Args.Location))
            filename.AddAfterSelf(new XElement("sourcePath", Path.GetFullPath(Path.GetDirectoryName(srcPath))));

        if (game != null)
        {
            filename.AddAfterSelf(new XElement("game", game.ToString()));
        }

        if (!string.IsNullOrEmpty(root))
            files.AddBeforeSelf(new XElement("root", root));

        var xw = XmlWriter.Create($"{destDir}\\{GetFolderXmlFilename()}", new XmlWriterSettings
        {
            Indent = true
        });
        xml.WriteTo(xw);
        xw.Close();
    }

    public override void Repack(string srcPath)
    {
        var bnd = new BND3();

        var doc = XDocument.Load(GetFolderXmlPath(srcPath));
        if (doc.Root == null) throw new XmlException("XML has no root");
        XElement xml = doc.Root;

        string root = xml.Element("root")?.Value ?? "";

        Enum.TryParse(xml.Element("compression")?.Value ?? "None", out DCX.Type compression);
        bnd.Compression = compression;

        bnd.Version = xml.Element("version")!.Value;
        bnd.Format = (Binder.Format)Enum.Parse(typeof(Binder.Format), xml.Element("format")!.Value);
        bnd.BigEndian = bool.Parse(xml.Element("bigendian")!.Value);
        bnd.BitBigEndian = bool.Parse(xml.Element("bitbigendian")!.Value);
        bnd.Unk18 = int.Parse(xml.Element("unk18")!.Value);

        if (xml.Element("files") != null)
            ReadBinderFiles(bnd, xml.Element("files")!, srcPath, root);

        var destPath = GetRepackDestPath(srcPath, xml);

        WBUtil.Backup(destPath);
        bnd.Write(destPath);
    }
}