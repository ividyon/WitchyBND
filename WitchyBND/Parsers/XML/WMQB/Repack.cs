using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Xml;
using System.Xml.Linq;
using SoulsFormats;
using WitchyLib;
using Enum = System.Enum;
namespace WitchyBND.Parsers;

public partial class WMQB
{

    public override void Repack(string srcPath, bool recursive)
    {

        MQB mqb = new MQB();
        XmlDocument xml = new XmlDocument();
        xml.Load(srcPath);

        string name = xml.SelectSingleNode("MQB/Name").InnerText;
        var version = FriendlyParseEnum<MQB.MQBVersion>(nameof(MQB), nameof(MQB.Version), xml.SelectSingleNode("MQB/Version").InnerText);
        float framerate = FriendlyParseFloat32(nameof(MQB), nameof(MQB.Framerate), xml.SelectSingleNode("MQB/Framerate").InnerText);
        bool bigendian = FriendlyParseBool(nameof(MQB), nameof(MQB.BigEndian), xml.SelectSingleNode("MQB/BigEndian").InnerText);
        if (!Enum.TryParse(xml.SelectSingleNode("MQB/Compression")?.InnerText ?? "None", out DCX.Type compression))
            throw new FriendlyException($"{nameof(MQB)} {nameof(MQB.Compression)} could not be parsed.");

        string resDir = xml.SelectSingleNode("MQB/ResourceDirectory").InnerText;
        List<MQB.Resource> resources = new List<MQB.Resource>();
        List<MQB.Cut> cuts = new List<MQB.Cut>();

        var resourcesNode = xml.SelectSingleNode("MQB/Resources");
        foreach (XmlNode resNode in resourcesNode.SelectNodes("Resource"))
            resources.Add(RepackResource(resNode));

        var cutsNode = xml.SelectSingleNode("MQB/Cuts");
        foreach (XmlNode cutNode in cutsNode.SelectNodes("Cut"))
            cuts.Add(RepackCut(cutNode));

        mqb.Name = name;
        mqb.Version = version;
        mqb.Framerate = framerate;
        mqb.BigEndian = bigendian;
        mqb.Compression = compression;
        mqb.ResourceDirectory = resDir;
        mqb.Resources = resources;
        mqb.Cuts = cuts;

        XElement xelem = LoadXml(srcPath);
        string outPath = GetRepackDestPath(srcPath, xelem);
        WBUtil.Backup(outPath);
        mqb.TryWriteSoulsFile(outPath);
    }
    #region RepackHelpers

        public static MQB.Resource RepackResource(XmlNode resNode)
        {
            MQB.Resource resource = new MQB.Resource();

            string name = resNode.SelectSingleNode("Name").InnerText;
            string path = resNode.SelectSingleNode("Path").InnerText;
            int parentIndex = FriendlyParseInt32(nameof(MQB.Resource), nameof(MQB.Resource.ParentIndex), resNode.SelectSingleNode("ParentIndex").InnerText);
            int unk48 = FriendlyParseInt32(nameof(MQB.Resource), nameof(MQB.Resource.Unk48), resNode.SelectSingleNode("Unk48").InnerText);
            List<MQB.CustomData> customData = new List<MQB.CustomData>();

            var resCusDataNode = resNode.SelectSingleNode("Resource_CustomData");
            foreach (XmlNode cusDataNode in resCusDataNode.SelectNodes("CustomData"))
                customData.Add(RepackCustomData(cusDataNode));

            resource.Name = name;
            resource.Path = path;
            resource.ParentIndex = parentIndex;
            resource.Unk48 = unk48;
            resource.CustomData = customData;
            return resource;
        }

        public static MQB.CustomData RepackCustomData(XmlNode customdataNode)
        {
            MQB.CustomData customdata = new MQB.CustomData();

            string name = customdataNode.SelectSingleNode("Name").InnerText;
            var type = FriendlyParseEnum<MQB.CustomData.DataType>(nameof(MQB.CustomData), nameof(MQB.CustomData.Type), customdataNode.SelectSingleNode("Type").InnerText);
            
            int memberCount = FriendlyParseInt32(nameof(MQB.CustomData), nameof(MQB.CustomData.MemberCount), customdataNode.SelectSingleNode("MemberCount").InnerText);
            object value = ConvertValueToDataType(customdataNode.SelectSingleNode("Value").InnerText, type, memberCount);
            var sequences = new List<MQB.CustomData.Sequence>();

            var seqsNode = customdataNode.SelectSingleNode("Sequences");
            foreach (XmlNode seqNode in seqsNode.SelectNodes("Sequence"))
                sequences.Add(RepackSequence(seqNode));

            customdata.Name = name;
            customdata.Type = type;
            customdata.Value = value;
            customdata.MemberCount = memberCount;
            customdata.Sequences = sequences;
            return customdata;
        }

        public static MQB.CustomData.Sequence RepackSequence(XmlNode seqNode)
        {
            MQB.CustomData.Sequence sequence = new MQB.CustomData.Sequence();

            int valueIndex = FriendlyParseInt32(nameof(MQB.CustomData.Sequence), nameof(MQB.CustomData.Sequence.ValueIndex), seqNode.SelectSingleNode("ValueIndex").InnerText);
            var type = FriendlyParseEnum<MQB.CustomData.DataType>(nameof(MQB.CustomData.Sequence), nameof(MQB.CustomData.Sequence.ValueType), seqNode.SelectSingleNode("ValueType").InnerText);
            int pointType = FriendlyParseInt32(nameof(MQB.CustomData.Sequence), nameof(MQB.CustomData.Sequence.PointType), seqNode.SelectSingleNode("PointType").InnerText);
            List<MQB.CustomData.Sequence.Point> points = new List<MQB.CustomData.Sequence.Point>();

            var pointsNode = seqNode.SelectSingleNode("Points");
            foreach (XmlNode pointNode in pointsNode.SelectNodes("Point"))
                points.Add(RepackPoint(pointNode, type));

            sequence.ValueIndex = valueIndex;
            sequence.ValueType = type;
            sequence.PointType = pointType;
            sequence.Points = points;
            return sequence;
        }

        public static MQB.CustomData.Sequence.Point RepackPoint(XmlNode pointNode, MQB.CustomData.DataType type)
        {
            MQB.CustomData.Sequence.Point point = new MQB.CustomData.Sequence.Point();

            string valueStr = pointNode.SelectSingleNode("Value").InnerText;
            object value;

            switch (type)
            {
                case MQB.CustomData.DataType.Byte: value = FriendlyParseByte(nameof(MQB.CustomData.Sequence.Point), nameof(MQB.CustomData.Sequence.Point.Value), valueStr); break;
                case MQB.CustomData.DataType.Float: value = FriendlyParseFloat32(nameof(MQB.CustomData.Sequence.Point), nameof(MQB.CustomData.Sequence.Point.Value), valueStr); break;
                default: throw new NotSupportedException($"Unsupported sequence point value type: {type}");
            }

            int unk08 = FriendlyParseInt32(nameof(MQB.CustomData.Sequence.Point), nameof(MQB.CustomData.Sequence.Point.Unk08), pointNode.SelectSingleNode("Unk08").InnerText);
            float unk10 = FriendlyParseFloat32(nameof(MQB.CustomData.Sequence.Point), nameof(MQB.CustomData.Sequence.Point.Unk10), pointNode.SelectSingleNode("Unk10").InnerText);
            float unk14 = FriendlyParseFloat32(nameof(MQB.CustomData.Sequence.Point), nameof(MQB.CustomData.Sequence.Point.Unk14), pointNode.SelectSingleNode("Unk14").InnerText);

            point.Value = value;
            point.Unk08 = unk08;
            point.Unk10 = unk10;
            point.Unk14 = unk14;
            return point;
        }

        public static MQB.Cut RepackCut(XmlNode cutNode)
        {
            MQB.Cut cut = new MQB.Cut();

            string name = cutNode.SelectSingleNode("Name").InnerText;
            int duration = FriendlyParseInt32("Cut", nameof(cut.Duration), cutNode.SelectSingleNode("Duration").InnerText);
            int unk44 = FriendlyParseInt32("Cut", nameof(cut.Unk44), cutNode.SelectSingleNode("Unk44").InnerText);
            List<MQB.Timeline> timelines = new List<MQB.Timeline>();

            var timelinesNode = cutNode.SelectSingleNode("Timelines");
            foreach (XmlNode timelineNode in timelinesNode.SelectNodes("Timeline"))
                timelines.Add(RepackTimeline(timelineNode));

            cut.Name = name;
            cut.Duration = duration;
            cut.Unk44 = unk44;
            cut.Timelines = timelines;
            return cut;
        }

        public static MQB.Timeline RepackTimeline(XmlNode timelineNode)
        {
            MQB.Timeline timeline = new MQB.Timeline();

            int unk10 = FriendlyParseInt32(nameof(MQB.Timeline), nameof(MQB.Timeline.Unk10), timelineNode.SelectSingleNode("Unk10").InnerText);
            List<MQB.Disposition> dispositions = new List<MQB.Disposition>();
            List<MQB.CustomData> customdata = new List<MQB.CustomData>();

            var dispositionsNode = timelineNode.SelectSingleNode("Dispositions");
            foreach (XmlNode disNode in dispositionsNode.SelectNodes("Disposition"))
                dispositions.Add(RepackDisposition(disNode));

            var timelineCusDataNode = timelineNode.SelectSingleNode("Timeline_CustomData");
            foreach (XmlNode cusDataNode in timelineCusDataNode.SelectNodes("CustomData"))
                customdata.Add(RepackCustomData(cusDataNode));

            timeline.Unk10 = unk10;
            timeline.Dispositions = dispositions;
            timeline.CustomData = customdata;
            return timeline;
        }

        public static MQB.Disposition RepackDisposition(XmlNode disNode)
        {
            MQB.Disposition disposition = new MQB.Disposition();

            int id = FriendlyParseInt32(nameof(MQB.Disposition), nameof(MQB.Disposition.ID), disNode.SelectSingleNode("ID").InnerText);
            int duration = FriendlyParseInt32(nameof(MQB.Disposition), nameof(MQB.Disposition.Duration), disNode.SelectSingleNode("Duration").InnerText);
            int resIndex = FriendlyParseInt32(nameof(MQB.Disposition), nameof(MQB.Disposition.ResourceIndex), disNode.SelectSingleNode("ResourceIndex").InnerText);
            int startFrame = FriendlyParseInt32(nameof(MQB.Disposition), nameof(MQB.Disposition.StartFrame), disNode.SelectSingleNode("StartFrame").InnerText);
            int unk08 = FriendlyParseInt32(nameof(MQB.Disposition), nameof(MQB.Disposition.Unk08), disNode.SelectSingleNode("Unk08").InnerText);
            int unk14 = FriendlyParseInt32(nameof(MQB.Disposition), nameof(MQB.Disposition.Unk14), disNode.SelectSingleNode("Unk14").InnerText);
            int unk18 = FriendlyParseInt32(nameof(MQB.Disposition), nameof(MQB.Disposition.Unk18), disNode.SelectSingleNode("Unk18").InnerText);
            int unk1C = FriendlyParseInt32(nameof(MQB.Disposition), nameof(MQB.Disposition.Unk1C), disNode.SelectSingleNode("Unk1C").InnerText);
            int unk20 = FriendlyParseInt32(nameof(MQB.Disposition), nameof(MQB.Disposition.Unk20), disNode.SelectSingleNode("Unk20").InnerText);
            int unk28 = FriendlyParseInt32(nameof(MQB.Disposition), nameof(MQB.Disposition.Unk28), disNode.SelectSingleNode("Unk28").InnerText);
            List<MQB.Transform> transforms = new List<MQB.Transform>();
            List<MQB.CustomData> customdata = new List<MQB.CustomData>();

            var transformsNode = disNode.SelectSingleNode("Transforms");
            foreach (XmlNode transformNode in transformsNode.SelectNodes("Transform"))
                transforms.Add(RepackTransform(transformNode));

            var disCusDataNode = disNode.SelectSingleNode("Disposition_CustomData");
            foreach (XmlNode cusDataNode in disCusDataNode.SelectNodes("CustomData"))
                customdata.Add(RepackCustomData(cusDataNode));

            disposition.ID = id;
            disposition.Duration = duration;
            disposition.ResourceIndex = resIndex;
            disposition.StartFrame = startFrame;
            disposition.Unk08 = unk08;
            disposition.Unk14 = unk14;
            disposition.Unk18 = unk18;
            disposition.Unk1C = unk1C;
            disposition.Unk20 = unk20;
            disposition.Unk28 = unk28;
            disposition.Transforms = transforms;
            disposition.CustomData = customdata;
            return disposition;
        }

        public static MQB.Transform RepackTransform(XmlNode transNode)
        {
            MQB.Transform transform = new MQB.Transform();

            float frame = FriendlyParseFloat32(nameof(MQB.Transform), nameof(MQB.Transform.Frame), transNode.SelectSingleNode("Frame").InnerText);
            Vector3 translation = FriendlyParseVector3(nameof(MQB.Transform), nameof(MQB.Transform.Translation), transNode.SelectSingleNode("Translation").InnerText);
            Vector3 rotation = FriendlyParseVector3(nameof(MQB.Transform), nameof(MQB.Transform.Rotation), transNode.SelectSingleNode("Rotation").InnerText);
            Vector3 scale = FriendlyParseVector3(nameof(MQB.Transform), nameof(MQB.Transform.Scale), transNode.SelectSingleNode("Scale").InnerText);
            Vector3 unk10 = FriendlyParseVector3(nameof(MQB.Transform), nameof(MQB.Transform.Unk10), transNode.SelectSingleNode("Unk10").InnerText);
            Vector3 unk1C = FriendlyParseVector3(nameof(MQB.Transform), nameof(MQB.Transform.Unk1C), transNode.SelectSingleNode("Unk1C").InnerText);
            Vector3 unk34 = FriendlyParseVector3(nameof(MQB.Transform), nameof(MQB.Transform.Unk34), transNode.SelectSingleNode("Unk34").InnerText);
            Vector3 unk40 = FriendlyParseVector3(nameof(MQB.Transform), nameof(MQB.Transform.Unk40), transNode.SelectSingleNode("Unk40").InnerText);
            Vector3 unk58 = FriendlyParseVector3(nameof(MQB.Transform), nameof(MQB.Transform.Unk58), transNode.SelectSingleNode("Unk58").InnerText);
            Vector3 unk64 = FriendlyParseVector3(nameof(MQB.Transform), nameof(MQB.Transform.Unk64), transNode.SelectSingleNode("Unk64").InnerText);

            transform.Frame = frame;
            transform.Translation = translation;
            transform.Rotation = rotation;
            transform.Scale = scale;
            transform.Unk10 = unk10;
            transform.Unk1C = unk1C;
            transform.Unk34 = unk34;
            transform.Unk40 = unk40;
            transform.Unk58 = unk58;
            transform.Unk64 = unk64;
            return transform;
        }

        public static object ConvertValueToDataType(string str, MQB.CustomData.DataType type, int memberCount)
        {
            try
            {
                object value = str;
                switch (type)
                {
                    case MQB.CustomData.DataType.Bool: return Convert.ToBoolean(value);
                    case MQB.CustomData.DataType.SByte: return Convert.ToSByte(value);
                    case MQB.CustomData.DataType.Byte: return Convert.ToByte(value);
                    case MQB.CustomData.DataType.Short: return Convert.ToInt16(value);
                    case MQB.CustomData.DataType.Int: return Convert.ToInt32(value);
                    case MQB.CustomData.DataType.UInt: return Convert.ToUInt32(value);
                    case MQB.CustomData.DataType.Float: return Convert.ToSingle(value);
                    case MQB.CustomData.DataType.String: return str;
                    case MQB.CustomData.DataType.Custom: return str.FriendlyHexToByteArray();
                    case MQB.CustomData.DataType.Color: return ConvertValueToColor(value);
                    case MQB.CustomData.DataType.IntColor: return ConvertValueToColor(value);
                    case MQB.CustomData.DataType.Vector:
                        switch (memberCount)
                        {
                            case 2: return str.ToVector2();
                            case 3: return str.ToVector3();
                            case 4: return str.ToVector4();
                            default: throw new NotSupportedException($"{nameof(MQB.CustomData.MemberCount)} {memberCount} not supported for: {nameof(MQB.CustomData.DataType.Vector)}");
                        }
                    default: throw new NotImplementedException($"Unimplemented custom {nameof(MQB.CustomData.DataType)}: {type}");
                }
            }
            catch
            {
                throw new FriendlyException($"The value \"{str}\" could not be converted to the type {type} during custom data repacking.");
            }
        }

        public static object ConvertValueToColor(object value)
        {
            string invalidChars = ",] ";
            string str = value.ToString();
            string alphaStr = str.Substring(str.IndexOf("A=") + 2, 3).TrimEnd(invalidChars.ToCharArray());
            string redStr = str.Substring(str.IndexOf("R=") + 2, 3).TrimEnd(invalidChars.ToCharArray());
            string greenStr = str.Substring(str.IndexOf("G=") + 2, 3).TrimEnd(invalidChars.ToCharArray());
            string blueStr = str.Substring(str.IndexOf("B=") + 2, 3).TrimEnd(invalidChars.ToCharArray());

            int alpha = int.Parse(alphaStr);
            int red = int.Parse(redStr);
            int green = int.Parse(greenStr);
            int blue = int.Parse(blueStr);
            return Color.FromArgb(alpha, red, green, blue);
        }

        public static byte FriendlyParseByte(string parentName, string valueName, string value)
        {
            try
            {
                return byte.Parse(value);
            }
            catch (FormatException)
            {
                throw new FriendlyException($"In a {parentName} {valueName} is a number, but its value \"{value}\" could not be read as a number.");
            }
            catch (OverflowException)
            {
                throw new FriendlyException($"In a {parentName} {valueName} with value \"{value}\" caused an overflow error, it may be too large or too small.");
            }
            catch (ArgumentNullException)
            {
                throw new FriendlyException($"In a {parentName} {valueName} had a null value, make sure it exists.");
            }
            catch (Exception ex)
            {
                throw new FriendlyException($"In a {parentName} {valueName} had an unknown error occur; Exception message: {ex.Message}");
            }
        }

        public static bool FriendlyParseBool(string parentName, string valueName, string value)
        {
            try
            {
                return bool.Parse(value);
            }
            catch (FormatException)
            {
                throw new FriendlyException($"In a {parentName} {valueName} is a True or False, but its value \"{value}\" could not be read as a True or False.");
            }
            catch (ArgumentNullException)
            {
                throw new FriendlyException($"In a {parentName} {valueName} had a null value, make sure it exists.");
            }
            catch (Exception ex)
            {
                throw new FriendlyException($"In a {parentName} {valueName} had an unknown error occur; Exception message: {ex.Message}");
            }
        }

        public static int FriendlyParseInt32(string parentName, string valueName, string value)
        {
            try
            {
                return int.Parse(value);
            }
            catch (FormatException)
            {
                throw new FriendlyException($"In a {parentName} {valueName} is a number, but its value \"{value}\" could not be read as a number.");
            }
            catch (OverflowException)
            {
                throw new FriendlyException($"In a {parentName} {valueName} with value \"{value}\" caused an overflow error, it may be too large or too small.");
            }
            catch (ArgumentNullException)
            {
                throw new FriendlyException($"In a {parentName} {valueName} had a null value, make sure it exists.");
            }
            catch (Exception ex)
            {
                throw new FriendlyException($"In a {parentName} {valueName} had an unknown error occur; Exception message: {ex.Message}");
            }
        }

        public static float FriendlyParseFloat32(string parentName, string valueName, string value)
        {
            try
            {
                return float.Parse(value);
            }
            catch (FormatException)
            {
                throw new FriendlyException($"In a {parentName} {valueName} is a number, but its value \"{value}\" could not be read as a number.");
            }
            catch (OverflowException)
            {
                throw new FriendlyException($"In a {parentName} {valueName} with value \"{value}\" caused an overflow error, it may be too large or too small.");
            }
            catch (ArgumentNullException)
            {
                throw new FriendlyException($"In a {parentName} {valueName} had a null value, make sure it exists.");
            }
            catch (Exception ex)
            {
                throw new FriendlyException($"In a {parentName} {valueName} had an unknown error occur; Exception message: {ex.Message}");
            }
        }

        public static Vector3 FriendlyParseVector3(string parentName, string valueName, string value)
        {
            try
            {
                return value.ToVector3();
            }
            catch (FormatException)
            {
                throw new FriendlyException($"In a {parentName} {valueName} is a vector, but its value \"{value}\" could not be parsed as one. Example how format should be: 1 1 1");
            }
            catch (OverflowException)
            {
                throw new FriendlyException($"In a {parentName} {valueName} with value \"{value}\" caused an overflow error, it may be too large or too small.");
            }
            catch (ArgumentNullException)
            {
                throw new FriendlyException($"In a {parentName} {valueName} had a null value, make sure it exists.");
            }
            catch (Exception ex)
            {
                throw new FriendlyException($"In a {parentName} {valueName} had an unknown error occur; Exception message: {ex.Message}");
            }
        }

        public static TEnum FriendlyParseEnum<TEnum>(string parentName, string valueName, string value) where TEnum : Enum
        {
            try
            {
                return (TEnum)Enum.Parse(typeof(TEnum), value);
            }
            catch (OverflowException)
            {
                throw new FriendlyException($"In a {parentName} {valueName} with value \"{value}\" caused an overflow error, it may be too large or too small.");
            }
            catch (ArgumentNullException)
            {
                throw new FriendlyException($"In a {parentName} {valueName} had a null value, make sure it exists.");
            }
            catch (Exception ex)
            {
                throw new FriendlyException($"In a {parentName} {valueName} had an unknown error occur; Exception message: {ex.Message}");
            }
        }

        #endregion
}