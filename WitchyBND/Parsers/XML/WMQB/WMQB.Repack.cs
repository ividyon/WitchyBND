using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using SoulsFormats;
using SoulsFormats.Formats.MQB;
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
        List<MQB.Parameter> parameter = new List<MQB.Parameter>();

        var resParametersNode = resNode.Element("Resource_Parameters")!;
        foreach (XElement parameterNode in resParametersNode.Elements("Parameter"))
            parameter.Add(RepackParameter(parameterNode));

        resource.Name = name;
        resource.Path = path;
        resource.ParentIndex = parentIndex;
        resource.Unk48 = unk48;
        resource.Parameters = parameter;
        return resource;
    }

    public static MQB.Parameter RepackParameter(XElement parametersNode)
    {
        MQB.Parameter parameters = new MQB.Parameter();

        string name = parametersNode.Element("Name")!.Value;
        var type = FriendlyParseEnum<MQB.Parameter.DataType>(nameof(MQB.Parameter), nameof(MQB.Parameter.Type), parametersNode.Element("Type")!.Value);

        int memberCount = FriendlyParseInt32(nameof(MQB.Parameter), nameof(MQB.Parameter.MemberCount), parametersNode.Element("MemberCount")!.Value);
        object value = ConvertValueToDataType(parametersNode.Element("Value")!.Value, type, memberCount);
        var sequences = new List<MQB.Parameter.Sequence>();

        var seqsNode = parametersNode.Element("Sequences")!;
        foreach (XElement seqNode in seqsNode.Elements("Sequence"))
            sequences.Add(RepackSequence(seqNode));

        parameters.Name = name;
        parameters.Type = type;
        parameters.Value = value;
        parameters.MemberCount = memberCount;
        parameters.Sequences = sequences;
        return parameters;
    }

    public static MQB.Parameter.Sequence RepackSequence(XElement seqNode)
    {
        MQB.Parameter.Sequence sequence = new MQB.Parameter.Sequence();

        int valueIndex = FriendlyParseInt32(nameof(MQB.Parameter.Sequence), nameof(MQB.Parameter.Sequence.ValueIndex), seqNode.Element("ValueIndex")!.Value);
        var type = FriendlyParseEnum<MQB.Parameter.DataType>(nameof(MQB.Parameter.Sequence), nameof(MQB.Parameter.Sequence.ValueType), seqNode.Element("ValueType")!.Value);
        int pointType = FriendlyParseInt32(nameof(MQB.Parameter.Sequence), nameof(MQB.Parameter.Sequence.PointType), seqNode.Element("PointType")!.Value);
        List<MQB.Parameter.Sequence.Point> points = new List<MQB.Parameter.Sequence.Point>();

        var pointsNode = seqNode.Element("Points")!;
        foreach (XElement pointNode in pointsNode.Elements("Point"))
            points.Add(RepackPoint(pointNode, type));

        sequence.ValueIndex = valueIndex;
        sequence.ValueType = type;
        sequence.PointType = pointType;
        sequence.Points = points;
        return sequence;
    }

    public static MQB.Parameter.Sequence.Point RepackPoint(XElement pointNode, MQB.Parameter.DataType type)
    {
        MQB.Parameter.Sequence.Point point = new MQB.Parameter.Sequence.Point();

        string valueStr = pointNode.Element("Value")!.Value;
        object value;

        switch (type)
        {
            case MQB.Parameter.DataType.Byte: value = FriendlyParseByte(nameof(MQB.Parameter.Sequence.Point), nameof(MQB.Parameter.Sequence.Point.Value), valueStr); break;
            case MQB.Parameter.DataType.Float: value = FriendlyParseFloat32(nameof(MQB.Parameter.Sequence.Point), nameof(MQB.Parameter.Sequence.Point.Value), valueStr); break;
            default: throw new NotSupportedException($"Unsupported sequence point value type: {type}");
        }

        int unk08 = FriendlyParseInt32(nameof(MQB.Parameter.Sequence.Point), nameof(MQB.Parameter.Sequence.Point.Unk08), pointNode.Element("Unk08")!.Value);
        float unk10 = FriendlyParseFloat32(nameof(MQB.Parameter.Sequence.Point), nameof(MQB.Parameter.Sequence.Point.Unk10), pointNode.Element("Unk10")!.Value);
        float unk14 = FriendlyParseFloat32(nameof(MQB.Parameter.Sequence.Point), nameof(MQB.Parameter.Sequence.Point.Unk14), pointNode.Element("Unk14")!.Value);

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
        List<MQB.Event> events = new List<MQB.Event>();
        List<MQB.Parameter> parameters = new List<MQB.Parameter>();

        var eventsNode = timelineNode.Element("Events")!;
        foreach (XElement eventNode in eventsNode.Elements("Event"))
            events.Add(RepackEvent(eventNode));

        var timelineParametersNode = timelineNode.Element("Timeline_Parameters")!;
        foreach (XElement parameterNode in timelineParametersNode.Elements("Parameter"))
            parameters.Add(RepackParameter(parameterNode));

        timeline.Unk10 = unk10;
        timeline.Events = events;
        timeline.Parameters = parameters;
        return timeline;
    }

    public static MQB.Event RepackEvent(XElement eventNode)
    {
        MQB.Event @event = new MQB.Event();

        int id = FriendlyParseInt32(nameof(MQB.Event), nameof(MQB.Event.ID), eventNode.Element("ID")!.Value);
        int duration = FriendlyParseInt32(nameof(MQB.Event), nameof(MQB.Event.Duration), eventNode.Element("Duration")!.Value);
        int resIndex = FriendlyParseInt32(nameof(MQB.Event), nameof(MQB.Event.ResourceIndex), eventNode.Element("ResourceIndex")!.Value);
        int startFrame = FriendlyParseInt32(nameof(MQB.Event), nameof(MQB.Event.StartFrame), eventNode.Element("StartFrame")!.Value);
        int unk08 = FriendlyParseInt32(nameof(MQB.Event), nameof(MQB.Event.Unk08), eventNode.Element("Unk08")!.Value);
        int unk14 = FriendlyParseInt32(nameof(MQB.Event), nameof(MQB.Event.Unk14), eventNode.Element("Unk14")!.Value);
        int unk18 = FriendlyParseInt32(nameof(MQB.Event), nameof(MQB.Event.Unk18), eventNode.Element("Unk18")!.Value);
        int unk1C = FriendlyParseInt32(nameof(MQB.Event), nameof(MQB.Event.Unk1C), eventNode.Element("Unk1C")!.Value);
        int unk20 = FriendlyParseInt32(nameof(MQB.Event), nameof(MQB.Event.Unk20), eventNode.Element("Unk20")!.Value);
        int unk28 = FriendlyParseInt32(nameof(MQB.Event), nameof(MQB.Event.Unk28), eventNode.Element("Unk28")!.Value);
        List<MQB.Transform> transforms = new List<MQB.Transform>();
        List<MQB.Parameter> parameters = new List<MQB.Parameter>();

        var transformsNode = eventNode.Element("Transforms")!;
        foreach (XElement transformNode in transformsNode.Elements("Transform"))
            transforms.Add(RepackTransform(transformNode));

        var evParametersNode = eventNode.Element("Event_Parameters")!;
        foreach (XElement parameterNode in evParametersNode.Elements("Parameter"))
            parameters.Add(RepackParameter(parameterNode));

        @event.ID = id;
        @event.Duration = duration;
        @event.ResourceIndex = resIndex;
        @event.StartFrame = startFrame;
        @event.Unk08 = unk08;
        @event.Unk14 = unk14;
        @event.Unk18 = unk18;
        @event.Unk1C = unk1C;
        @event.Unk20 = unk20;
        @event.Unk28 = unk28;
        @event.Transforms = transforms;
        @event.Parameters = parameters;
        return @event;
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

    public static object ConvertValueToDataType(string str, MQB.Parameter.DataType type, int memberCount)
    {
        try
        {
            switch (type)
            {
                case MQB.Parameter.DataType.Bool: return Convert.ToBoolean(str);
                case MQB.Parameter.DataType.SByte: return Convert.ToSByte(str);
                case MQB.Parameter.DataType.Byte: return Convert.ToByte(str);
                case MQB.Parameter.DataType.Short: return Convert.ToInt16(str);
                case MQB.Parameter.DataType.Int: return Convert.ToInt32(str);
                case MQB.Parameter.DataType.UInt: return Convert.ToUInt32(str);
                case MQB.Parameter.DataType.Float: return Convert.ToSingle(str);
                case MQB.Parameter.DataType.String: return str;
                case MQB.Parameter.DataType.Custom: return str.FriendlyHexToByteArray();
                case MQB.Parameter.DataType.Color: return ConvertValueToColor(str);
                case MQB.Parameter.DataType.IntColor: return ConvertValueToColor(str);
                case MQB.Parameter.DataType.Vector:
                    switch (memberCount)
                    {
                        case 2: return str.ToVector2();
                        case 3: return str.ToVector3();
                        case 4: return str.ToVector4();
                        default: throw new NotSupportedException($"{nameof(MQB.Parameter.MemberCount)} {memberCount} not supported for: {nameof(MQB.Parameter.DataType.Vector)}");
                    }
                default: throw new NotImplementedException($"Unimplemented parameter type: {type}");
            }
        }
        catch
        {
            throw new FriendlyException($"The value \"{str}\" could not be converted to the type {type} during parameter repacking.");
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