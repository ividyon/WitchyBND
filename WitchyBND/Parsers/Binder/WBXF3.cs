﻿using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WBXF3 : WBinderParser
{
    public override string Name => "BXF3";

    public override bool Is(string path)
    {
        return File.Exists(path) && (BXF3.IsBHD(path) || BXF3.IsBDT(path));
    }

    public override void Unpack(string srcPath)
    {
        string bdtPath;
        string bdtName;
        string bhdPath;
        string bhdName;
        string srcDirPath = new DirectoryInfo(srcPath).FullName;
        string nameWithoutExt = Path.GetFileNameWithoutExtension(srcPath);
        string destDir = GetUnpackDestDir(srcPath);

        if (BXF3.IsBHD(srcPath))
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

        var bxf = BXF3.Read(bhdPath, bdtPath);
        Directory.CreateDirectory(destDir);

        var root = "";
        if (Binder.HasNames(bxf.Format))
        {
            root = WBUtil.FindCommonRootPath(bxf.Files.Select(bxfFile => bxfFile.Name));
        }

        XElement files = WriteBinderFiles(bxf, destDir, root);

        var xml =
            new XElement(Name.ToLower(),
                new XElement("bhd_filename", bhdName),
                new XElement("bdt_filename", bdtName),
                new XElement("version", bxf.Version),
                new XElement("format", bxf.Format.ToString()),
                new XElement("bigendian", bxf.BigEndian.ToString()),
                new XElement("bitbigendian", bxf.BitBigEndian.ToString()),
                files);

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
        var bxf = new BXF3();

        var doc = XDocument.Load(GetBinderXmlPath(srcPath));
        if (doc.Root == null) throw new XmlException("XML has no root");
        XElement xml = doc.Root;
        string bhdName = doc.Root.Element("bhd_filename")!.Value;
        string bdtName = doc.Root.Element("bdt_filename")!.Value;

        string root = xml.Element("root")?.Value ?? "";

        bxf.Version = xml.Element("version")!.Value;
        bxf.Format = (Binder.Format)Enum.Parse(typeof(Binder.Format), xml.Element("format")!.Value);
        bxf.BigEndian = bool.Parse(xml.Element("bigendian")!.Value);
        bxf.BitBigEndian = bool.Parse(xml.Element("bitbigendian")!.Value);

        if (xml.Element("files") != null)
            ReadBinderFiles(bxf, xml.Element("files")!, srcPath, root);

        var bhdDestPath = GetRepackDestPath(srcPath, bhdName);
        var bdtDestPath = GetRepackDestPath(srcPath, bdtName);
        WBUtil.Backup(bhdDestPath);
        WBUtil.Backup(bdtDestPath);
        bxf.Write(bhdDestPath, bdtDestPath);
    }
}