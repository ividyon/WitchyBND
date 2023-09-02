using System;
using System.IO;
using System.Xml;
using SoulsFormats;

namespace WitchyBND.Parsers;

/// <summary>
/// Parser of *just* DCX files, equivalent to previous Yabber.DCX.exe.
/// Only active when "DCX mode" is active, either through configuration or command-line.
/// Will just decompress the DCX and nothing else.
/// </summary>
public class WDCX : WFileParser
{

    public override string Name => "DCX";
    public override bool Is(string path)
    {
        return Program.Configuration.Dcx && DCX.Is(path);
    }

    public override bool Exists(string path)
    {
        return File.Exists(path);
    }

    public override bool UnpackedExists(string path)
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
        string sourceDir = new FileInfo(srcPath).Directory?.FullName;
        string outPath;
        if (srcPath.EndsWith(".dcx"))
            outPath = $"{sourceDir}\\{Path.GetFileNameWithoutExtension(srcPath)}";
        else
            outPath = $"{srcPath}.undcx";

        byte[] bytes = DCX.Decompress(srcPath, out DCX.Type compression);
        File.WriteAllBytes(outPath, bytes);

        XmlWriterSettings xws = new XmlWriterSettings();
        xws.Indent = true;
        XmlWriter xw = XmlWriter.Create($"{outPath}-wbinder-dcx.xml", xws);

        xw.WriteStartElement("dcx");
        xw.WriteElementString("compression", compression.ToString());
        xw.WriteEndElement();
        xw.Close();
    }

    public override void Repack(string srcPath)
    {
        string xmlPath = $"{srcPath}-wbinder-dcx.xml";
        XmlDocument xml = new XmlDocument();
        xml.Load(xmlPath);
        DCX.Type compression = (DCX.Type)Enum.Parse(typeof(DCX.Type), xml.SelectSingleNode("dcx/compression").InnerText);

        string outPath;
        if (srcPath.EndsWith(".undcx"))
            outPath = srcPath.Substring(0, srcPath.Length - 6);
        else
            outPath = srcPath + ".dcx";

        if (File.Exists(outPath) && !File.Exists(outPath + ".bak"))
            File.Move(outPath, outPath + ".bak");

        DCX.Compress(File.ReadAllBytes(srcPath), compression, outPath);
    }
}