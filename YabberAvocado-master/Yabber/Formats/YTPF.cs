using SoulsFormats;
using System;
using System.IO;
using System.Xml;

namespace Yabber
{
    static class YTPF
    {
        public static void Unpack(this TPF tpf, string sourceName, string targetDir, IProgress<float> progress)
        {
            if (Directory.Exists(targetDir)) YBUtil.Backup(targetDir);

#if !DEBUG
            if (tpf.Platform != TPF.TPFPlatform.PC)
                throw new NotSupportedException("Yabber does not support console TPFs at the moment.");
#endif

            Directory.CreateDirectory(targetDir);
            var xws = new XmlWriterSettings();
            xws.Indent = true;

            using (var xw = XmlWriter.Create($"{targetDir}\\_yabber-tpf.xml", xws))
            {
                xw.WriteStartElement("tpf");

                xw.WriteElementString("filename", sourceName);
                xw.WriteElementString("compression", tpf.Compression.ToString());
                xw.WriteElementString("encoding", $"0x{tpf.Encoding:X2}");
                xw.WriteElementString("flag2", $"0x{tpf.Flag2:X2}");

                xw.WriteStartElement("textures");
                for (int i = 0; i < tpf.Textures.Count; i++)
                {
                    TPF.Texture texture = tpf.Textures[i];
                    xw.WriteStartElement("texture");
                    xw.WriteElementString("name", texture.Name + ".dds");
                    xw.WriteElementString("format", texture.Format.ToString());
                    xw.WriteElementString("flags1", $"0x{texture.Flags1:X2}");

                    if (texture.FloatStruct != null)
                    {
                        xw.WriteStartElement("FloatStruct");
                        xw.WriteAttributeString("Unk00", texture.FloatStruct.Unk00.ToString());
                        foreach (float value in texture.FloatStruct.Values)
                        {
                            xw.WriteElementString("Value", value.ToString());
                        }
                        xw.WriteEndElement();
                    }
                    xw.WriteEndElement();

                    File.WriteAllBytes($"{targetDir}\\{texture.Name}.dds", texture.Headerize());
                    progress.Report((float)i / tpf.Textures.Count);
                }
                xw.WriteEndElement();

                xw.WriteEndElement();
                xw.Close();
            }
        }

        public static void Repack(string sourceDir, string targetDir)
        {
            TPF tpf = new TPF();
            XmlDocument xml = new XmlDocument();
            xml.Load($"{sourceDir}\\_yabber-tpf.xml");

            string filename = xml.SelectSingleNode("tpf/filename").InnerText;
            Enum.TryParse(xml.SelectSingleNode("tpf/compression")?.InnerText ?? "None", out DCX.Type cmpr);
            tpf.Compression = cmpr;
            tpf.Encoding = Convert.ToByte(xml.SelectSingleNode("tpf/encoding").InnerText, 16);
            tpf.Flag2 = Convert.ToByte(xml.SelectSingleNode("tpf/flag2").InnerText, 16);

            foreach (XmlNode texNode in xml.SelectNodes("tpf/textures/texture"))
            {
                string name = Path.GetFileNameWithoutExtension(texNode.SelectSingleNode("name").InnerText);
                byte format = Convert.ToByte(texNode.SelectSingleNode("format").InnerText);
                byte flags1 = Convert.ToByte(texNode.SelectSingleNode("flags1").InnerText, 16);

                TPF.FloatStruct floatStruct = null;
                XmlNode floatsNode = texNode.SelectSingleNode("FloatStruct");
                if (floatsNode != null)
                {
                    floatStruct = new TPF.FloatStruct();
                    floatStruct.Unk00 = int.Parse(floatsNode.Attributes["Unk00"].InnerText);
                    foreach (XmlNode valueNode in floatsNode.SelectNodes("Value"))
                        floatStruct.Values.Add(float.Parse(valueNode.InnerText));
                }

                byte[] bytes = File.ReadAllBytes($"{sourceDir}\\{name}.dds");
                var texture = new TPF.Texture(name, format, flags1, bytes);
                texture.FloatStruct = floatStruct;
                tpf.Textures.Add(texture);
            }

            string outPath = $"{targetDir}\\{filename}";

            if (File.Exists(outPath)) YBUtil.Backup(outPath);

            tpf.Write(outPath);
        }
    }
}
