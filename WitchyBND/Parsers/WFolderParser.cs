using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    protected void WarnAboutKrak(DCX.Type compression, int count)
    {
        if (compression is not DCX.Type.DCX_KRAK and not DCX.Type.DCX_KRAK_MAX) return;
        if (WarnedAboutKrak) return;
        if (count <= 10) return;

        errorService.RegisterNotice(@$"DCX compression is set to DCX_KRAK or DCX_KRAK_MAX.
Kraken compression is slightly more compact, but extremely slow.
Consider switching to a faster compression such as: DCX_DFLT_11000_44_9_15
Simply replace the compression level in the {GetFolderXmlFilename()} file to this value.");

        WarnedAboutKrak = true;
    }

    protected bool WarnAboutZstd(DCX.Type compression)
    {
        if (compression is not DCX.Type.DCX_ZSTD) return false;
        if (WarnedAboutZstd) return true;

        errorService.RegisterNotice(@$"DCX compression is set to DCX_ZSTD.
Zstd compression is currently not supported. The file will be compressed using Deflate compression.
This should not cause adverse effects in the game.");

        WarnedAboutZstd = true;
        return true;
    }

    public override string GetUnpackDestPath(string srcPath)
    {
        string sourceDir = new FileInfo(srcPath).Directory?.FullName;
        if (!string.IsNullOrEmpty(Configuration.Active.Location))
            sourceDir = Configuration.Active.Location;
        string fileName = Path.GetFileName(srcPath);
        return $"{sourceDir}\\{fileName.Replace('.', '-')}";
    }

    public virtual string GetRepackDestPath(string srcDirPath, XElement xml, string filenameElement = "filename")
    {
        var filename = xml.Element(filenameElement)?.Value;
        if (filename == null)
            throw new InvalidDataException("XML does not have filename.");
        var sourceDir = xml.Element("sourcePath")?.Value;
        if (sourceDir != null)
        {
            return $"{sourceDir}\\{filename}";
        }
        string targetDir = new DirectoryInfo(srcDirPath).Parent?.FullName;
        return $"{targetDir}\\{filename}";
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
        dir = string.IsNullOrEmpty(dir) ? dir : $"{dir}\\";

        if (File.Exists($"{dir}{GetFolderXmlFilename(name)}"))
        {
            return $"{dir}_witchy-{name}.xml";
        }

        if (File.Exists($"{dir}_yabber-{name}.xml"))
        {
            return $"{dir}_yabber-{name}.xml";
        }

        return $"{dir}{GetFolderXmlFilename(name)}";
    }

    public static List<string> GetFolderFilePaths(XElement filesElement, string srcDirPath)
    {
        return filesElement.Elements("file").Select(file => {
            XElement? pathEl = file.Element("path");
            if (pathEl == null)
                throw new FriendlyException("File node missing path tag.");
            string path = pathEl.Value;
            string suffix = file.Element("suffix")?.Value ?? "";
            string inPath =
                $@"{srcDirPath}\{Path.GetDirectoryName(path)}\{Path.GetFileNameWithoutExtension(path)}{suffix}{Path.GetExtension(path)}";
            if (!File.Exists(inPath))
                throw new FriendlyException($"File not found: {inPath}");
            return inPath;
        }).ToList();
    }
}