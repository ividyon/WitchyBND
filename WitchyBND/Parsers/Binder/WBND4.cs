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

    public override void Unpack(string srcPath, ISoulsFile? file, string? recursiveOriginPath)
    {
        Unpack(srcPath, file, recursiveOriginPath, null);
    }

    public void Unpack(string srcPath, ISoulsFile? file, string? recursiveOriginPath, WBUtil.GameType? game)
    {
        BND4 bnd = (file as BND4)!;
        string srcName = Path.GetFileName(srcPath);
        string destDir = GetUnpackDestPath(srcPath, recursiveOriginPath);
        Directory.CreateDirectory(destDir);

        var root = "";
        if (Binder.HasNames(bnd.Format))
        {
            root = WBUtil.FindCommonRootPath(bnd.Files.Select(bndFile => bndFile.Name));
        }

        XElement filename = new XElement("filename", srcName);
        XElement files = WriteBinderFiles(bnd, srcPath, destDir, root);

        var xml =
            new XElement(XmlTag,
                filename,
                new XElement("compression", bnd.Compression.ToString()),
                new XElement("version", bnd.Version),
                new XElement("format", bnd.Format.ToString()),
                new XElement("bigendian", bnd.BigEndian.ToString()),
                new XElement("bitbigendian", bnd.BitBigEndian.ToString()),
                new XElement("unicode", bnd.Unicode.ToString()),
                new XElement("extended", $"0x{bnd.Extended:X2}"),
                new XElement("unk04", bnd.Unk04.ToString()),
                new XElement("unk05", bnd.Unk05.ToString()),
                files);

        if (Version > 0) xml.SetAttributeValue(VersionAttributeName, Version.ToString());

        if (!string.IsNullOrEmpty(Configuration.Active.Location))
            filename.AddAfterSelf(new XElement("sourcePath", Path.GetFullPath(Path.GetDirectoryName(srcPath))));

        if (game != null)
        {
            filename.AddAfterSelf(new XElement("game", game.ToString()));
        }
        if (!string.IsNullOrEmpty(root))
            files.AddBeforeSelf(new XElement("root", root));

        using var xw = XmlWriter.Create($"{destDir}\\{GetFolderXmlFilename()}", new XmlWriterSettings
        {
            Indent = true
        });
        xml.WriteTo(xw);
        xw.Close();
    }

    public override void Repack(string srcPath, string? recursiveOriginPath)
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

        if (xml.Element("files") != null)
            ReadBinderFiles(bnd, xml.Element("files")!, srcPath, recursiveOriginPath, root);

        var destPath = GetRepackDestPath(srcPath, recursiveOriginPath, xml);

        WBUtil.Backup(destPath);

        WarnAboutKrak(compression, bnd.Files.Count);

        if (WarnAboutZstd(compression))
        {
            bnd.Compression = DCX.Type.DCX_DFLT_11000_44_9;
        }

        bnd.Write(destPath);
    }
}