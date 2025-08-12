using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Xml.Linq;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WGPARAM : WXMLParser
{
    public override string Name => "GPARAM";

    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        return IsRead<GPARAM>(path, data, out file);
    }

    public override bool? IsSimple(string path)
    {
        string filename = Path.GetFileName(path).ToLower();
        return filename.EndsWith(".gparam") || filename.EndsWith(".gparam.dcx");
    }

    public override void Unpack(string srcPath, ISoulsFile? file, bool recursive)
    {
        GPARAM gparam = (file as GPARAM)!;
        var xml = PrepareXmlManifest(srcPath, recursive, false, gparam.Compression, out XDocument xDoc, null);

        xml.Add(new XElement("game", gparam.Game.ToString()));
        xml.Add(new XElement("unk0D", gparam.Unk0D.ToString()));
        xml.Add(new XElement("unk14", gparam.Unk14.ToString()));
        xml.Add(new XElement("unk40", gparam.Unk40.ToString(CultureInfo.InvariantCulture)));
        if (gparam.Game == GPARAM.GPGame.Sekiro)
            xml.Add(new XElement("unk50", gparam.Unk50.ToString(CultureInfo.InvariantCulture)));

        var groupsXml = new XElement("groups");
        xml.Add(groupsXml);
        foreach (GPARAM.Group group in gparam.Groups)
        {
            var groupXml = new XElement("group");
            groupsXml.Add(groupXml);
            groupXml.Add(new XAttribute("name1", group.Name1));
            if (gparam.Game == GPARAM.GPGame.DarkSouls3 || gparam.Game == GPARAM.GPGame.Sekiro)
            {
                groupXml.Add(new XAttribute("name2", group.Name2));
                var commentsXml = new XElement("comments");
                groupXml.Add(commentsXml);
                foreach (string comment in group.Comments)
                    commentsXml.Add(new XElement("comment", comment));
            }

            foreach (GPARAM.Param param in group.Params)
            {
                var paramXml = new XElement("param");
                groupXml.Add(paramXml);
                paramXml.Add(new XAttribute("name1", param.Name1));
                if (gparam.Game == GPARAM.GPGame.DarkSouls3 || gparam.Game == GPARAM.GPGame.Sekiro)
                    paramXml.Add(new XAttribute("name2", param.Name2));
                paramXml.Add(new XAttribute("type", param.Type.ToString()));
                for (int index = 0; index < param.Values.Count; ++index)
                {
                    var valueXml = new XElement("value");
                    paramXml.Add(valueXml);
                    valueXml.Add(new XAttribute("id", param.ValueIDs[index].ToString()));
                    
                    if (gparam.Game == GPARAM.GPGame.Sekiro)
                        valueXml.Add(new XAttribute("timeOfDay", param.TimeOfDay[index].ToString(CultureInfo.InvariantCulture)));
                    
                    if (param.Type == GPARAM.ParamType.Float2)
                    {
                        Vector2 vector2 = (Vector2)param.Values[index];
                        valueXml.Value = $"{vector2.X} {vector2.Y}";
                    }
                    else if (param.Type == GPARAM.ParamType.Float3)
                    {
                        Vector3 vector3 = (Vector3)param.Values[index];
                        valueXml.Value = $"{vector3.X} {vector3.Y} {vector3.Z}";
                    }
                    else if (param.Type == GPARAM.ParamType.Float4)
                    {
                        Vector4 vector4 = (Vector4)param.Values[index];
                        valueXml.Value = $"{vector4.X} {vector4.Y} {vector4.Z} {vector4.W}";
                    }
                    else if (param.Type == GPARAM.ParamType.Byte4)
                    {
                        byte[] numArray = (byte[])param.Values[index];
                        valueXml.Value = $"{numArray[0]:X2} {numArray[1]:X2} {numArray[2]:X2} {numArray[3]:X2}";
                    }
                    else
                        valueXml.Value = param.Values[index].ToString();
                }
            }
        }

        var unk3sXml = new XElement("unk3s");
        xml.Add(unk3sXml);
        foreach (GPARAM.Unk3 unk3 in gparam.Unk3s)
        {
            var unk3Xml = new XElement("unk3");
            unk3sXml.Add(unk3Xml);
            unk3Xml.Add(new XAttribute("group_index", unk3.GroupIndex.ToString()));
            if (gparam.Game == GPARAM.GPGame.Sekiro)
                unk3Xml.Add(new XAttribute("unk0C", unk3.Unk0C.ToString()));
            foreach (int valueId in unk3.ValueIDs)
                unk3Xml.Add(new XElement("value_id", valueId.ToString()));
        }
        
        xml.Add(new XElement("unk_block_2", string.Join(" ", gparam.UnkBlock2.Select((Func<byte, string>)(b => b.ToString("X2"))))));
        
        WriteXmlManifest(xDoc, srcPath, recursive);
    }

    public override void Repack(string srcPath, bool recursive)
    {
        GPARAM gparam = new GPARAM();
        XElement xml = LoadXml(srcPath);
        gparam.Compression = ReadCompressionInfoFromXml(xml);
        Enum.TryParse(xml.Element("game")!.Value, out gparam.Game);
        gparam.Unk0D = bool.Parse(xml.Element("unk0D")!.Value);
        gparam.Unk14 = int.Parse(xml.Element("unk14")!.Value);
        gparam.Unk40 = float.Parse(xml.Element("unk40")!.Value);
        if (gparam.Game == GPARAM.GPGame.Sekiro)
            gparam.Unk50 = float.Parse(xml.Element("unk50")!.Value);
        foreach (XElement selectNode1 in xml.Element("groups")!.Elements("group"))
        {
            string value1 = selectNode1.Attribute("name1")!.Value;
            GPARAM.Group group;
            if (gparam.Game == GPARAM.GPGame.DarkSouls2)
            {
                group = new GPARAM.Group(value1, null);
            }
            else
            {
                string value2 = selectNode1.Attribute("name2")!.Value;
                group = new GPARAM.Group(value1, value2);
                foreach (XElement selectNode2 in selectNode1.Element("comments")!.Elements("comment"))
                    group.Comments.Add(selectNode2.Value);
            }

            foreach (XElement selectNode3 in selectNode1.Elements("param"))
            {
                string value3 = selectNode3.Attribute("name1")!.Value;
                GPARAM.ParamType type =
                    (GPARAM.ParamType)Enum.Parse(typeof(GPARAM.ParamType), selectNode3.Attribute("type")!.Value);
                GPARAM.Param obj;
                if (gparam.Game == GPARAM.GPGame.DarkSouls2)
                {
                    obj = new GPARAM.Param(value3, null, type);
                }
                else
                {
                    string Value4 = selectNode3.Attribute("name2")!.Value;
                    obj = new GPARAM.Param(value3, Value4, type);
                    if (gparam.Game == GPARAM.GPGame.Sekiro)
                        obj.TimeOfDay = new List<float>();
                }

                foreach (XElement selectNode4 in selectNode3.Elements("value"))
                {
                    obj.ValueIDs.Add(int.Parse(selectNode4.Attribute("id")!.Value));
                    if (gparam.Game == GPARAM.GPGame.Sekiro)
                        obj.TimeOfDay.Add(float.Parse(selectNode4.Attribute("timeOfDay")!.Value));
                    switch (obj.Type)
                    {
                        case GPARAM.ParamType.Byte:
                            obj.Values.Add(byte.Parse(selectNode4.Value));
                            continue;
                        case GPARAM.ParamType.Short:
                            obj.Values.Add(short.Parse(selectNode4.Value));
                            continue;
                        case GPARAM.ParamType.IntA:
                            obj.Values.Add(int.Parse(selectNode4.Value));
                            continue;
                        case GPARAM.ParamType.BoolA:
                            obj.Values.Add(bool.Parse(selectNode4.Value));
                            continue;
                        case GPARAM.ParamType.IntB:
                            obj.Values.Add(int.Parse(selectNode4.Value));
                            continue;
                        case GPARAM.ParamType.Float:
                            obj.Values.Add(float.Parse(selectNode4.Value));
                            continue;
                        case GPARAM.ParamType.BoolB:
                            obj.Values.Add(bool.Parse(selectNode4.Value));
                            continue;
                        case GPARAM.ParamType.Float2:
                            float[] array1 = selectNode4.Value.Split(' ')
                                .Select((Func<string, float>)(f => float.Parse(f))).ToArray();
                            obj.Values.Add(new Vector2(array1[0], array1[1]));
                            continue;
                        case GPARAM.ParamType.Float3:
                            float[] array2 = selectNode4.Value.Split(' ')
                                .Select((Func<string, float>)(f => float.Parse(f))).ToArray();
                            obj.Values.Add(new Vector3(array2[0], array2[1], array2[1]));
                            continue;
                        case GPARAM.ParamType.Float4:
                            float[] array3 = selectNode4.Value.Split(' ')
                                .Select((Func<string, float>)(f => float.Parse(f))).ToArray();
                            obj.Values.Add(new Vector4(array3[0], array3[1], array3[2], array3[3]));
                            continue;
                        case GPARAM.ParamType.Byte4:
                            byte[] array4 = selectNode4.Value.Split(' ')
                                .Select((Func<string, byte>)(b => byte.Parse(b, NumberStyles.AllowHexSpecifier)))
                                .ToArray();
                            obj.Values.Add(array4);
                            continue;
                        default:
                            continue;
                    }
                }

                group.Params.Add(obj);
            }

            gparam.Groups.Add(group);
        }

        foreach (XElement selectNode5 in xml.Element("unk3s")!.Elements("unk3"))
        {
            GPARAM.Unk3 unk3 = new GPARAM.Unk3(int.Parse(selectNode5.Attribute("group_index")!.Value));
            if (gparam.Game == GPARAM.GPGame.Sekiro)
                unk3.Unk0C = int.Parse(selectNode5.Attribute("unk0C")!.Value);
            foreach (XElement selectNode6 in selectNode5.Elements("value_id"))
                unk3.ValueIDs.Add(int.Parse(selectNode6.Value));
            gparam.Unk3s.Add(unk3);
        }

        gparam.UnkBlock2 = xml.Element("unk_block_2")!.Value.Split(new char[1]
        {
            ' '
        }, StringSplitOptions.RemoveEmptyEntries).Select((Func<string, byte>)(s => Convert.ToByte(s, 16))).ToArray();

        string path = GetRepackDestPath(srcPath, xml);
        if (File.Exists(path))
            Backup(path);
        gparam.TryWriteSoulsFile(path);
    }
}