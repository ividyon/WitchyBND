using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using SoulsFormats;
using WitchyBND.CliModes;
using WitchyBND.Services;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WBND4 : WBinderParser
{
    public override string Name => "BND4";

    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        return IsRead<BND4>(path, data, out file);
    }

    public override bool? IsSimple(string path)
    {
        return null;
    }

    public override void Unpack(string srcPath, ISoulsFile? file, bool recursive)
    {
        Unpack(srcPath, file, recursive, null);
    }

    public void Unpack(string srcPath, ISoulsFile? file, bool recursive, WBUtil.GameType? game)
    {
        BND4 bnd = (file as BND4)!;
        string destDir = GetUnpackDestPath(srcPath, recursive);
        Directory.CreateDirectory(destDir);

        var root = "";
        if (Binder.HasNames(bnd.Format))
        {
            root = WBUtil.FindCommonRootPath(bnd.Files.Select(bndFile => bndFile.Name));
        }

        XElement files = WriteBinderFiles(bnd, destDir, root);

        var xml = PrepareXmlManifest(srcPath, recursive, false, bnd.Compression, out XDocument xDoc, root);
        xml.Add(
                new XElement("version", bnd.Version),
                new XElement("format", bnd.Format.ToString()),
                new XElement("bigendian", bnd.BigEndian.ToString()),
                new XElement("bitbigendian", bnd.BitBigEndian.ToString()),
                new XElement("unicode", bnd.Unicode.ToString()),
                new XElement("extended", $"0x{bnd.Extended:X2}"),
                new XElement("unk04", bnd.Unk04.ToString()),
                new XElement("unk05", bnd.Unk05.ToString()),
                files);

        if (game != null)
        {
            xml.AddFirst(new XElement("game", game.ToString()));
        }

        WriteXmlManifest(xDoc, srcPath, recursive);
    }

    public override void Repack(string srcPath, bool recursive)
    {
        var bnd = new BND4();

        XElement xml = LoadXml(GetFolderXmlPath(srcPath));

        string root = xml.Element("root")?.Value ?? "";

        bnd.Compression = ReadCompressionInfoFromXml(xml);

        bnd.Version = xml.Element("version")!.Value;
        bnd.Format = (Binder.Format)Enum.Parse(typeof(Binder.Format), xml.Element("format")!.Value);
        bnd.BigEndian = bool.Parse(xml.Element("bigendian")!.Value);
        bnd.BitBigEndian = bool.Parse(xml.Element("bitbigendian")!.Value);
        bnd.Unicode = bool.Parse(xml.Element("unicode")!.Value);
        bnd.Extended = Convert.ToByte(xml.Element("extended")!.Value, 16);
        bnd.Unk04 = bool.Parse(xml.Element("unk04")!.Value);
        bnd.Unk05 = bool.Parse(xml.Element("unk05")!.Value);

        if (xml.Element("files") != null)
            ReadBinderFiles(bnd, xml.Element("files")!, srcPath, root, recursive);

        var destPath = GetRepackDestPath(srcPath, xml);

        Backup(destPath);

        WarnAboutKrak(bnd.Compression, bnd.Files.Count);

        bnd.Write(destPath);
    }
}