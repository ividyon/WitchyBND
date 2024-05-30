using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using PPlus;
using SoulsFormats;
using WitchyBND.Errors;
using WitchyBND.Services;
using WitchyFormats.Utils;
using WitchyLib;
using TPF = WitchyFormats.TPF;

namespace WitchyBND.Parsers;

public class WTPF : WFolderParser
{
    public override string Name => "TPF";

    static List<TPF.TPFPlatform> supportedPlatforms =
        new() { TPF.TPFPlatform.PC, TPF.TPFPlatform.PS3, TPF.TPFPlatform.PS4 };

    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        return IsRead<TPF>(path, data, out file);
    }

    public override void Unpack(string srcPath, ISoulsFile? file)
    {
        var tpf = (file as TPF)!;
        var destDir = GetUnpackDestPath(srcPath);
        var sourceName = Path.GetFileName(srcPath);
        if (!supportedPlatforms.Contains(tpf.Platform))
        {
            errorService.RegisterError(new WitchyError(
                "WitchyBND currently only supports unpacking PC, PS3 and PS4 TPFs. There may be issues with other console TPFs.",
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

            try
            {
                File.WriteAllBytes($"{destDir}\\{WBUtil.SanitizeFilename(texture.Name)}.dds", texture.Headerize());
            }
            catch (EndOfStreamException)
            {
                File.WriteAllBytes($"{destDir}\\{WBUtil.SanitizeFilename(texture.Name)}.dds",
                    SecretHeaderizer.SecretHeaderize(texture));
            }

            textures.Add(texElement);
        }

        var filename = new XElement("filename", sourceName);
        var xml = new XElement(XmlTag,
            filename,
            new XElement("compression", tpf.Compression.ToString()),
            new XElement("encoding", $"0x{tpf.Encoding:X2}"),
            new XElement("flag2", $"0x{tpf.Flag2:X2}"),
            new XElement("platform", tpf.Platform.ToString()),
            textures
        );

        if (Version > 0) xml.SetAttributeValue(VersionAttributeName, Version.ToString());

        if (!string.IsNullOrEmpty(Configuration.Args.Location))
            filename.AddAfterSelf(new XElement("sourcePath", Path.GetFullPath(Path.GetDirectoryName(srcPath))));

        using var xw = XmlWriter.Create(GetFolderXmlPath(destDir), new XmlWriterSettings
        {
            Indent = true
        });
        xml.WriteTo(xw);
        xw.Close();
    }

    public override void Repack(string srcPath)
    {
        TPF tpf = new TPF();
        // XmlDocument xml = new XmlDocument();

        // xml.Load(GetBinderXmlPath(srcPath));
        var doc = XDocument.Load(GetFolderXmlPath(srcPath));
        if (doc.Root == null) throw new XmlException("XML has no root");
        XElement xml = doc.Root;

        Enum.TryParse(xml.Element("platform")?.Value ?? "None", out TPF.TPFPlatform platform);
        tpf.Platform = platform;

        Enum.TryParse(xml.Element("compression")?.Value ?? "None", out DCX.Type compression);
        tpf.Compression = compression;

        tpf.Encoding = Convert.ToByte(xml.Element("encoding").Value, 16);
        tpf.Flag2 = Convert.ToByte(xml.Element("flag2").Value, 16);

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

            byte[] bytes = File.ReadAllBytes($"{srcPath}\\{outName}.dds");
            var texture = new TPF.Texture(inName, format, flags1, bytes);
            texture.FloatStruct = floatStruct;
            tpf.Textures.Add(texture);
        }

        string outPath = GetRepackDestPath(srcPath, xml);
        WBUtil.Backup(outPath);

        WarnAboutKrak(compression, tpf.Textures.Count);
        try
        {
            tpf.TryWriteSoulsFile(outPath);
        }
        catch (Exception e) when (e is not NoOodleFoundException)
        {
            if (platform == TPF.TPFPlatform.PC) throw;
            errorService.RegisterError(new WitchyError(
                "WitchyBND only officially supports repacking PC TPFs at the moment. Repacking console TPFs is not supported.",
                srcPath));
        }
    }
}