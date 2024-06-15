using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using SoulsFormats;
using WitchyBND.CliModes;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WFFXBNDModern : WBinderParser
{

    public override string Name => "Modern FFXBND";
    public override string XmlTag => "ffxbnd";

    public override int Version => WBUtil.WitchyVersionToInt("2.8.0.0");

    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        file = null;
        path = path.ToLower();
        return Configuration.Active.Bnd &&
               path.Contains(".ffxbnd") && IsRead<BND4>(path, data, out file);
    }

    public override void Unpack(string srcPath, ISoulsFile? file)
    {
        BND4 bnd = (file as BND4)!;
        var destDir = GetUnpackDestPath(srcPath);
        var srcName = Path.GetFileName(srcPath);
        Directory.CreateDirectory(destDir);

        var filename = new XElement("filename", srcName);
        var rootFile = bnd.Files.FirstOrDefault(f => f.Name.Contains("\\sfx\\"));
        if (rootFile == null)
            throw new Exception("FFXBND has invalid structure; expected \\sfx\\ path.");
        var rootPath = rootFile.Name.Substring(0, rootFile.Name.IndexOf("\\sfx\\", StringComparison.Ordinal) + 5);
        var xml = new XElement("ffxbnd",
            filename,
            new XElement("compression", bnd.Compression.ToString()),
            new XElement("version", bnd.Version),
            new XElement("format", bnd.Format.ToString()),
            new XElement("bigendian", bnd.BigEndian.ToString()),
            new XElement("bitbigendian", bnd.BitBigEndian.ToString()),
            new XElement("unicode", bnd.Unicode.ToString()),
            new XElement("extended", $"0x{bnd.Extended:X2}"),
            new XElement("unk04", bnd.Unk04.ToString()),
            new XElement("unk05", bnd.Unk05.ToString()),
            new XElement("root", rootPath)
        );

        if (Version > 0) xml.SetAttributeValue(VersionAttributeName, Version.ToString());

        if (!string.IsNullOrEmpty(Configuration.Active.Location))
            filename.AddAfterSelf(new XElement("sourcePath", Path.GetFullPath(Path.GetDirectoryName(srcPath))));

        // Files
        var firstEffect = bnd.Files.FirstOrDefault(f => f.Name.EndsWith(".fxr"));
        var effectDir = firstEffect != null ? new DirectoryInfo(Path.GetDirectoryName(firstEffect.Name)!).Name : "effect";
        if (firstEffect != null) xml.Add(new XElement("effectDir", effectDir));
        var effectTargetDir = $@"{destDir}\{effectDir}";

        var firstTexture = bnd.Files.FirstOrDefault(f => f.Name.EndsWith(".tpf"));
        var textureDir = firstTexture != null ? new DirectoryInfo(Path.GetDirectoryName(firstTexture.Name)!).Name : "texture";
        if (firstTexture != null) xml.Add(new XElement("textureDir", textureDir));
        var textureTargetDir = $@"{destDir}\{textureDir}";

        var firstModel = bnd.Files.FirstOrDefault(f => f.Name.EndsWith(".flver"));
        var modelDir = firstModel != null ? new DirectoryInfo(Path.GetDirectoryName(firstModel.Name)!).Name : "model";
        if (firstModel != null) xml.Add(new XElement("modelDir", modelDir));
        var modelTargetDir = $@"{destDir}\{modelDir}";

        var firstAnim = bnd.Files.FirstOrDefault(f => f.Name.EndsWith(".anibnd"));
        var animDir = firstAnim != null ? new DirectoryInfo(Path.GetDirectoryName(firstAnim.Name)!).Name : "animation";
        if (firstAnim != null) xml.Add(new XElement("animDir", animDir));
        var animTargetDir = $@"{destDir}\{animDir}";

        var firstRes = bnd.Files.FirstOrDefault(f => f.Name.EndsWith(".ffxreslist"));
        var resDir = firstRes != null ? new DirectoryInfo(Path.GetDirectoryName(firstRes.Name)!).Name : "resource";
        if (firstRes != null) xml.Add(new XElement("resDir", resDir));
        var resTargetDir = $@"{destDir}\{resDir}";

        using var xw = XmlWriter.Create($"{destDir}\\{GetFolderXmlFilename()}", new XmlWriterSettings
        {
            Indent = true,
        });
        xml.WriteTo(xw);
        xw.Close();

        void Callback(BinderFile bndFile)
        {
            byte[] bytes = bndFile.Bytes;
            string fileTargetDir;
            string fileTargetName = Path.GetFileName(bndFile.Name);
            switch (Path.GetExtension(bndFile.Name))
            {
                case ".fxr":
                    fileTargetDir = effectTargetDir;
                    break;
                case ".tpf":
                    var tpf = TPF.Read(bytes);
                    bytes = tpf.Textures[0].Bytes;
                    fileTargetName = $"{Path.GetFileName(tpf.Textures[0].Name)}.dds";
                    fileTargetDir = textureTargetDir;
                    break;
                case ".flver":
                    fileTargetDir = modelTargetDir;
                    break;
                case ".anibnd":
                    fileTargetDir = animTargetDir;
                    break;
                case ".ffxreslist":
                    fileTargetDir = resTargetDir;
                    break;
                default:
                    fileTargetDir = $@"{destDir}\other";
                    break;
            }
            if (!Directory.Exists(fileTargetDir))
                Directory.CreateDirectory(fileTargetDir);
            File.WriteAllBytes($"{fileTargetDir}\\{fileTargetName}", bytes);
        }

        if (Configuration.Active.Parallel)
            Parallel.ForEach(bnd.Files, Callback);
        else
            bnd.Files.ForEach(Callback);
    }

    public override void Repack(string srcPath)
    {
        var bnd = new BND4();

        XElement xml = LoadXml(GetFolderXmlPath(srcPath));

        DCX.Type compression = Enum.Parse<DCX.Type>(xml.Element("compression")?.Value ?? "None");
        bnd.Compression = compression;

        bnd.Version = xml.Element("version")!.Value;
        bnd.Format = (Binder.Format)Enum.Parse(typeof(Binder.Format), xml.Element("format")!.Value);
        bnd.BigEndian = bool.Parse(xml.Element("bigendian")!.Value);
        bnd.BitBigEndian = bool.Parse(xml.Element("bitbigendian")!.Value);
        bnd.Unicode = bool.Parse(xml.Element("unicode")!.Value);
        bnd.Extended = Convert.ToByte(xml.Element("extended")!.Value, 16);
        bnd.Unk04 = bool.Parse(xml.Element("unk04")!.Value);
        bnd.Unk05 = bool.Parse(xml.Element("unk05")!.Value);

        // Files

        List<string> effectPaths = Directory.GetFiles(srcPath, "*.fxr", SearchOption.AllDirectories).OrderBy(Path.GetFileName).ToList();
        List<string> texturePaths = Directory.GetFiles(srcPath, "*.dds", SearchOption.AllDirectories).OrderBy(Path.GetFileName).ToList();
        List<string> modelPaths = Directory.GetFiles(srcPath, "*.flver", SearchOption.AllDirectories).OrderBy(Path.GetFileName).ToList();
        List<string> animPaths = Directory.GetFiles(srcPath, "*.anibnd", SearchOption.AllDirectories).OrderBy(Path.GetFileName).ToList();
        List<string> resPaths = Directory.GetFiles(srcPath, "*.ffxreslist", SearchOption.AllDirectories).OrderBy(Path.GetFileName).ToList();

        // Sanity check fxr and reslist
        // Every FXR must have a matching reslist with the exact same name and vice versa
        // therefore there must also be the same amount of FXRs as reslists
        List<string> missingReslists = new();
        if (effectPaths.Count > 0 && resPaths.Count > 0)
        {
            var effectNames = new SortedSet<string>(effectPaths.Select(p => Path.GetFileNameWithoutExtension(p)));
            var resNames = new SortedSet<string>(resPaths.Select(p => Path.GetFileNameWithoutExtension(p)));
            if (!effectNames.SetEquals(resNames))
            {
                missingReslists = effectNames.Except(resNames).ToList();
                string[] diff2 = resNames.Except(effectNames).ToArray();
                if (missingReslists.Any())
                {
                    errorService.RegisterNotice(@$"Following FXRs are missing reslists:

{string.Join("\n", missingReslists)}

WitchyBND will create empty reslist files to compensate.");
                }
                if (diff2.Any())
                {
                    errorService.RegisterNotice(@$"Following reslists are missing FXRs:

{string.Join("\n", diff2)}

Consider tidying up the unpacked archive folder.");
                }
            }
        }

        // Write files

        ConcurrentBag<BinderFile> bag = new();

        string rootPath = xml.Element("root")!.Value;

        WFileParser effectParser = ParseMode.Parsers.OfType<WFXR3>().First();

        void effectCallback()
        {
            if (!effectPaths.Any()) return;

            var dir = xml.Element("effectDir")!.Value;
            string basePath = Path.Combine(rootPath, dir);

            if (Configuration.Active.Parallel)
                Parallel.ForEach(effectPaths, inEffectCallback);
            else
                effectPaths.ForEach(inEffectCallback);
            return;

            void inEffectCallback(string path)
            {
                var i = effectPaths!.IndexOf(path);

                var filePath = effectPaths[i];
                var fileName = Path.GetFileName(filePath);
                RecursiveRepackFile(filePath, effectParser);
                var bytes = File.ReadAllBytes(filePath);
                var file = new BinderFile(Binder.FileFlags.Flag1, i, Path.Combine(basePath, fileName),
                    bytes);
                bag.Add(file);
            }
        }

        void textureCallback()
        {
            if (!texturePaths.Any()) return;

            var dir = xml.Element("textureDir")!.Value;
            string basePath = Path.Combine(rootPath, dir);

            if (Configuration.Active.Parallel)
                Parallel.ForEach(texturePaths, inTextureCallback);
            else
                texturePaths.ForEach(inTextureCallback);
            return;

            void inTextureCallback(string path)
            {
                var i = texturePaths!.IndexOf(path);

                var filePath = texturePaths[i];
                var fileName = Path.GetFileName(filePath);
                var bytes = File.ReadAllBytes(filePath);
                var tpf = new TPF();

                tpf.Compression = DCX.Type.None;
                tpf.Encoding = 0x01;
                tpf.Flag2 = 0x03;
                tpf.Platform = TPF.TPFPlatform.PC;

                var tex = new TPF.Texture();
                tex.Name = Path.GetFileNameWithoutExtension(fileName).Trim();
                tex.Format = 0;
                if (fileName.ToLower().EndsWith("_m"))
                {
                    tex.Format = 103;
                }
                else if (fileName.ToLower().EndsWith("_n"))
                {
                    tex.Format = 106;
                }
                tex.Bytes = bytes;

                tpf.Textures.Add(tex);

                bytes = tpf.Write();

                var file = new BinderFile(Binder.FileFlags.Flag1, 100000 + i, Path.Combine(basePath, Path.ChangeExtension(fileName, "tpf")),
                    bytes);
                bag.Add(file);
            }
        }

        void modelCallback()
        {
            if (!modelPaths.Any()) return;

            var dir = xml.Element("modelDir")!.Value;
            string basePath = Path.Combine(rootPath, dir);

            if (Configuration.Active.Parallel)
                Parallel.ForEach(modelPaths, inModelCallback);
            else
                modelPaths.ForEach(inModelCallback);
            return;

            void inModelCallback(string path)
            {
                var i = modelPaths!.IndexOf(path);

                var filePath = modelPaths[i];
                var fileName = Path.GetFileName(filePath);
                var bytes = File.ReadAllBytes(filePath);
                var file = new BinderFile(Binder.FileFlags.Flag1, 200000 + i, Path.Combine(basePath, fileName),
                    bytes);
                bag.Add(file);
            }
        }

        void animCallback()
        {
            if (!animPaths.Any()) return;

            var dir = xml.Element("animDir")!.Value;
            string basePath = Path.Combine(rootPath, dir);

            if (Configuration.Active.Parallel)
                Parallel.ForEach(animPaths, inAnimCallback);
            else
                animPaths.ForEach(inAnimCallback);
            return;

            void inAnimCallback(string path)
            {
                var i = animPaths!.IndexOf(path);

                var filePath = animPaths[i];
                var fileName = Path.GetFileName(filePath);
                var bytes = File.ReadAllBytes(filePath);
                var file = new BinderFile(Binder.FileFlags.Flag1, 300000 + i, Path.Combine(basePath, fileName),
                    bytes);
                bag.Add(file);
            }
        }

        void resCallback()
        {
            if (!resPaths.Any()) return;

            var dir = xml.Element("resDir")!.Value;
            string basePath = Path.Combine(rootPath, dir);

            if (Configuration.Active.Parallel)
                Parallel.ForEach(resPaths, inResCallback);
            else
                resPaths.ForEach(inResCallback);
            return;

            void inResCallback(string path)
            {
                var i = resPaths!.IndexOf(path);

                var filePath = resPaths[i];
                var fileName = Path.GetFileName(filePath);
                var bytes = File.ReadAllBytes(filePath);
                var file = new BinderFile(Binder.FileFlags.Flag1, 400000 + i, Path.Combine(basePath, fileName),
                    bytes);
                bag.Add(file);
            }
        }

        void missingResCallback()
        {
            if (!missingReslists.Any()) return;

            var dir = xml.Element("resDir")!.Value;
            string basePath = Path.Combine(rootPath, dir);

            if (Configuration.Active.Parallel)
                Parallel.ForEach(missingReslists, inMissingResCallback);
            else
                missingReslists.ForEach(inMissingResCallback);
            return;

            void inMissingResCallback(string effectName)
            {
                var fileName = $"{effectName}.ffxreslist";
                var effect = effectPaths.First(f => f.ToLower().EndsWith($"{effectName.ToLower()}.fxr"));
                var file = new BinderFile(Binder.FileFlags.Flag1, 400000 + effectPaths.IndexOf(effect), Path.Combine(basePath, fileName), Array.Empty<byte>());
                bag.Add(file);
            }
        }

        if (Configuration.Active.Parallel)
        {
            Parallel.Invoke(effectCallback, textureCallback, modelCallback, animCallback, resCallback, missingResCallback);
        }
        else
        {
            effectCallback();
            textureCallback();
            modelCallback();
            animCallback();
            resCallback();
            missingResCallback();
        }

        bnd.Files = bag.OrderBy(f => f.ID).ToList();

        string destPath = GetRepackDestPath(srcPath, xml);
        WBUtil.Backup(destPath);

        WarnAboutKrak(compression, bnd.Files.Count);

        bnd.Write(destPath);
    }

    public override string GetUnpackDestPath(string srcPath)
    {
        string sourceDir = new FileInfo(srcPath).Directory?.FullName;
        string fileName = Path.GetFileName(srcPath);
        return $"{sourceDir}\\{fileName.Replace('.', '-')}-wffxbnd";
    }
}