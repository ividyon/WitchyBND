using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using SoulsFormats;
using WitchyBND.CliModes;
using WitchyLib;

namespace WitchyBND.Parsers;

public enum WFileParserVerb
{
    Unpack = 0,
    Serialize = 1,
}

public abstract class WFileParser
{
    public virtual WFileParserVerb Verb => WFileParserVerb.Unpack;
    public abstract string Name { get; }
    public abstract bool Is(string path);
    public abstract bool Exists(string path);
    public abstract bool ExistsUnpacked(string path);
    public abstract bool IsUnpacked(string path);
    public abstract void Unpack(string srcPath);
    public abstract void Repack(string srcPath);

    public static void AddLocationToXml(string path)
    {
        XElement xml = LoadXml(path);
        AddLocationToXml(path, xml);
    }

    public static XElement RemoveLocationFromXml(XElement xml)
    {
        xml.Element("filename")?.Remove();
        xml.Element("sourcePath")?.Remove();
        return xml;
    }
    public static void AddLocationToXml(string path, XElement xml)
    {
        if (!string.IsNullOrEmpty(Configuration.Args.Location))
            xml.AddFirst("sourcePath", Path.GetDirectoryName(path));
        xml.AddFirst("filename", Path.GetFileName(path));
    }
    public static XElement LoadXml(string path)
    {
        XDocument doc = XDocument.Load(path);
        if (doc.Root == null) throw new XmlException("XML has no root");
        return doc.Root;
    }
}

public abstract class WSingleFileParser : WFileParser
{
    public abstract string GetUnpackDestPath(string srcPath);
    public abstract string GetRepackDestPath(string srcPath, XElement xml);
    public override bool Exists(string path)
    {
        return File.Exists(path);
    }
    public override bool ExistsUnpacked(string path)
    {
        return File.Exists(path);
    }
}

public abstract class WFolderParser : WFileParser
{
    public override WFileParserVerb Verb => WFileParserVerb.Unpack;

    public virtual string GetUnpackDestDir(string srcPath)
    {
        string sourceDir = new FileInfo(srcPath).Directory?.FullName;
        if (!string.IsNullOrEmpty(Configuration.Args.Location))
            sourceDir = Configuration.Args.Location;
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

        string xmlPath = Path.Combine(path, GetBinderXmlFilename());
        if (!File.Exists(xmlPath)) return false;

        var doc = XDocument.Load(xmlPath);
        return doc.Root != null && doc.Root.Name == Name.ToLower();
    }

    public virtual string GetBinderXmlFilename(string name = null)
    {
        name ??= Name.ToLower();
        return $"_witchy-{name}.xml";
    }

    public virtual string GetBinderXmlPath(string dir, string name = null)
    {
        name ??= Name.ToLower();
        dir = string.IsNullOrEmpty(dir) ? dir : $"{dir}\\";

        if (File.Exists($"{dir}{GetBinderXmlFilename(name)}"))
        {
            return $"{dir}_witchy-{name}.xml";
        }

        if (File.Exists($"{dir}_yabber-{name}.xml"))
        {
            return $"{dir}_yabber-{name}.xml";
        }

        return $"{dir}{GetBinderXmlFilename(name)}";
    }
}

public abstract class WBinderParser : WFolderParser
{
    protected static XElement WriteBinderFiles(IBinder bnd, string destDirPath, string root)
    {
        var files = new XElement("files");
        var pathCounts = new Dictionary<string, int>();
        var resultingPaths = new List<string>();

        for (int i = 0; i < bnd.Files.Count; i++)
        {
            BinderFile file = bnd.Files[i];

            string path;
            if (Binder.HasNames(bnd.Format))
            {
                path = WBUtil.UnrootBNDPath(file.Name, root);
            }
            else if (Binder.HasIDs(bnd.Format))
            {
                path = file.ID.ToString();
            }
            else
            {
                path = i.ToString();
            }

            var fileElement = new XElement("file",
                new XElement("flags", file.Flags.ToString()));

            if (Binder.HasIDs(bnd.Format))
                fileElement.Add(new XElement("id", file.ID.ToString()));

            fileElement.Add(new XElement("path", path));

            string suffix = "";
            if (pathCounts.ContainsKey(path))
            {
                pathCounts[path]++;
                suffix = $" ({pathCounts[path]})";
                fileElement.Add(new XElement("suffix", suffix));
            }
            else
            {
                pathCounts[path] = 1;
            }

            if (file.CompressionType != DCX.Type.Zlib)
                fileElement.Add("compression_type", file.CompressionType.ToString());

            byte[] bytes = file.Bytes;
            string destPath =
                $@"{destDirPath}\{Path.GetDirectoryName(path)}\{Path.GetFileNameWithoutExtension(path)}{suffix}{Path.GetExtension(path)}";
            Directory.CreateDirectory(Path.GetDirectoryName(destPath));
            resultingPaths.Add(destPath);
            File.WriteAllBytes(destPath, bytes);

            files.Add(fileElement);
        }

        if (Configuration.Recursive)
        {
            ParseMode.ParseFiles(resultingPaths, true);
        }

        return files;
    }

    protected static void ReadBinderFiles(IBinder bnd, XElement filesElement, string srcDirPath, string root)
    {
        foreach (XElement file in filesElement.Elements("file"))
        {
            if (file.Element("path") == null)
                throw new FriendlyException("File node missing path tag.");

            string strFlags = file.Element("flags")?.Value ?? "Flag1";
            string strId = file.Element("id")?.Value ?? "-1";
            string path = file.Element("path")!.Value;
            string suffix = file.Element("suffix")?.Value ?? "";
            string strCompression = file.Element("compression_type")?.Value ?? DCX.Type.Zlib.ToString();
            string name = root + path;

            if (!Enum.TryParse(strFlags, out Binder.FileFlags flags))
                throw new FriendlyException(
                    $"Could not parse file flags: {strFlags}\nFlags must be comma-separated list of flags.");

            if (!int.TryParse(strId, out int id))
                throw new FriendlyException($"Could not parse file ID: {strId}\nID must be a 32-bit signed integer.");

            if (!Enum.TryParse(strCompression, out DCX.Type compressionType))
                throw new FriendlyException(
                    $"Could not parse compression type: {strCompression}\nCompression type must be a valid DCX Type.");

            string inPath =
                $@"{srcDirPath}\{Path.GetDirectoryName(path)}\{Path.GetFileNameWithoutExtension(path)}{suffix}{Path.GetExtension(path)}";
            if (!File.Exists(inPath))
                throw new FriendlyException($"File not found: {inPath}");

            byte[] bytes = File.ReadAllBytes(inPath);
            bnd.Files.Add(new BinderFile(flags, id, name, bytes)
            {
                CompressionType = compressionType
            });
        }
    }
}

public abstract class WXMLParser : WSingleFileParser
{
    public override WFileParserVerb Verb => WFileParserVerb.Serialize;

    public override string GetUnpackDestPath(string srcPath)
    {
        if (string.IsNullOrEmpty(Configuration.Args.Location))
            return $"{srcPath}.xml";
        return $"{Configuration.Args.Location}\\{Path.GetFileName(srcPath)}.xml";
    }

    public override string GetRepackDestPath(string srcPath, XElement xml)
    {
        var path = xml.Element("sourcePath")?.Value;
        if (path != null)
        {
            return $"{path}\\{Path.GetFileName(srcPath).Replace(".xml", "")}";
        }
        return srcPath.Replace(".xml", "");
    }

    public override bool IsUnpacked(string path)
    {
        if (Path.GetExtension(path) != ".xml")
            return false;

        var doc = XDocument.Load(path);
        return doc.Root != null && doc.Root.Name == Name.ToLower();
    }
}