using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

/// <summary>
/// Parser of *just* DCX files, equivalent to previous Yabber.DCX.exe.
/// Only active when "DCX mode" is active, either through configuration or command-line.
/// Will just decompress the DCX and nothing else.
/// </summary>
public class WDCX : WSingleFileParser
{
    public override string Name => "DCX";

    public override bool Is(string path, byte[] _, out ISoulsFile? file)
    {
        file = null;
        return Configuration.Active.Dcx && DCX.Is(path);
    }

    public override string GetUnpackDestPath(string srcPath, bool recursive)
    {
        string sourceDir = new FileInfo(srcPath).Directory?.FullName!;
        string? location = Configuration.Active.Location;
        string fileName = Path.GetFileName(srcPath);
        if (!string.IsNullOrEmpty(location) && !recursive)
            sourceDir = location;
        sourceDir = Path.GetFullPath(sourceDir);
        if (srcPath.ToLower().EndsWith(".dcx"))
            return $"{sourceDir}\\{Path.GetFileNameWithoutExtension(srcPath)}";
        return $"{sourceDir}\\{fileName}.undcx";
    }

    public override string GetRepackDestPath(string srcPath, XElement xml)
    {
        var path = xml.Element("sourcePath")?.Value;
        if (path != null)
            srcPath = $"{path}\\{Path.GetFileName(srcPath)}";
        if (srcPath.ToLower().EndsWith(".undcx"))
            return srcPath.Substring(0, srcPath.Length - 6);
        return srcPath + ".dcx";
    }

    public string GetXmlPath(string path, bool recursive, bool repack = false)
    {
        var innerPath = repack ? path : GetUnpackDestPath(path, recursive);
        return $"{innerPath}-wbinder-dcx.xml";
    }

    public override bool Exists(string path)
    {
        return File.Exists(path);
    }

    public override bool ExistsUnpacked(string path)
    {
        return File.Exists(path);
    }

    public override bool IsUnpacked(string path)
    {
        var xmlPath = $"{path}-wbinder-dcx.xml";
        return File.Exists(xmlPath);
    }

    public override void Unpack(string srcPath, ISoulsFile? _, bool recursive)
    {
        string outPath = GetUnpackDestPath(srcPath, recursive);

        byte[] bytes = DCX.Decompress(srcPath, out DCX.Type comp);
        File.WriteAllBytes(outPath, bytes);

        XmlWriterSettings xws = new XmlWriterSettings();
        xws.Indent = true;
        XmlWriter xw = XmlWriter.Create(GetXmlPath(srcPath, recursive), xws);

        xw.WriteStartElement("dcx");

        AddLocationToXml(srcPath, recursive, xw);

        xw.WriteElementString("compression", comp.ToString());
        xw.WriteEndElement();
        xw.Close();
    }

    public override void Repack(string srcPath, bool recursive)
    {
        string xmlPath = GetXmlPath(srcPath, true);
        XElement xml = LoadXml(xmlPath);

        DCX.Type compression = (DCX.Type)Enum.Parse(typeof(DCX.Type), xml.Element("compression").Value);

        var outPath = GetRepackDestPath(srcPath, xml);
        WBUtil.Backup(outPath);

        DCX.Compress(File.ReadAllBytes(srcPath), compression, outPath);
    }
}