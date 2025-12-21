using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using SoulsFormats;
using WitchyBND.CliModes;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WANIBND4 : WBinderParser
{
    public override string Name => "ANIBND4";
    public override string XmlTag => "anibnd4";

    public string[] ProcessedExtensions =
    [
        ".hkx",
        ".tae",
        ".nsa",
        ".mba",
        ".asa",
        ".qsa",
        ".nmb"
    ];

    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        file = null;
        path = path.ToLower();
        return Configuration.Active.Bnd &&
               path.Contains(".anibnd") && IsRead<BND4>(path, data, out file);
    }

    public override bool? IsSimple(string path)
    {
        return null;
    }

    public override string GetUnpackDestPath(string srcPath, bool recursive)
    {
        return $"{base.GetUnpackDestPath(srcPath, recursive)}-wanibnd";
    }

    public override void Unpack(string srcPath, ISoulsFile? file, bool recursive)
    {
        BND4 bnd = (file as BND4)!;
        var destDir = GetUnpackDestPath(srcPath, recursive);
        Directory.CreateDirectory(destDir);

        var root = "";
        if (Binder.HasNames(bnd.Format))
        {
            root = WBUtil.FindCommonBndRootPath(bnd.Files.Select(bndFile => bndFile.Name));
        }

        var xml = PrepareXmlManifest(srcPath, recursive, false, bnd.Compression, out XDocument xDoc, root);
        xml.Add(
            new XElement("version", bnd.Version),
            new XElement("format", bnd.Format.ToString()),
            new XElement("bigendian", bnd.BigEndian.ToString()),
            new XElement("bitbigendian", bnd.BitBigEndian.ToString()),
            new XElement("unicode", bnd.Unicode.ToString()),
            new XElement("extended", $"0x{bnd.Extended:X2}"),
            new XElement("unk04", bnd.Unk04.ToString()),
            new XElement("unk05", bnd.Unk05.ToString())
        );

        if (!bnd.Files.Any())
        {
            WriteXmlManifest(xDoc, srcPath, recursive);
            return;
        }

        var newFiles = new ConcurrentBag<BinderFile>();
        var resultingPaths = new ConcurrentStack<string>();

        void Callback(BinderFile bndFile)
        {
            var ext = Path.GetExtension(bndFile.Name);
            if (!ProcessedExtensions.Contains(ext) ||
                bndFile.ID >= 7000000 && bndFile.ID <= 7999999 || // BB behaviors
                (bndFile.Name.EndsWith(".hkx") && bndFile.Name.ToLower().Contains("skeleton")) ||
                (bndFile.Name.EndsWith(".tae") && Path.GetFileName(bndFile.Name).StartsWith("c"))
               )
            {
                newFiles.Add(bndFile);
                return;
            }

            byte[] bytes = bndFile.Bytes;
            var path = WBUtil.UnrootBNDPath(bndFile.Name, root);
            path = path.Replace('\\', Path.DirectorySeparatorChar);
            var destPath = Path.Combine(destDir, path);
            if (!Directory.Exists(Path.GetDirectoryName(destPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            File.WriteAllBytes(destPath, bytes);
            resultingPaths.Push(destPath);
        }

        if (Configuration.Active.Parallel)
            Parallel.ForEach(bnd.Files, Callback);
        else
            bnd.Files.ForEach(Callback);

        // Remaining files
        bnd.Files = newFiles.OrderBy(a => a.ID).ToList();

        XElement files = WriteBinderFiles(bnd, destDir, root);
        xml.Add(files);
        WriteXmlManifest(xDoc, srcPath, recursive);

        if (Configuration.Active.Recursive)
        {
            ParseMode.ParseFiles(resultingPaths.ToList(), true);
        }
    }

    public override void Repack(string srcPath, bool recursive)
    {
        var bnd = new BND4();

        XElement xml = LoadXml(GetFolderXmlPath(srcPath));

        string root = xml.Element("root")?.Value ?? "";

        bnd.Compression = ReadCompressionInfoFromXml(xml);

        bnd.Version = xml.Element("version")!.Value;
        bnd.Format = (Binder.Format)Enum.Parse(typeof(Binder.Format), xml.Element("format")!.Value);
        bnd.BigEndian = bool.Parse(xml.Element("bigendian")!.Value);
        bnd.BitBigEndian = bool.Parse(xml.Element("bitbigendian")!.Value);
        bnd.Unicode = bool.Parse(xml.Element("unicode")!.Value);
        bnd.Extended = Convert.ToByte(xml.Element("extended")!.Value, 16);
        bnd.Unk04 = bool.Parse(xml.Element("unk04")!.Value);
        bnd.Unk05 = bool.Parse(xml.Element("unk05")!.Value);

        var filesElement = xml.Element("files");
        if (filesElement != null)
            ReadBinderFiles(bnd, filesElement, srcPath, root, recursive);
        var pathsToSkip = filesElement != null
            ? filesElement.Elements("file").Select(file => Path.Combine(root, file.Element("path")!.Value)).ToList()
            : new List<string>();

        ConcurrentBag<BinderFile> fileBag = new();

        void FileCallback(string ext, string filePath)
        {
            var pathDir = filePath.Substring(srcPath.Length + 1);
            var binderPath = Path.Combine(root, pathDir);
            if (pathsToSkip.Contains(binderPath)) return;
            RecursiveRepackFile(filePath, recursive);

            string fileName = Path.GetFileNameWithoutExtension(filePath);
            var regex = Regex.Match(pathDir.ToLower(), "c[1-9][0-9][0-9][0-9]");
            bool isEnemy = regex.Success;
            bool isPlayer = pathDir.ToLower().Contains("c0000");
            if (WBUtil.MorphemeExtensions.Contains(ext))
            {
                fileBag.Add(new BinderFile(Binder.FileFlags.Flag1, -1, binderPath, File.ReadAllBytes(filePath)));
            }
            else if (ext == ".hkx")
            {
                int baseHkxId = 1000000000;
                if (fileName.ToLower().Contains("skeleton"))
                {
                    fileBag.Add(new BinderFile(Binder.FileFlags.Flag1, isEnemy ? 4000000 : 1000000, binderPath,
                        File.ReadAllBytes(filePath)));
                }
                else
                {
                    int fileId;

                    try
                    {
                        var rawSplit = fileName.Split("_");
                        var split = rawSplit.Select(a => int.Parse(new string(a.Where(c => char.IsDigit(c)).ToArray()))).ToArray();
                        bool ffx = rawSplit.Length == 3 && rawSplit[0].Length == 6 && rawSplit[0].First() == 's';
                        int taeId;
                        int animId;
                        if (ffx)
                        {
                            taeId = split[1];
                            animId = split[2];
                        }
                        else
                        {
                            taeId = split[0];
                            animId = split[1];
                        }

                        fileId = baseHkxId + (1000000 * taeId) + animId;
                    }
                    catch (Exception)
                    {
                        errorService.RegisterError($"Skipping HKX with unrecognized file name pattern: {pathDir}\nPlease correct any erroneous file names or, if this is an operation performed on an unmodified vanilla file, please report this as a bug.");
                        return;
                    }

                    fileBag.Add(new BinderFile(Binder.FileFlags.Flag1, fileId, binderPath,
                        File.ReadAllBytes(filePath)));
                }
            }
            else if (ext == ".tae")
            {
                var split = fileName.Split("_");
                bool namedByTaeId = split.Length == 1 && split[0].First() == 'a';
                if (namedByTaeId)
                {
                    int taeId;
                    try
                    {
                        taeId = int.Parse(fileName.Substring(1));
                    }
                    catch (Exception)
                    {
                        errorService.RegisterError($"Skipping TAE with unrecognized file name pattern: {pathDir}\nPlease correct any erroneous file names or, if this is an operation performed on an unmodified vanilla file, please report this as a bug.");
                        return;
                    }

                    taeId = 5000000 + taeId;
                    fileBag.Add(new BinderFile(Binder.FileFlags.Flag1, taeId, binderPath,
                        File.ReadAllBytes(filePath)));
                }
                else
                {
                    fileBag.Add(new BinderFile(Binder.FileFlags.Flag1, 3000000, binderPath,
                        File.ReadAllBytes(filePath)));
                }
            }
            else
            {
                throw new InvalidDataException($"Unrecognized extension {ext}");
            }
        }

        void ExtensionCallback(string ext)
        {
            var files = Directory.EnumerateFiles(srcPath, $"*{ext}", SearchOption.AllDirectories).ToList();
            var dupes = files.GroupBy(f => Path.GetFileName(f)).Where(g => g.Count() > 1).ToList();
            if (dupes.Any())
            {
                throw new DuplicateNameException(@$"Found the following duplicate files across different folders:

{String.Join("\n", dupes.SelectMany(g => g.ToList()).Select(f => $"- {f}"))}

Please address this issue before trying again.");
            }
            if (Configuration.Active.Parallel)
                Parallel.ForEach(files,
                    filePath => FileCallback(ext, filePath));
            else
            {
                foreach (string filePath in files)
                {
                    FileCallback(ext, filePath);
                }
            }
        }

        if (Configuration.Active.Parallel)
            Parallel.ForEach(ProcessedExtensions, ExtensionCallback);
        else
        {
            foreach (string ext in ProcessedExtensions)
            {
                ExtensionCallback(ext);
            }
        }

        bnd.Files = bnd.Files.Union(fileBag.ToList()).ToList();
        var takenIds = bnd.Files.Select(a => a.ID).ToList();

        // Properly add unsorted files
        foreach (BinderFile unsortedFile in bnd.Files.Where(a => a.ID == -1))
        {
            int firstAvailable = takenIds.NextAvailable()!.Value;
            unsortedFile.ID = firstAvailable;
            takenIds.Add(firstAvailable);
        }

        bnd.Files = bnd.Files.OrderBy(a => a.ID).ToList();

        var destPath = GetRepackDestPath(srcPath, xml);

        Backup(destPath);

        WarnAboutKrak(bnd.Compression, bnd.Files.Count);

        bnd.Write(destPath);
    }
}