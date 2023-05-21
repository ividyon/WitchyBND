using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Xml;
using System.IO;

namespace Yabber
{
    static class YGPARAM
    {
        public static void Unpack(this GPARAM gparam, string sourceFile)
        {
            string targetFile = $"{sourceFile}.xml";

            if (File.Exists(targetFile)) YBUtil.Backup(targetFile);

            var xws = new XmlWriterSettings();
            xws.Indent = true;

            using (XmlWriter xw = XmlWriter.Create(targetFile, xws))
            {
                xw.WriteStartElement("gparam");
                xw.WriteElementString("compression", gparam.Compression.ToString());
                xw.WriteElementString("game", gparam.Game.ToString());
                xw.WriteElementString("unk0D", gparam.Unk0D.ToString());
                xw.WriteElementString("unk14", gparam.Unk14.ToString());
                xw.WriteElementString("unk40", gparam.Unk40.ToString());
                if (gparam.Game == GPARAM.GPGame.Sekiro)
                    xw.WriteElementString("unk50", gparam.Unk50.ToString());

                xw.WriteStartElement("groups");
                foreach (GPARAM.Group group in gparam.Groups)
                {
                    xw.WriteStartElement("group");
                    xw.WriteAttributeString("name1", group.Name1);
                    if (gparam.Game == GPARAM.GPGame.DarkSouls3 || gparam.Game == GPARAM.GPGame.Sekiro)
                    {
                        xw.WriteAttributeString("name2", group.Name2);

                        xw.WriteStartElement("comments");
                        foreach (string comment in group.Comments)
                            xw.WriteElementString("comment", comment);
                        xw.WriteEndElement();
                    }

                    foreach (GPARAM.Param param in group.Params)
                    {
                        xw.WriteStartElement("param");
                        xw.WriteAttributeString("name1", param.Name1);
                        if (gparam.Game == GPARAM.GPGame.DarkSouls3 || gparam.Game == GPARAM.GPGame.Sekiro)
                            xw.WriteAttributeString("name2", param.Name2);

                        xw.WriteAttributeString("type", param.Type.ToString());
                        for (var i = 0; i < param.Values.Count; i++)
                        {
                            xw.WriteStartElement("value");
                            xw.WriteAttributeString("id", param.ValueIDs[i].ToString());
                            if (gparam.Game == GPARAM.GPGame.Sekiro)
                                xw.WriteAttributeString("timeOfDay", param.TimeOfDay[i].ToString());

                            if (param.Type == GPARAM.ParamType.Float2)
                            {
                                var value = (Vector2)param.Values[i];
                                xw.WriteString($"{value.X} {value.Y}");
                            }
                            else if (param.Type == GPARAM.ParamType.Float3)
                            {
                                var value = (Vector3)param.Values[i];
                                xw.WriteString($"{value.X} {value.Y} {value.Z}");
                            }
                            else if (param.Type == GPARAM.ParamType.Float4)
                            {
                                var value = (Vector4)param.Values[i];
                                xw.WriteString($"{value.X} {value.Y} {value.Z} {value.W}");
                            }
                            else if (param.Type == GPARAM.ParamType.Byte4)
                            {
                                var value = (byte[])param.Values[i];
                                xw.WriteString($"{value[0]:X2} {value[1]:X2} {value[2]:X2} {value[3]:X2}");
                            }
                            else
                            {
                                xw.WriteString(param.Values[i].ToString());
                            }
                            xw.WriteEndElement();
                        }
                        xw.WriteEndElement();
                    }
                    xw.WriteEndElement();
                }
                xw.WriteEndElement();

                xw.WriteStartElement("unk3s");
                foreach (var unk3 in gparam.Unk3s)
                {
                    xw.WriteStartElement("unk3");
                    xw.WriteAttributeString("group_index", unk3.GroupIndex.ToString());
                    if (gparam.Game == GPARAM.GPGame.Sekiro)
                        xw.WriteAttributeString("unk0C", unk3.Unk0C.ToString());
                    foreach (var id in unk3.ValueIDs)
                    {
                        xw.WriteElementString("value_id", id.ToString());
                    }
                    xw.WriteEndElement();
                }
                xw.WriteEndElement();

                xw.WriteElementString("unk_block_2", string.Join(" ", gparam.UnkBlock2.Select(b => b.ToString("X2"))));

                xw.WriteEndElement();
                xw.Close();
            }
        }

        public static void Repack(string sourceFile)
        {
            var gparam = new GPARAM();
            var xml = new XmlDocument();
            xml.Load(sourceFile);
            Enum.TryParse(xml.SelectSingleNode("gparam/compression")?.InnerText ?? "None", out DCX.Type cmpr);
            gparam.Compression = cmpr;
            Enum.TryParse(xml.SelectSingleNode("gparam/game").InnerText, out gparam.Game);
            gparam.Unk0D = bool.Parse(xml.SelectSingleNode("gparam/unk0D").InnerText);
            gparam.Unk14 = int.Parse(xml.SelectSingleNode("gparam/unk14").InnerText);
            gparam.Unk40 = float.Parse(xml.SelectSingleNode("gparam/unk40").InnerText);
            if (gparam.Game == GPARAM.GPGame.Sekiro)
                gparam.Unk50 = float.Parse(xml.SelectSingleNode("gparam/unk50").InnerText);

            foreach (XmlNode groupNode in xml.SelectNodes("gparam/groups/group"))
            {
                string groupName1 = groupNode.Attributes["name1"].InnerText;
                GPARAM.Group group;
                if (gparam.Game == GPARAM.GPGame.DarkSouls2)
                {
                    group = new GPARAM.Group(groupName1, null);
                }
                else
                {
                    string groupName2 = groupNode.Attributes["name2"].InnerText;
                    group = new GPARAM.Group(groupName1, groupName2);
                    foreach (XmlNode commentNode in groupNode.SelectNodes("comments/comment"))
                        group.Comments.Add(commentNode.InnerText);
                }

                foreach (XmlNode paramNode in groupNode.SelectNodes("param"))
                {
                    string paramName1 = paramNode.Attributes["name1"].InnerText;
                    var paramType = (GPARAM.ParamType)Enum.Parse(typeof(GPARAM.ParamType), paramNode.Attributes["type"].InnerText);
                    GPARAM.Param param;

                    if (gparam.Game == GPARAM.GPGame.DarkSouls2)
                    {
                        param = new GPARAM.Param(paramName1, null, paramType);
                    }
                    else
                    {
                        string paramName2 = paramNode.Attributes["name2"].InnerText;
                        param = new GPARAM.Param(paramName1, paramName2, paramType);
                        if (gparam.Game == GPARAM.GPGame.Sekiro)
                            param.TimeOfDay = new List<float>();
                    }

                    foreach (XmlNode value in paramNode.SelectNodes("value"))
                    {
                        param.ValueIDs.Add(int.Parse(value.Attributes["id"].InnerText));
                        if (gparam.Game == GPARAM.GPGame.Sekiro)
                            param.TimeOfDay.Add(float.Parse(value.Attributes["timeOfDay"].InnerText));

                        switch (param.Type)
                        {
                            case GPARAM.ParamType.BoolA:
                                param.Values.Add(bool.Parse(value.InnerText));
                                break;

                            case GPARAM.ParamType.BoolB:
                                param.Values.Add(bool.Parse(value.InnerText));
                                break;

                            case GPARAM.ParamType.Byte:
                                param.Values.Add(byte.Parse(value.InnerText));
                                break;

                            case GPARAM.ParamType.Byte4:
                                byte[] bytes = value.InnerText.Split(' ').Select(b => byte.Parse(b, NumberStyles.AllowHexSpecifier)).ToArray();
                                param.Values.Add(bytes);
                                break;

                            case GPARAM.ParamType.Float:
                                param.Values.Add(float.Parse(value.InnerText));
                                break;

                            case GPARAM.ParamType.Float2:
                                float[] vec2 = value.InnerText.Split(' ').Select(f => float.Parse(f)).ToArray();
                                param.Values.Add(new Vector2(vec2[0], vec2[1]));
                                break;

                            case GPARAM.ParamType.Float3:
                                float[] vec3 = value.InnerText.Split(' ').Select(f => float.Parse(f)).ToArray();
                                param.Values.Add(new Vector3(vec3[0], vec3[1], vec3[1]));
                                break;

                            case GPARAM.ParamType.Float4:
                                float[] vec4 = value.InnerText.Split(' ').Select(f => float.Parse(f)).ToArray();
                                param.Values.Add(new Vector4(vec4[0], vec4[1], vec4[2], vec4[3]));
                                break;

                            case GPARAM.ParamType.IntA:
                                param.Values.Add(int.Parse(value.InnerText));
                                break;

                            case GPARAM.ParamType.IntB:
                                param.Values.Add(int.Parse(value.InnerText));
                                break;

                            case GPARAM.ParamType.Short:
                                param.Values.Add(short.Parse(value.InnerText));
                                break;
                        }
                    }
                    group.Params.Add(param);
                }
                gparam.Groups.Add(group);
            }

            foreach (XmlNode unk3Node in xml.SelectNodes("gparam/unk3s/unk3"))
            {
                int groupIndex = int.Parse(unk3Node.Attributes["group_index"].InnerText);
                var unk3 = new GPARAM.Unk3(groupIndex);
                if (gparam.Game == GPARAM.GPGame.Sekiro)
                    unk3.Unk0C = int.Parse(unk3Node.Attributes["unk0C"].InnerText);
                foreach (XmlNode value in unk3Node.SelectNodes("value_id"))
                {
                    unk3.ValueIDs.Add(int.Parse(value.InnerText));
                }
                gparam.Unk3s.Add(unk3);
            }

            gparam.UnkBlock2 = xml.SelectSingleNode("gparam/unk_block_2").InnerText
                .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => Convert.ToByte(s, 16)).ToArray();

            string outPath;
            if (sourceFile.EndsWith(".gparam.xml"))
                outPath = sourceFile.Replace(".gparam.xml", ".gparam");
            else if (sourceFile.EndsWith(".gparam.dcx.xml"))
                outPath = sourceFile.Replace(".gparam.dcx.xml", ".gparam.dcx");
            else if (sourceFile.EndsWith(".fltparam.xml"))
                outPath = sourceFile.Replace(".fltparam.xml", ".fltparam");
            else if (sourceFile.EndsWith(".fltparam.dcx.xml"))
                outPath = sourceFile.Replace(".fltparam.dcx.xml", ".fltparam.dcx");
            else
                throw new InvalidOperationException("Invalid GPARAM xml filename.");

            if (File.Exists(outPath)) YBUtil.Backup(outPath);

            gparam.Write(outPath);
        }
    }
}
