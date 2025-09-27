using System;
using System.IO;
using System.Linq;
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

    public override bool? IsSimple(string path)
    {
        return null;
    }


    public override void Unpack(string srcPath, ISoulsFile? file, bool recursive)
    {
        Unpack(srcPath, file, recursive , null);
    }

    public void Unpack(string srcPath, ISoulsFile? file, bool recursive, WBUtil.GameType? game)
    {
        BND3 bnd = (file as BND3)!;
        string destDir = GetUnpackDestPath(srcPath, recursive);
        Directory.CreateDirectory(destDir);

        var root = "";
        if (Binder.HasNames(bnd.Format))
        {
            root = WBUtil.FindCommonBndRootPath(bnd.Files.Select(bndFile => bndFile.Name));
        }

        XElement files = WriteBinderFiles(bnd, destDir, root);

       var xml = PrepareXmlManifest(srcPath, recursive, false, bnd.Compression, out XDocument xDoc, root); 
        xml.Add(
                new XElement("version", bnd.Version),
                new XElement("format", bnd.Format.ToString()),
                new XElement("bigendian", bnd.BigEndian.ToString()),
                new XElement("bitbigendian", bnd.BitBigEndian.ToString()),
                new XElement("unk18", bnd.Unk18.ToString()),
                files);

        if (game != null)
        {
            xml.AddFirst(new XElement("game", game.ToString()));
        }

        if (!string.IsNullOrEmpty(root))
            files.AddBeforeSelf(new XElement("root", root));

        WriteXmlManifest(xDoc, srcPath, recursive);
    }

    public override void Repack(string srcPath, bool recursive)
    {
        var bnd = new BND3();

        var xml = LoadXml(GetFolderXmlPath(srcPath));

        string root = xml.Element("root")?.Value ?? "";

        bnd.Compression = ReadCompressionInfoFromXml(xml);

        bnd.Version = xml.Element("version")!.Value;
        bnd.Format = (Binder.Format)Enum.Parse(typeof(Binder.Format), xml.Element("format")!.Value);
        bnd.BigEndian = bool.Parse(xml.Element("bigendian")!.Value);
        bnd.BitBigEndian = bool.Parse(xml.Element("bitbigendian")!.Value);
        bnd.Unk18 = int.Parse(xml.Element("unk18")!.Value);

        if (xml.Element("files") != null)
            ReadBinderFiles(bnd, xml.Element("files")!, srcPath, root, recursive);

        var destPath = GetRepackDestPath(srcPath, xml);

        Backup(destPath);
        bnd.Write(destPath);
    }
}