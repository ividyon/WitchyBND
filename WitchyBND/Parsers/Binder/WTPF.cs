using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using SoulsFormats;
using WitchyBND.Errors;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WTPF : WFolderParser
{
    public override string Name => "TPF";

    public override int Version => WBUtil.WitchyVersionToInt("2.17.0.0");


    private static readonly List<TPF.TPFPlatform> UnpackPlatforms = new()
    {
        TPF.TPFPlatform.PC,
        TPF.TPFPlatform.Xbox360,
        TPF.TPFPlatform.PS3,
        TPF.TPFPlatform.PS4,
        TPF.TPFPlatform.PS5
    };

    private static readonly List<TPF.TPFPlatform> RepackPlatforms = new()
    {
        TPF.TPFPlatform.PC,
        TPF.TPFPlatform.PS3,
        TPF.TPFPlatform.PS4
    };

    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        return IsRead<TPF>(path, data, out file);
    }

    public override bool? IsSimple(string path)
    {
        string filename = Path.GetFileName(path).ToLower();
        return filename.EndsWith(".tpf") || filename.EndsWith(".tpf.dcx");
    }

    public override void Unpack(string srcPath, ISoulsFile? file, bool recursive)
    {
        var tpf = (file as TPF)!;
        var destDir = GetUnpackDestPath(srcPath, recursive);

        if (!UnpackPlatforms.Contains(tpf.Platform))
        {
            errorService.RegisterError(new WitchyError(
                $"WitchyBND currently only supports unpacking TPFs for the following platforms: {string.Join(", ", UnpackPlatforms)}. The selected TPF is {tpf.Platform}. Expect issues to occur during the process.",
                srcPath));
        }

        Directory.CreateDirectory(destDir);

        var textures = new XElement("textures");

        foreach (TPF.Texture texture in tpf.Textures)
        {
            var texElement = new XElement("texture");
            texElement.WriteSanitizedBinderFilePath(texture.Name + ".dds", "name");
            texElement.Add(new XElement("format", texture.Format.ToString()),
                new XElement("flags1", $"0x{texture.Flags1:X2}"));

            if (tpf.Platform == TPF.TPFPlatform.PS4)
                texElement.Add(new XElement("unk2", $"0x{texture.Header.Unk2:X2}"));

            if (texture.FloatStruct != null)
            {
                var floatStruct = new XElement("FloatStruct");
                floatStruct.SetAttributeValue("Unk00", texture.FloatStruct.Unk00.ToString());
                foreach (float value in texture.FloatStruct.Values)
                {
                    floatStruct.Add(new XElement("Value", value.ToString()));
                }

                texElement.Add(floatStruct);
            }

            File.WriteAllBytes(Path.Combine(destDir, $"{WBUtil.SanitizeFilename(texture.Name)}.dds"), texture.Headerize());
            textures.Add(texElement);
        }

        var xml = PrepareXmlManifest(srcPath, recursive, false, tpf.Compression, out XDocument xDoc, null);
        xml.Add(
            new XElement("encoding", $"0x{tpf.Encoding:X2}"),
            new XElement("flag2", $"0x{tpf.Flag2:X2}"),
            new XElement("platform", tpf.Platform.ToString()),
            textures
        );
        
        WriteXmlManifest(xDoc, srcPath, recursive);
    }

    public override void Repack(string srcPath, bool recursive)
    {
        TPF tpf = new TPF();
        
        var doc = XDocument.Load(GetFolderXmlPath(srcPath));
        if (doc.Root == null) throw new XmlException("XML has no root");
        XElement xml = doc.Root;

        Enum.TryParse(xml.Element("platform")?.Value ?? "None", out TPF.TPFPlatform platform);
        tpf.Platform = platform;


        if (!RepackPlatforms.Contains(platform))
        {
            errorService.RegisterError(new WitchyError(
                $"WitchyBND currently only supports repacking TPFs for the following platforms: {string.Join(", ", RepackPlatforms)}. The selected TPF is {platform}. Expect issues to occur during the process.",
                srcPath));
        }

        tpf.Compression = ReadCompressionInfoFromXml(xml);

        tpf.Encoding = Convert.ToByte(xml.Element("encoding")!.Value, 16);
        tpf.Flag2 = Convert.ToByte(xml.Element("flag2")!.Value, 16);

        foreach (XElement texNode in xml.Element("textures")!.Elements("texture"))
        {
            string inName = Path.GetFileNameWithoutExtension(texNode.GetSanitizedBinderFilePath("name"));
            string outName = Path.GetFileNameWithoutExtension(texNode.GetSanitizedBinderFilePath("name", true));
            byte format = Convert.ToByte(texNode.Element("format")!.Value);
            byte flags1 = Convert.ToByte(texNode.Element("flags1")!.Value, 16);

            TPF.FloatStruct floatStruct = null;
            XElement floatsNode = texNode.Element("FloatStruct");
            if (floatsNode != null)
            {
                floatStruct = new TPF.FloatStruct();
                floatStruct.Unk00 = int.Parse(floatsNode.Attribute("Unk00").Value);
                foreach (XElement valueNode in floatsNode.Elements("Value"))
                    floatStruct.Values.Add(float.Parse(valueNode.Value));
            }

            byte[] bytes = File.ReadAllBytes(Path.Combine(srcPath, $"{outName}.dds"));
            var texture = new TPF.Texture(inName, format, flags1, bytes, platform);
            if (platform == TPF.TPFPlatform.PS4)
            {
                texture.Header.Unk2 = Convert.ToInt32(texNode.Element("unk2")!.Value, 16);
            }
            texture.FloatStruct = floatStruct;
            tpf.Textures.Add(texture);
        }

        string outPath = GetRepackDestPath(srcPath, xml);
        Backup(outPath);

        WarnAboutKrak(tpf.Compression, tpf.Textures.Count);
        tpf.Write(outPath);
    }
}