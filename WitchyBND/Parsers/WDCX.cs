using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using SoulsFormats;

namespace WitchyBND.Parsers;

/// <summary>
/// Parser of *just* DCX files, equivalent to previous Yabber.DCX.exe.
/// Only active when "DCX mode" is active, either through configuration or command-line.
/// Will just decompress the DCX and nothing else.
/// </summary>
public class WDCX : WSingleFileParser
{
    public override string Name => "DCX";

    private bool _dcxConfig;
    public WDCX(bool dcxConfig)
    {
        _dcxConfig = dcxConfig;
    }

    public override bool Is(string path, byte[] _, out ISoulsFile? file)
    {
        file = null;
        if (_dcxConfig)
            return Configuration.Active.Dcx && DCX.Is(path);
        return DCX.Is(path);
    }

    public override bool? IsSimple(string path)
    {
        if (_dcxConfig)
            return Configuration.Active.Dcx && path.EndsWith(".dcx");
        return path.EndsWith(".dcx");
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
            return Path.Combine(sourceDir, Path.GetFileNameWithoutExtension(srcPath));
        return Path.Combine(sourceDir, $"{fileName}.undcx");
    }

    public override string GetRepackDestPath(string srcPath, XElement? xml)
    {
        var path = xml?.Element("sourcePath")?.Value;
        if (path != null)
            srcPath = Path.Combine(path.Replace('\\', Path.DirectorySeparatorChar), Path.GetFileName(srcPath));
        if (srcPath.ToLower().EndsWith(".undcx"))
            return srcPath.Substring(0, srcPath.Length - 6);
        return srcPath + ".dcx";
    }

    public string GetXmlPath(string path, bool recursive)
    {
        var innerPath = GetUnpackDestPath(path, recursive);
        return $"{innerPath}-wdcx.xml";
    }

    public string GetRepackXmlPath(string path, XElement? xml)
    {
        var innerPath = GetRepackDestPath(path, xml);
        return $"{innerPath.Replace(".dcx", "")}-wdcx.xml";
    }
    public override void WriteXmlManifest(XDocument xDoc, string srcPath, bool recursive, bool indent = true)
    {
        var destPath = GetXmlPath(srcPath, recursive);
        using var xw = new XmlTextWriter(destPath, Encoding.UTF8);
        xw.Formatting = Formatting.Indented;
        xw.Indentation = indent ? 2 : 0;
        xDoc.WriteTo(xw);
        xw.Close();
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
        var xmlPath = $"{path}-wdcx.xml";
        return File.Exists(xmlPath);
    }

    public override void Unpack(string srcPath, ISoulsFile? _, bool recursive)
    {
        string outPath = GetUnpackDestPath(srcPath, recursive);

        byte[] bytes = DCX.Decompress(srcPath, out DCX.CompressionInfo comp);
        File.WriteAllBytes(outPath, bytes);

        PrepareXmlManifest(srcPath, recursive, false, comp, out XDocument xDoc, null);
        
        WriteXmlManifest(xDoc, srcPath, recursive);
    }

    public override void Repack(string srcPath, bool recursive)
    {
        string xmlPath = GetRepackXmlPath(srcPath, null);
        XElement xml = LoadXml(xmlPath);

        DCX.CompressionInfo compression = ReadCompressionInfoFromXml(xml);

        var outPath = GetRepackDestPath(srcPath, xml);
        Backup(outPath);

        DCX.Compress(File.ReadAllBytes(srcPath), compression, outPath);
    }
}