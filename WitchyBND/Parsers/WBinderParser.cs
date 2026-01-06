using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using SoulsFormats;
using WitchyBND.CliModes;
using WitchyLib;

namespace WitchyBND.Parsers;

public abstract class WBinderParser : WFolderParser
{
    public override bool HasPreprocess => Configuration.Active.Recursive;

    public override bool Preprocess(string srcPath, bool recursive,
        ref Dictionary<string, (WFileParser, ISoulsFile)> files)
    {
        ISoulsFile? file = null;
        bool unpacked = ExistsUnpacked(srcPath) && IsUnpacked(srcPath);
        bool packed = !unpacked && Exists(srcPath) && IsSimpleFirst(srcPath, null, out file);
        if (file != null)
            files.TryAdd(srcPath, (this, file));
        if (!unpacked && !packed) return false;
        if (!Configuration.Active.Recursive) return true;
        var parsers = ParseMode.GetPreprocessors(true);
        if (packed && file != null && file is IBinder bnd)
        {
            string destDir = GetUnpackDestPath(srcPath, recursive);
            var pathCounts = new Dictionary<string, int>();
            foreach (var bndFile in bnd.Files)
            {

                string bndPath = GetBinderFilePath(bnd, bndFile, null);

                string suffix = "";
                if (pathCounts.ContainsKey(bndPath))
                {
                    pathCounts[bndPath]++;
                    suffix = $" ({pathCounts[bndPath]})";
                }
                else
                {
                    pathCounts[bndPath] = 1;
                }

                var path = bndPath.ToOSPath();
                string destFilePath = OSPath.Combine(destDir, OSPath.GetDirectoryName(path)!,
                    $"{OSPath.GetFileNameWithoutExtension(path)}{suffix}{OSPath.GetExtension(path)}");
                foreach (var parser in parsers)
                {
                    bool toBreak = parser.Preprocess(destFilePath, true, ref files);
                    if (toBreak)
                        break;
                }
            }
        }
        else if (unpacked)
        {
            foreach (string insidePath in Directory.EnumerateFiles(srcPath, "*", SearchOption.AllDirectories))
            {
                foreach (var parser in parsers)
                {
                    bool toBreak = parser.Preprocess(insidePath, true, ref files);
                    if (toBreak)
                        break;
                }
            }
        }

        return true;
    }

    protected static string GetBinderFilePath(IBinder bnd, BinderFile file, string? root)
    {
        root ??= BndPath.FindCommonBndRootPath(bnd.Files.Select(bndFile => bndFile.Name));
        string path;
        if (Binder.HasNames(bnd.Format))
        {
            path = BndPath.Unroot(file.Name, root);
        }
        else if (Binder.HasIDs(bnd.Format))
        {
            path = file.ID.ToString();
        }
        else
        {
            path = bnd.Files.IndexOf(file).ToString();
        }

        return path;
    }

    protected static XElement WriteBinderFiles(IBinder bnd, string destDirPath, string? root)
    {
        var bag = new ConcurrentDictionary<string, XElement>();

        var files = new XElement("files");
        var pathCounts = new ConcurrentDictionary<string, int>();
        var resultingPaths = new ConcurrentStack<string>();

        void Callback(BinderFile file)
        {
            string bndPath = GetBinderFilePath(bnd, file, root);

            var fileElement = new XElement("file",
                new XElement("flags", file.Flags.ToString()));

            if (Binder.HasIDs(bnd.Format))
                fileElement.Add(new XElement("id", file.ID.ToString()));
            // Edge case for PC save files
            else if (bnd.Format == Binder.Format.Names1 && bnd.Files.Any(f => f.Name.StartsWith("USER_DATA")))
                fileElement.Add(new XElement("id", file.ID.ToString()));

            fileElement.Add(new XElement("path", bndPath));

            string suffix = "";
            if (pathCounts.ContainsKey(bndPath))
            {
                pathCounts[bndPath]++;
                suffix = $" ({pathCounts[bndPath]})";
                fileElement.Add(new XElement("suffix", suffix));
            }
            else
            {
                pathCounts[bndPath] = 1;
            }
            if (file.CompressionInfo.Type != DCX.Type.Zlib)
                WriteCompressionInfoToXml(fileElement, file.CompressionInfo);

            byte[] bytes = file.Bytes;

            bndPath = bndPath.ToOSPath();
            string destPath = OSPath.Combine(destDirPath, OSPath.GetDirectoryName(bndPath)!,
                $"{OSPath.GetFileNameWithoutExtension(bndPath)}{suffix}{OSPath.GetExtension(bndPath)}");
            Directory.CreateDirectory(OSPath.GetDirectoryName(destPath)!);
            resultingPaths.Push(destPath);
            File.WriteAllBytes(destPath, bytes);

            bag.TryAdd(file.Name, fileElement);
        }

        if (Configuration.Active.Parallel)
            Parallel.ForEach(bnd.Files, Callback);
        else
        {
            bnd.Files.ForEach(Callback);
        }

        // Spill the bag
        foreach (BinderFile file in bnd.Files)
        {
            files.Add(bag[file.Name]);
        }

        if (Configuration.Active.Recursive)
        {
            ParseMode.ParseFiles(resultingPaths.ToList(), true);
        }

        return files;
    }

    // Recursive repack: Check if any files from the file list are unpacked, and repack them before continuing.
    protected static void RecursiveRepackFile(string path, bool recursive, IEnumerable<WFileParser> parsers)
    {
        if (!Configuration.Active.Recursive) return;
        WFileParser? parser = parsers.FirstOrDefault(p => p.Exists(path) && p.Is(path, null, out ISoulsFile? _));
        if (parser != null)
        {
            var unpackPath = parser.GetUnpackDestPath(path, recursive);
            if (parser.ExistsUnpacked(unpackPath) && parser.IsUnpacked(unpackPath))
            {
                ParseMode.ParseFiles(new[] { unpackPath }, true);
            }
        }
    }

    protected static void RecursiveRepackFile(string path, bool recursive)
    {
        RecursiveRepackFile(path, recursive, ParseMode.GetParsers(true));
    }

    protected static void RecursiveRepackFile(string path, bool recursive, WFileParser parser)
    {
        RecursiveRepackFile(path, recursive, new[] { parser });
    }

    protected static void ReadBinderFiles(IBinder bnd, XElement filesElement, string srcDirPath, string root,
        bool recursive)
    {
        var bag = new ConcurrentDictionary<string, BinderFile>();
        var nameList = filesElement.Elements("file").Select(file => BndPath.Combine(root, file.Element("path")!.Value))
            .ToList();

        void Callback(XElement file)
        {
            if (file.Element("path") == null)
                throw new FriendlyException("File node missing path tag.");


            string strFlags = file.Element("flags")?.Value ?? "Flag1";
            string strId = file.Element("id")?.Value ?? "-1"; // Edge case for PC save files
            string bndPath = file.Element("path")!.Value;
            string suffix = file.Element("suffix")?.Value ?? "";
            string? strCompression = file.Element("compression")?.Value;
            DCX.CompressionInfo compression;
            if (strCompression != null)
            {
                compression = ReadCompressionInfoFromXml(file);
            }
            else
            {
                compression = new DCX.ZlibCompressionInfo();
            }
            string bndName = BndPath.Combine(root, bndPath);

            if (!Enum.TryParse(strFlags, out Binder.FileFlags flags))
                throw new FriendlyException(
                    $"Could not parse file flags: {strFlags}\nFlags must be comma-separated list of flags.");

            if (!int.TryParse(strId, out int id))
                throw new FriendlyException($"Could not parse file ID: {strId}\nID must be a 32-bit signed integer.");

            var path = bndPath.ToOSPath();

            string inPath = OSPath.Combine(srcDirPath, OSPath.GetDirectoryName(path)!,
                $"{OSPath.GetFileNameWithoutExtension(path)}{suffix}{OSPath.GetExtension(path)}");
            if (!File.Exists(inPath))
                throw new FriendlyException($"File not found: {inPath}");

            RecursiveRepackFile(inPath, recursive);

            byte[] bytes = File.ReadAllBytes(inPath);
            bag.TryAdd(bndName, new BinderFile(flags, id, bndName, bytes)
            {
                CompressionInfo = compression
            });
        }

        if (Configuration.Active.Parallel)
            Parallel.ForEach(filesElement.Elements("file"), Callback);
        else
        {
            foreach (XElement element in filesElement.Elements("file"))
            {
                Callback(element);
            }
        }

        // Spill the bag
        foreach (string name in nameList)
        {
            bnd.Files.Add(bag[name]);
        }
    }
}

public class UnsortedFileFormat
{
    public string SearchPattern { get; }
    public Binder.FileFlags FileFlags { get; }

    public DCX.CompressionInfo Compression { get; }

    public UnsortedFileFormat(string pattern, Binder.FileFlags flags, DCX.CompressionInfo? compression = null)
    {
        SearchPattern = pattern;
        FileFlags = flags;
        Compression = compression ?? new DCX.ZlibCompressionInfo();
    }
}

public abstract class WUnsortedBinderParser : WBinderParser
{
    public abstract string Extension { get; }
    public abstract UnsortedFileFormat[] PackedFormats { get; }

    public virtual bool EndsInExtension(string path)
    {
        return path.ToLower().EndsWith($".{Extension.ToLower()}") ||
               path.ToLower().EndsWith($".{Extension.ToLower()}.dcx");
    }

    protected virtual void ReadUnsortedBinderFiles(IBinder bnd, string srcDirPath, string root, bool recursive)
    {
        string searchPattern = string.Join(';', PackedFormats.Select(f => f.SearchPattern));
        var i = 0;
        foreach (string filePath in Directory.GetFiles(srcDirPath, searchPattern, SearchOption.AllDirectories))
        {
            string filename = OSPath.GetFileName(filePath);
            UnsortedFileFormat format = PackedFormats.FirstOrDefault(a =>
                FileSystemName.MatchesWin32Expression(a.SearchPattern.AsSpan(), filename));
            if (format == null)
                throw new InvalidDataException(
                    $"File {filename} passed pattern checks, but was not found among formats.");
            string name = !string.IsNullOrEmpty(root)
                ? BndPath.Combine(root, OSPath.GetRelativePath(srcDirPath, filePath).ToBndPath())
                : filePath;

            RecursiveRepackFile(filePath, recursive);

            byte[] bytes = File.ReadAllBytes(filePath);
            bnd.Files.Add(new BinderFile(format.FileFlags, i, name, bytes)
            {
                CompressionInfo = format.Compression
            });
            i++;
        }
    }
}