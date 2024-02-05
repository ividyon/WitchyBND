using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using PPlus;
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
    public override bool HasPreprocess => true;

    public override bool Is(string path, byte[] _, out ISoulsFile? file)
    {
        file = null;
        return Configuration.Dcx && DCX.Is(path);
    }

    public override string GetUnpackDestPath(string srcPath)
    {
        string sourceDir = new FileInfo(srcPath).Directory?.FullName;
        if (srcPath.ToLower().EndsWith(".dcx"))
            return $"{sourceDir}\\{Path.GetFileNameWithoutExtension(srcPath)}";
        return $"{srcPath}.undcx";
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

    public string GetXmlPath(string path, bool repack = false)
    {
        var innerPath = repack ? path : GetUnpackDestPath(path);
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

    public override void Unpack(string srcPath, ISoulsFile? _)
    {
        string outPath = GetUnpackDestPath(srcPath);

        byte[] bytes = DCX.Decompress(srcPath, out DCX.Type comp);
        File.WriteAllBytes(outPath, bytes);

        XmlWriterSettings xws = new XmlWriterSettings();
        xws.Indent = true;
        XmlWriter xw = XmlWriter.Create(GetXmlPath(srcPath), xws);

        xw.WriteStartElement("dcx");

        xw.WriteElementString("filename", Path.GetFileName(srcPath));
        if (!string.IsNullOrEmpty(Configuration.Args.Location))
            xw.WriteElementString("sourcePath", Path.GetDirectoryName(srcPath));

        xw.WriteElementString("compression", comp.ToString());
        xw.WriteEndElement();
        xw.Close();
    }

    public override void Repack(string srcPath)
    {
        string xmlPath = GetXmlPath(srcPath, true);
        XElement xml = LoadXml(xmlPath);

        DCX.Type compression = (DCX.Type)Enum.Parse(typeof(DCX.Type), xml.Element("compression").Value);

        var outPath = GetRepackDestPath(srcPath, xml);
        WBUtil.Backup(outPath);

        DCX.Compress(File.ReadAllBytes(srcPath), compression, outPath);
    }
}