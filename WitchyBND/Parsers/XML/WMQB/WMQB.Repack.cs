﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Text;
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
        XElement xml = LoadXml(srcPath);

        string name = xml.Element("Name")!.Value;
        var version = FriendlyParseEnum<MQB.MQBVersion>(nameof(MQB), nameof(MQB.Version), xml.Element("MQBVersion")!.Value);
        float framerate = FriendlyParseFloat32(nameof(MQB), nameof(MQB.Framerate), xml.Element("Framerate")!.Value);
        bool bigendian = FriendlyParseBool(nameof(MQB), nameof(MQB.BigEndian), xml.Element("BigEndian")!.Value);

        string resDir = xml.Element("ResourceDirectory")!.Value;
        List<MQB.Resource> resources = new List<MQB.Resource>();
        List<MQB.Cut> cuts = new List<MQB.Cut>();

        var resourcesNode = xml.Element("Resources")!;
        foreach (XElement resNode in resourcesNode.Elements("Resource"))
            resources.Add(RepackResource(resNode));

        var cutsNode = xml.Element("Cuts")!;
        foreach (XElement cutNode in cutsNode.Elements("Cut"))
            cuts.Add(RepackCut(cutNode));

        mqb.Name = name;
        mqb.Version = version;
        mqb.Framerate = framerate;
        mqb.BigEndian = bigendian;
        mqb.Compression = ReadCompressionInfoFromXml(xml);
        mqb.ResourceDirectory = resDir;
        mqb.Resources = resources;
        mqb.Cuts = cuts;

        XElement xelem = LoadXml(srcPath);
        string outPath = GetRepackDestPath(srcPath, xelem);
        Backup(outPath);
        mqb.TryWriteSoulsFile(outPath);
    }

    #region Repack Helpers

    public static MQB.Resource RepackResource(XElement resNode)
    {
        MQB.Resource resource = new MQB.Resource();

        string name = resNode.Element("Name")!.Value;
        string path = resNode.Element("Path")!.Value;
        int parentIndex = FriendlyParseInt32(nameof(MQB.Resource), nameof(MQB.Resource.ParentIndex), resNode.Element("ParentIndex")!.Value);
        int unk48 = FriendlyParseInt32(nameof(MQB.Resource), nameof(MQB.Resource.Unk48), resNode.Element("Unk48")!.Value);
        List<MQB.CustomData> customData = new List<MQB.CustomData>();

        var resCusDataNode = resNode.Element("Resource_CustomData")!;
        foreach (XElement cusDataNode in resCusDataNode.Elements("CustomData"))
            customData.Add(RepackCustomData(cusDataNode));

        resource.Name = name;
        resource.Path = path;
        resource.ParentIndex = parentIndex;
        resource.Unk48 = unk48;
        resource.CustomData = customData;
        return resource;
    }

    public static MQB.CustomData RepackCustomData(XElement customdataNode)
    {
        MQB.CustomData customdata = new MQB.CustomData();

        string name = customdataNode.Element("Name")!.Value;
        var type = FriendlyParseEnum<MQB.CustomData.DataType>(nameof(MQB.CustomData), nameof(MQB.CustomData.Type), customdataNode.Element("Type")!.Value);

        int memberCount = FriendlyParseInt32(nameof(MQB.CustomData), nameof(MQB.CustomData.MemberCount), customdataNode.Element("MemberCount")!.Value);
        object value = ConvertValueToDataType(customdataNode.Element("Value")!.Value, type, memberCount);
        var sequences = new List<MQB.CustomData.Sequence>();

        var seqsNode = customdataNode.Element("Sequences")!;
        foreach (XElement seqNode in seqsNode.Elements("Sequence"))
            sequences.Add(RepackSequence(seqNode));

        customdata.Name = name;
        customdata.Type = type;
        customdata.Value = value;
        customdata.MemberCount = memberCount;
        customdata.Sequences = sequences;
        return customdata;
    }

    public static MQB.CustomData.Sequence RepackSequence(XElement seqNode)
    {
        MQB.CustomData.Sequence sequence = new MQB.CustomData.Sequence();

        int valueIndex = FriendlyParseInt32(nameof(MQB.CustomData.Sequence), nameof(MQB.CustomData.Sequence.ValueIndex), seqNode.Element("ValueIndex")!.Value);
        var type = FriendlyParseEnum<MQB.CustomData.DataType>(nameof(MQB.CustomData.Sequence), nameof(MQB.CustomData.Sequence.ValueType), seqNode.Element("ValueType")!.Value);
        int pointType = FriendlyParseInt32(nameof(MQB.CustomData.Sequence), nameof(MQB.CustomData.Sequence.PointType), seqNode.Element("PointType")!.Value);
        List<MQB.CustomData.Sequence.Point> points = new List<MQB.CustomData.Sequence.Point>();

        var pointsNode = seqNode.Element("Points")!;
        foreach (XElement pointNode in pointsNode.Elements("Point"))
            points.Add(RepackPoint(pointNode, type));

        sequence.ValueIndex = valueIndex;
        sequence.ValueType = type;
        sequence.PointType = pointType;
        sequence.Points = points;
        return sequence;
    }

    public static MQB.CustomData.Sequence.Point RepackPoint(XElement pointNode, MQB.CustomData.DataType type)
    {
        MQB.CustomData.Sequence.Point point = new MQB.CustomData.Sequence.Point();

        string valueStr = pointNode.Element("Value")!.Value;
        object value;

        switch (type)
        {
            case MQB.CustomData.DataType.Byte: value = FriendlyParseByte(nameof(MQB.CustomData.Sequence.Point), nameof(MQB.CustomData.Sequence.Point.Value), valueStr); break;
            case MQB.CustomData.DataType.Float: value = FriendlyParseFloat32(nameof(MQB.CustomData.Sequence.Point), nameof(MQB.CustomData.Sequence.Point.Value), valueStr); break;
            default: throw new NotSupportedException($"Unsupported sequence point value type: {type}");
        }

        int unk08 = FriendlyParseInt32(nameof(MQB.CustomData.Sequence.Point), nameof(MQB.CustomData.Sequence.Point.Unk08), pointNode.Element("Unk08")!.Value);
        float unk10 = FriendlyParseFloat32(nameof(MQB.CustomData.Sequence.Point), nameof(MQB.CustomData.Sequence.Point.Unk10), pointNode.Element("Unk10")!.Value);
        float unk14 = FriendlyParseFloat32(nameof(MQB.CustomData.Sequence.Point), nameof(MQB.CustomData.Sequence.Point.Unk14), pointNode.Element("Unk14")!.Value);

        point.Value = value;
        point.Unk08 = unk08;
        point.Unk10 = unk10;
        point.Unk14 = unk14;
        return point;
    }

    public static MQB.Cut RepackCut(XElement cutNode)
    {
        MQB.Cut cut = new MQB.Cut();

        string name = cutNode.Element("Name")!.Value;
        int duration = FriendlyParseInt32("Cut", nameof(cut.Duration), cutNode.Element("Duration")!.Value);
        int unk44 = FriendlyParseInt32("Cut", nameof(cut.Unk44), cutNode.Element("Unk44")!.Value);
        List<MQB.Timeline> timelines = new List<MQB.Timeline>();

        var timelinesNode = cutNode.Element("Timelines")!;
        foreach (XElement timelineNode in timelinesNode.Elements("Timeline"))
            timelines.Add(RepackTimeline(timelineNode));

        cut.Name = name;
        cut.Duration = duration;
        cut.Unk44 = unk44;
        cut.Timelines = timelines;
        return cut;
    }

    public static MQB.Timeline RepackTimeline(XElement timelineNode)
    {
        MQB.Timeline timeline = new MQB.Timeline();

        int unk10 = FriendlyParseInt32(nameof(MQB.Timeline), nameof(MQB.Timeline.Unk10), timelineNode.Element("Unk10")!.Value);
        List<MQB.Disposition> dispositions = new List<MQB.Disposition>();
        List<MQB.CustomData> customdata = new List<MQB.CustomData>();

        var dispositionsNode = timelineNode.Element("Dispositions")!;
        foreach (XElement disNode in dispositionsNode.Elements("Disposition"))
            dispositions.Add(RepackDisposition(disNode));

        var timelineCusDataNode = timelineNode.Element("Timeline_CustomData")!;
        foreach (XElement cusDataNode in timelineCusDataNode.Elements("CustomData"))
            customdata.Add(RepackCustomData(cusDataNode));

        timeline.Unk10 = unk10;
        timeline.Dispositions = dispositions;
        timeline.CustomData = customdata;
        return timeline;
    }

    public static MQB.Disposition RepackDisposition(XElement disNode)
    {
        MQB.Disposition disposition = new MQB.Disposition();

        int id = FriendlyParseInt32(nameof(MQB.Disposition), nameof(MQB.Disposition.ID), disNode.Element("ID")!.Value);
        int duration = FriendlyParseInt32(nameof(MQB.Disposition), nameof(MQB.Disposition.Duration), disNode.Element("Duration")!.Value);
        int resIndex = FriendlyParseInt32(nameof(MQB.Disposition), nameof(MQB.Disposition.ResourceIndex), disNode.Element("ResourceIndex")!.Value);
        int startFrame = FriendlyParseInt32(nameof(MQB.Disposition), nameof(MQB.Disposition.StartFrame), disNode.Element("StartFrame")!.Value);
        int unk08 = FriendlyParseInt32(nameof(MQB.Disposition), nameof(MQB.Disposition.Unk08), disNode.Element("Unk08")!.Value);
        int unk14 = FriendlyParseInt32(nameof(MQB.Disposition), nameof(MQB.Disposition.Unk14), disNode.Element("Unk14")!.Value);
        int unk18 = FriendlyParseInt32(nameof(MQB.Disposition), nameof(MQB.Disposition.Unk18), disNode.Element("Unk18")!.Value);
        int unk1C = FriendlyParseInt32(nameof(MQB.Disposition), nameof(MQB.Disposition.Unk1C), disNode.Element("Unk1C")!.Value);
        int unk20 = FriendlyParseInt32(nameof(MQB.Disposition), nameof(MQB.Disposition.Unk20), disNode.Element("Unk20")!.Value);
        int unk28 = FriendlyParseInt32(nameof(MQB.Disposition), nameof(MQB.Disposition.Unk28), disNode.Element("Unk28")!.Value);
        List<MQB.Transform> transforms = new List<MQB.Transform>();
        List<MQB.CustomData> customdata = new List<MQB.CustomData>();

        var transformsNode = disNode.Element("Transforms")!;
        foreach (XElement transformNode in transformsNode.Elements("Transform"))
            transforms.Add(RepackTransform(transformNode));

        var disCusDataNode = disNode.Element("Disposition_CustomData")!;
        foreach (XElement cusDataNode in disCusDataNode.Elements("CustomData"))
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

    public static MQB.Transform RepackTransform(XElement transNode)
    {
        MQB.Transform transform = new MQB.Transform();

        float frame = FriendlyParseFloat32(nameof(MQB.Transform), nameof(MQB.Transform.Frame), transNode.Element("Frame")!.Value);
        Vector3 translation = FriendlyParseVector3(nameof(MQB.Transform), nameof(MQB.Transform.Translation), transNode.Element("Translation")!.Value);
        Vector3 rotation = FriendlyParseVector3(nameof(MQB.Transform), nameof(MQB.Transform.Rotation), transNode.Element("Rotation")!.Value);
        Vector3 scale = FriendlyParseVector3(nameof(MQB.Transform), nameof(MQB.Transform.Scale), transNode.Element("Scale")!.Value);
        Vector3 unk10 = FriendlyParseVector3(nameof(MQB.Transform), nameof(MQB.Transform.Unk10), transNode.Element("Unk10")!.Value);
        Vector3 unk1C = FriendlyParseVector3(nameof(MQB.Transform), nameof(MQB.Transform.Unk1C), transNode.Element("Unk1C")!.Value);
        Vector3 unk34 = FriendlyParseVector3(nameof(MQB.Transform), nameof(MQB.Transform.Unk34), transNode.Element("Unk34")!.Value);
        Vector3 unk40 = FriendlyParseVector3(nameof(MQB.Transform), nameof(MQB.Transform.Unk40), transNode.Element("Unk40")!.Value);
        Vector3 unk58 = FriendlyParseVector3(nameof(MQB.Transform), nameof(MQB.Transform.Unk58), transNode.Element("Unk58")!.Value);
        Vector3 unk64 = FriendlyParseVector3(nameof(MQB.Transform), nameof(MQB.Transform.Unk64), transNode.Element("Unk64")!.Value);

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
            switch (type)
            {
                case MQB.CustomData.DataType.Bool: return Convert.ToBoolean(str);
                case MQB.CustomData.DataType.SByte: return Convert.ToSByte(str);
                case MQB.CustomData.DataType.Byte: return Convert.ToByte(str);
                case MQB.CustomData.DataType.Short: return Convert.ToInt16(str);
                case MQB.CustomData.DataType.Int: return Convert.ToInt32(str);
                case MQB.CustomData.DataType.UInt: return Convert.ToUInt32(str);
                case MQB.CustomData.DataType.Float: return Convert.ToSingle(str);
                case MQB.CustomData.DataType.String: return str;
                case MQB.CustomData.DataType.Custom: return str.FriendlyHexToByteArray();
                case MQB.CustomData.DataType.Color: return ConvertValueToColor(str);
                case MQB.CustomData.DataType.IntColor: return ConvertValueToColor(str);
                case MQB.CustomData.DataType.Vector:
                    switch (memberCount)
                    {
                        case 2: return str.ToVector2();
                        case 3: return str.ToVector3();
                        case 4: return str.ToVector4();
                        default: throw new NotSupportedException($"{nameof(MQB.CustomData.MemberCount)} {memberCount} not supported for: {nameof(MQB.CustomData.DataType.Vector)}");
                    }
                default: throw new NotImplementedException($"Unimplemented custom data type: {type}");
            }
        }
        catch
        {
            throw new FriendlyException($"The value \"{str}\" could not be converted to the type {type} during custom data repacking.");
        }
    }

    public static object ConvertValueToColor(string value)
    {
        string GetComponent(string component)
        {
            int componentIndex = value.IndexOf(component);
            if (componentIndex < 0)
            {
                return string.Empty;
            }

            int startIndex = componentIndex + component.Length;
            if (startIndex >= value.Length)
            {
                return string.Empty;
            }

            int endIndex = value.IndexOf(',', startIndex);
            if (endIndex < 0)
            {
                endIndex = value.IndexOf(']', startIndex);
            }

            if (endIndex < 0)
            {
                return string.Empty;
            }
            else
            {
                int length = endIndex - startIndex;
                return value.Substring(startIndex, length);
            }
        }

        static string CleanComponent(string componentValue)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in componentValue)
            {
                if (char.IsDigit(c))
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        static int ParseComponent(string componentValue, int defaultValue)
        {
            if (int.TryParse(componentValue, out int result))
            {
                return result;
            }

            return defaultValue;
        }

        string alphaComponent = GetComponent("A=");
        string redComponent = GetComponent("R=");
        string greenComponent = GetComponent("G=");
        string blueComponent = GetComponent("B=");
        alphaComponent = CleanComponent(alphaComponent);
        redComponent = CleanComponent(redComponent);
        greenComponent = CleanComponent(greenComponent);
        blueComponent = CleanComponent(blueComponent);
        int alpha = ParseComponent(alphaComponent, 255);
        int red = ParseComponent(redComponent, 0);
        int green = ParseComponent(greenComponent, 0);
        int blue = ParseComponent(blueComponent, 0);
        alpha = Math.Clamp(alpha, 0, 255);
        red = Math.Clamp(red, 0, 255);
        green = Math.Clamp(green, 0, 255);
        blue = Math.Clamp(blue, 0, 255);

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