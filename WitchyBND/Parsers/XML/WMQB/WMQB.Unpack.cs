using System;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Xml.Linq;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public partial class WMQB
{
    public override void Unpack(string srcPath, ISoulsFile? file, bool recursive)
    {
        var mqb = (file as MQB)!;
        var filename = OSPath.GetFileName(srcPath);

        var xml = PrepareXmlManifest(srcPath, recursive, true, mqb.Compression, out XDocument xDoc, null);

        xml.Add(new XElement("Name", mqb.Name));
        xml.Add(new XElement("MQBVersion", mqb.Version.ToString()));
        xml.Add(new XElement("Filename", filename));
        xml.Add(new XElement("Framerate", mqb.Framerate));
        xml.Add(new XElement("BigEndian", mqb.BigEndian.ToString()));
        xml.Add(new XElement("ResourceDirectory", mqb.ResourceDirectory));

        var resXml = new XElement("Resources");
        foreach (var resource in mqb.Resources)
            UnpackResource(resXml, resource);
        xml.Add(resXml);
        
        var cutsXml = new XElement("Cuts");
        foreach (var cut in mqb.Cuts)
            UnpackCut(cutsXml, cut);
        xml.Add(cutsXml);
        
        var destPath = GetUnpackDestPath(srcPath, recursive);
        WriteXmlManifest(xDoc, srcPath, recursive);
    }

    #region Unpack Helpers

    public static void UnpackResource(XElement xml, MQB.Resource resource)
    {
        var resXml = new XElement("Resource");
        resXml.Add(new XElement("Name", resource.Name));
        resXml.Add(new XElement("Path", resource.Path));
        resXml.Add(new XElement("ParentIndex", resource.ParentIndex.ToString()));
        resXml.Add(new XElement("Unk48", resource.Unk48.ToString()));
        var parametersXml = new XElement("Resource_Parameters");
        foreach (var parameter in resource.Parameters)
            UnpackParameter(parametersXml, parameter);
        resXml.Add(parametersXml);
        xml.Add(resXml);
    }

    public static void UnpackParameter(XElement xml, MQB.Parameter parameter)
    {
        var paramXml = new XElement("Parameter");
        paramXml.Add(new XElement("Name", parameter.Name));
        paramXml.Add(new XElement("Type", parameter.Type));

        switch (parameter.Type)
        {
            case MQB.Parameter.DataType.Vector:
                switch (parameter.MemberCount)
                {
                    case 2:
                        paramXml.Add(new XElement("Value", ((Vector2)parameter.Value).Vector2ToString()));
                        break;
                    case 3:
                        paramXml.Add(new XElement("Value", ((Vector3)parameter.Value).Vector3ToString()));
                        break;
                    case 4:
                        paramXml.Add(new XElement("Value", ((Vector4)parameter.Value).Vector4ToString()));
                        break;
                    default:
                        throw new NotImplementedException($"{nameof(MQB.Parameter.MemberCount)} {parameter.MemberCount} not implemented for: {nameof(MQB.Parameter.DataType.Vector)}");
                }
                break;
            case MQB.Parameter.DataType.Custom:
                paramXml.Add(new XElement("Value", ((byte[])parameter.Value).ToHexString()));
                break;
            case MQB.Parameter.DataType.Color:
            case MQB.Parameter.DataType.IntColor:
                Color color = (Color)parameter.Value;
                switch (parameter.MemberCount)
                {
                    case 3:
                        // Prevent showing alpha when it really isn't there
                        paramXml.Add(new XElement("Value", $"Color [R={color.R}, G={color.G}, B={color.B}]"));
                        break;
                    case 4:
                    default:
                        paramXml.Add(new XElement("Value", parameter.Value.ToString()));
                        break;
                }
                break;
            default:
                paramXml.Add(new XElement("Value", parameter.Value.ToString()));
                break;
        }

        paramXml.Add(new XElement("MemberCount", parameter.MemberCount.ToString()));
        var seqXml = new XElement("Sequences");
        foreach (MQB.Parameter.Sequence sequence in parameter.Sequences)
            UnpackSequence(seqXml, sequence);
        paramXml.Add(seqXml);
        xml.Add(paramXml);
    }

    public static void UnpackSequence(XElement xml, MQB.Parameter.Sequence sequence)
    {
        var seqXml = new XElement("Sequence");
        seqXml.Add(new XElement("ValueIndex", sequence.ValueIndex.ToString()));
        seqXml.Add(new XElement("ValueType", sequence.ValueType.ToString()));
        seqXml.Add(new XElement("PointType", sequence.PointType.ToString()));
        var pointsXml = new XElement("Points");
        foreach (var point in sequence.Points)
            UnpackPoint(pointsXml, point);
        seqXml.Add(pointsXml);
        xml.Add(seqXml);
    }

    public static void UnpackPoint(XElement xml, MQB.Parameter.Sequence.Point point)
    {
        var pointXml = new XElement("Point");
        pointXml.Add(new XElement("Value", $"{point.Value}"));
        pointXml.Add(new XElement("Unk08", $"{point.Unk08}"));
        pointXml.Add(new XElement("Unk10", $"{point.Unk10}"));
        pointXml.Add(new XElement("Unk14", $"{point.Unk14}"));
        xml.Add(pointXml);
    }

    public static void UnpackCut(XElement xml, MQB.Cut cut)
    {
        var cutXml = new XElement("Cut");
        cutXml.Add(new XElement("Name", $"{cut.Name}"));
        cutXml.Add(new XElement("Duration", $"{cut.Duration}"));
        cutXml.Add(new XElement("Unk44", $"{cut.Unk44}"));
        var timeXml = new XElement("Timelines");
        foreach (var timeline in cut.Timelines)
            UnpackTimeline(timeXml, timeline);
        cutXml.Add(timeXml);
        xml.Add(cutXml);
    }

    public static void UnpackTimeline(XElement xml, MQB.Timeline timeline)
    {
        var timeXml = new XElement("Timeline");
        timeXml.Add(new XElement("Unk10", $"{timeline.Unk10}"));
        var dispXml = new XElement("Events");
        foreach (MQB.Event? @event in timeline.Events)
            UnpackEvent(dispXml, @event);
        var parametersXml = new XElement("Timeline_Parameters");
        foreach (var parameter in timeline.Parameters)
            UnpackParameter(parametersXml, parameter);
        timeXml.Add(dispXml);
        timeXml.Add(parametersXml);
        xml.Add(timeXml);
    }

    public static void UnpackEvent(XElement xml, MQB.Event @event)
    {
        var dispXml = new XElement("Event");
        dispXml.Add(new XElement("ID", $"{@event.ID}"));
        dispXml.Add(new XElement("Duration", $"{@event.Duration}"));
        dispXml.Add(new XElement("ResourceIndex", $"{@event.ResourceIndex}"));
        dispXml.Add(new XElement("StartFrame", $"{@event.StartFrame}"));
        dispXml.Add(new XElement("Unk08", $"{@event.Unk08}"));
        dispXml.Add(new XElement("Unk14", $"{@event.Unk14}"));
        dispXml.Add(new XElement("Unk18", $"{@event.Unk18}"));
        dispXml.Add(new XElement("Unk1C", $"{@event.Unk1C}"));
        dispXml.Add(new XElement("Unk20", $"{@event.Unk20}"));
        dispXml.Add(new XElement("Unk28", $"{@event.Unk28}"));
        
        var tfXml = new XElement("Transforms");
        foreach (var transform in @event.Transforms)
            UnpackTransform(tfXml, transform);
        dispXml.Add(tfXml);
        var cdXml = new XElement("Event_Parameters");
        foreach (var parameter in @event.Parameters)
            UnpackParameter(cdXml, parameter);
        dispXml.Add(cdXml);
        xml.Add(dispXml);
    }

    public static void UnpackTransform(XElement xml, MQB.Transform transform)
    {
        var tfXml = new XElement("Transform");
        tfXml.Add(new XElement("Frame", $"{transform.Frame}"));
        tfXml.Add(new XElement("Translation", transform.Translation.Vector3ToString()));
        tfXml.Add(new XElement("Rotation", transform.Rotation.Vector3ToString()));
        tfXml.Add(new XElement("Scale", transform.Scale.Vector3ToString()));
        tfXml.Add(new XElement("Unk10", transform.Unk10.Vector3ToString()));
        tfXml.Add(new XElement("Unk1C", transform.Unk1C.Vector3ToString()));
        tfXml.Add(new XElement("Unk34", transform.Unk34.Vector3ToString()));
        tfXml.Add(new XElement("Unk40", transform.Unk40.Vector3ToString()));
        tfXml.Add(new XElement("Unk58", transform.Unk58.Vector3ToString()));
        tfXml.Add(new XElement("Unk64", transform.Unk64.Vector3ToString()));
        xml.Add(tfXml);
    }

    #endregion
}