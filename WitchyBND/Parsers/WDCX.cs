using System;
using System.IO;
using System.Xml;
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
    public override bool Is(string path)
    {
        return Configuration.Dcx && DCX.Is(path);
    }

    public override string GetUnpackDestPath(string srcPath)
    {
        string sourceDir = new FileInfo(srcPath).Directory?.FullName;
        if (srcPath.EndsWith(".dcx"))
            return $"{sourceDir}\\{Path.GetFileNameWithoutExtension(srcPath)}";
        return $"{srcPath}.undcx";
    }

    public override string GetRepackDestPath(string srcPath)
    {
        if (srcPath.EndsWith(".undcx"))
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

    public override void Unpack(string srcPath)
    {
        string outPath = GetUnpackDestPath(srcPath);

        byte[] bytes = DCX.Decompress(srcPath, out DCX.Type compression);
        File.WriteAllBytes(outPath, bytes);

        XmlWriterSettings xws = new XmlWriterSettings();
        xws.Indent = true;
        XmlWriter xw = XmlWriter.Create(GetXmlPath(srcPath), xws);

        xw.WriteStartElement("dcx");
        xw.WriteElementString("compression", compression.ToString());
        xw.WriteEndElement();
        xw.Close();
    }

    public override void Repack(string srcPath)
    {
        string xmlPath = GetXmlPath(srcPath, true);
        XmlDocument xml = new XmlDocument();
        xml.Load(xmlPath);
        DCX.Type compression = (DCX.Type)Enum.Parse(typeof(DCX.Type), xml.SelectSingleNode("dcx/compression").InnerText);

        var outPath = GetRepackDestPath(srcPath);
        WBUtil.Backup(outPath);

        DCX.Compress(File.ReadAllBytes(srcPath), compression, outPath);
    }
}