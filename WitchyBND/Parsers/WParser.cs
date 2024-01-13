using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using SoulsFormats;
using WitchyBND.CliModes;
using WitchyLib;

namespace WitchyBND.Parsers;

public enum WFileParserVerb
{
    None = 0,
    Unpack = 1,
    Serialize = 2,
}

public abstract class WFileParser
{
    public virtual WFileParserVerb Verb => WFileParserVerb.Unpack;
    public virtual bool IncludeInList => true;
    public abstract string Name { get; }
    public virtual int Version => 0;
    public virtual string XmlTag => Name.ToLower();
    public abstract bool Is(string path, byte[]? data, out ISoulsFile? file);
    public abstract bool Exists(string path);
    public abstract bool ExistsUnpacked(string path);
    public abstract bool IsUnpacked(string path);

    public virtual int GetUnpackedVersion(string path)
    {
        return Version;
    }
    public virtual bool UnpackedFitsVersion(string path)
    {
        if (Version == 0) return true;
        if (GetUnpackedVersion(path) < Version)
            return false;
        return true;
    }

    public abstract void Unpack(string srcPath, ISoulsFile? file);
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
    private static bool IsRead<TFormat>(string path, out ISoulsFile? file) where TFormat : SoulsFile<TFormat>, new()
    {
        if (SoulsFile<TFormat>.IsRead(path, out TFormat format))
        {
            file = format;
            return true;
        }

        file = null;
        return false;
    }

    private static bool IsRead<TFormat>(byte[] data, out ISoulsFile? file) where TFormat : SoulsFile<TFormat>, new()
    {
        if (SoulsFile<TFormat>.IsRead(data, out TFormat format))
        {
            file = format;
            return true;
        }

        file = null;
        return false;
    }

    public static bool IsRead<TFormat>(string path, byte[]? data, out ISoulsFile? file) where TFormat : SoulsFile<TFormat>, new()
    {
        return data != null ? IsRead<TFormat>(data, out file) : IsRead<TFormat>(path, out file);
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
    private static bool WarnedAboutKrak { get; set; }

    public override WFileParserVerb Verb => WFileParserVerb.Unpack;


    public static void WarnAboutKrak()
    {
        if (WarnedAboutKrak) return;

        Program.RegisterNotice(@"DCX compression is set to DCX_KRAK or DCX_KRAK_MAX.
Kraken compression is extremely slow - taking up almost 100% of repacking time - and recommended only for for the final repack before releasing something to the public.
During development, you may wish to switch to a faster compression such as: DCX_DFLT_11000_44_9_15
Simply replace the compression level in the Witchy XML to this value.");

        WarnedAboutKrak = true;
    }

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
        return doc.Root != null && doc.Root.Name == XmlTag;
    }

    public virtual string GetBinderXmlFilename(string? name = null)
    {
        name ??= XmlTag.ToLower();
        return $"_witchy-{name}.xml";
    }

    public virtual string GetBinderXmlPath(string dir, string? name = null)
    {
        name ??= XmlTag.ToLower();
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
        var bag = new ConcurrentBag<XElement>();

        var files = new XElement("files");
        var pathCounts = new ConcurrentDictionary<string, int>();
        var resultingPaths = new ConcurrentStack<string>();

        void ParallelCallback(BinderFile file, ParallelLoopState state, long i)
        {
            Callback(file, i);
        }

        void Callback(BinderFile file, long i) {
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
            // Edge case for PC save files
            else if (bnd.Format == Binder.Format.Names1 && bnd.Files.Any(f => f.Name.StartsWith("USER_DATA")))
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
            resultingPaths.Push(destPath);
            File.WriteAllBytes(destPath, bytes);

            bag.Add(fileElement);
        }

        if (Configuration.Parallel)
            Parallel.ForEach(bnd.Files, ParallelCallback);
        else
        {
            for (var i = 0; i < bnd.Files.Count; i++)
            {
                Callback(bnd.Files[i], i);
            }
        }

        if (Configuration.Parallel)
        {
            if (Binder.HasIDs(bnd.Format))
            {
                files.Add(bag.OrderBy(el => el.Element("id")!.Value));
            }
            else if (Binder.HasNames(bnd.Format))
            {
                files.Add(bag.OrderBy(el => el.Element("path")!.Value));
            }
            else
                files.Add(bag);
        }
        else
            files.Add(bag);

        if (Configuration.Recursive)
        {
            ParseMode.ParseFiles(resultingPaths.ToList(), true);
        }

        return files;
    }

    protected static void ReadBinderFiles(IBinder bnd, XElement filesElement, string srcDirPath, string root)
    {
        var bag = new ConcurrentBag<BinderFile>();

        void Callback(XElement file) {
            if (file.Element("path") == null)
                throw new FriendlyException("File node missing path tag.");

            string strFlags = file.Element("flags")?.Value ?? "Flag1";
            string strId = file.Element("id")?.Value ?? "-1"; // Edge case for PC save files
            string path = file.Element("path")!.Value;
            string suffix = file.Element("suffix")?.Value ?? "";
            string strCompression = file.Element("compression_type")?.Value ?? DCX.Type.Zlib.ToString();
            string name = Path.Combine(root, path);

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
            bag.Add(new BinderFile(flags, id, name, bytes)
            {
                CompressionType = compressionType
            });
        }
        if (Configuration.Parallel)
            Parallel.ForEach(filesElement.Elements("file"), Callback);
        else
        {
            foreach (XElement element in filesElement.Elements("file"))
            {
                Callback(element);
            }
        }

        bnd.Files = bag.ToList();
    }
}

public class UnsortedFileFormat
{
    public string SearchPattern { get; }
    public Binder.FileFlags FileFlags { get; }

    public DCX.Type Compression { get; }

    public UnsortedFileFormat(string pattern, Binder.FileFlags flags, DCX.Type compression = DCX.Type.Zlib)
    {
        SearchPattern = pattern;
        FileFlags = flags;
        Compression = compression;
    }
}
public abstract class WUnsortedBinderParser : WBinderParser
{
    public abstract string Extension { get; }
    public abstract UnsortedFileFormat[] PackedFormats { get; }

    public virtual bool EndsInExtension(string path)
    {
        return path.EndsWith($".{Extension}") || path.EndsWith($".{Extension}.dcx");
    }

    protected virtual void ReadUnsortedBinderFiles(IBinder bnd, string srcDirPath, string root)
    {
        string searchPattern = string.Join(';', PackedFormats.Select(f => f.SearchPattern));
        var i = 0;
        foreach (string filePath in Directory.GetFiles(srcDirPath, searchPattern, SearchOption.AllDirectories))
        {
            string filename = Path.GetFileName(filePath);
            UnsortedFileFormat format = PackedFormats.FirstOrDefault(a =>
                FileSystemName.MatchesWin32Expression(a.SearchPattern.AsSpan(), filename));
            if (format == null)
                throw new InvalidDataException(
                    $"File {filename} passed pattern checks, but was not found among formats.");
            string name = !string.IsNullOrEmpty(root) ? Path.Combine(root, Path.GetRelativePath(srcDirPath, filePath)) : filePath;
            byte[] bytes = File.ReadAllBytes(filePath);
            bnd.Files.Add(new BinderFile(format.FileFlags, i, name, bytes)
            {
                CompressionType = format.Compression
            });
            i++;
        }
    }
}

public abstract class WXMLParser : WSingleFileParser
{
    public virtual string VersionAttributeName => "WitchyVersion";
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
        return doc.Root != null && doc.Root.Name.ToString().ToLower() == XmlTag.ToLower();
    }
}