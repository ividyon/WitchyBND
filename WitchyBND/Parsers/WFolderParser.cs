using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND.Parsers;



public abstract class WFolderParser : WFileParser
{
    private static bool WarnedAboutKrak { get; set; }
    private static bool WarnedAboutZstd { get; set; }

    public override WFileParserVerb Verb => WFileParserVerb.Unpack;

    public override int GetUnpackedVersion(string path)
    {
        var doc = XDocument.Load(GetFolderXmlPath(path));
        var attr = doc.Root?.Attribute(VersionAttributeName);
        if (attr == null) return 0;
        return int.Parse(attr.Value);
    }

    protected void WarnAboutKrak(DCX.CompressionInfo compression, int count)
    {
        if (compression.Type is not DCX.Type.DCX_KRAK) return;
        if (WarnedAboutKrak) return;
        if (count <= 10) return;

        errorService.RegisterNotice(@$"DCX compression is set to DCX_KRAK.
Kraken compression is slightly more compact, but extremely slow.
Consider switching to a faster compression such as: DCX_DFLT
Simply replace the compression level in the {GetFolderXmlFilename()} file to this value.");

        WarnedAboutKrak = true;
    }

    public override string GetUnpackDestPath(string srcPath, bool recursive)
    {
        string sourceDir = new FileInfo(srcPath).Directory?.FullName!;
        string? location = Configuration.Active.Location;
        string fileName = Path.GetFileName(srcPath);
        if (!string.IsNullOrEmpty(location) && !recursive)
            sourceDir = location;
        sourceDir = Path.GetFullPath(sourceDir);
        string tarDir = fileName.Replace('.', '-');
        if (fileName == tarDir)
        {
            tarDir += "-unpacked";
        }
        return Path.Combine(sourceDir, tarDir);
    }

    public override XElement PrepareXmlManifest(string srcPath, bool recursive, bool skipFilename,
        DCX.CompressionInfo? compression, out XDocument xDoc, string? root)
    {
        var destPath = GetUnpackDestPath(srcPath, recursive);
        Directory.CreateDirectory(destPath);
        return base.PrepareXmlManifest(srcPath, recursive, skipFilename, compression, out xDoc, root);
    }

    public override void WriteXmlManifest(XDocument xDoc, string srcPath, bool recursive, bool indent = true)
    {
        var destPath = GetUnpackDestPath(srcPath, recursive);

        using var xw = new XmlTextWriter(GetFolderXmlPath(destPath), Encoding.UTF8);
        xw.Formatting = Formatting.Indented;
        xw.Indentation = indent ? 2 : 0;
        xDoc.WriteTo(xw);
        xw.Close();
    }

    public virtual string GetRepackDestPath(string srcDirPath, XElement xml, string filenameElement = "filename")
    {
        var filename = xml.Element(filenameElement)?.Value;
        if (filename == null)
            throw new InvalidDataException("XML does not have filename.");
        var sourceDir = xml.Element("sourcePath")?.Value;
        if (sourceDir != null)
        {
            return Path.GetFullPath(Path.Combine(srcDirPath, "..", sourceDir.Replace('\\', Path.DirectorySeparatorChar), filename));
        }
        string targetDir = new DirectoryInfo(srcDirPath).Parent?.FullName!;
        return Path.GetFullPath(Path.Combine(targetDir, filename));
    }
    // public virtual string GetRepackDestPath(string srcDirPath, string destFileName)
    // {
    //     string targetDir = new DirectoryInfo(srcDirPath).Parent?.FullName;
    //     return $"{targetDir}\\{destFileName}";
    // }

    public override bool Exists(string path)
    {
        return File.Exists(path);
    }

    public override bool ExistsUnpacked(string path)
    {
        return Directory.Exists(path);
    }

    public override bool IsUnpacked(string path)
    {
        if (!Directory.Exists(path)) return false;

        string xmlPath = Path.Combine(path, GetFolderXmlFilename());
        if (!File.Exists(xmlPath)) return false;

        var doc = XDocument.Load(xmlPath);
        return doc.Root != null && doc.Root.Name == XmlTag;
    }

    public virtual string GetFolderXmlFilename(string? name = null)
    {
        name ??= XmlTag.ToLower();
        return $"_witchy-{name}.xml";
    }

    public virtual string GetFolderXmlPath(string dir, string? name = null)
    {
        name ??= XmlTag.ToLower();
        return Path.Combine(dir, GetFolderXmlFilename(name));
    }

    public static List<string> GetFolderFilePaths(XElement filesElement, string srcDirPath)
    {
        return filesElement.Elements("file").Select(file => {
            XElement? pathEl = file.Element("path");
            if (pathEl == null)
                throw new FriendlyException("File node missing path tag.");
            string path = pathEl.Value;
            string suffix = file.Element("suffix")?.Value ?? "";
            string inPath = Path.Combine(srcDirPath, Path.GetDirectoryName(path)!, $"{Path.GetFileNameWithoutExtension(path)}{suffix}{Path.GetExtension(path)}");
            if (!File.Exists(inPath))
                throw new FriendlyException($"File not found: {inPath}");
            return inPath;
        }).ToList();
    }
}