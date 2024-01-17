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
                files.Add(bag.OrderBy(el => Convert.ToInt32(el.Element("id")!.Value)));
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

    // Recursive repack: Check if any files from the file list are unpacked, and repack them before continuing.
    protected static void RecursiveRepackFile(string path, IEnumerable<WFileParser> parsers)
    {
        if (!Configuration.Recursive) return;
        WFileParser? parser = parsers.FirstOrDefault(p => p.Exists(path) && p.Is(path, null, out ISoulsFile? _));
        if (parser != null)
        {
            var unpackPath = parser.GetUnpackDestPath(path);
            if (parser.ExistsUnpacked(unpackPath) && parser.IsUnpacked(unpackPath))
            {
                ParseMode.ParseFiles(new[] { unpackPath }, true);
            }
        }
    }

    protected static void RecursiveRepackFile(string path)
    {
        RecursiveRepackFile(path, ParseMode.Parsers);
    }

    protected static void RecursiveRepackFile(string path, WFileParser parser)
    {
        RecursiveRepackFile(path, new[] { parser });
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

            RecursiveRepackFile(inPath);

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

            RecursiveRepackFile(filePath);

            byte[] bytes = File.ReadAllBytes(filePath);
            bnd.Files.Add(new BinderFile(format.FileFlags, i, name, bytes)
            {
                CompressionType = format.Compression
            });
            i++;
        }
    }
}