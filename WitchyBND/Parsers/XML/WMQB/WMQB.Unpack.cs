using System;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Xml;
using System.Xml.Linq;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public partial class WMQB
{
    public override void Unpack(string srcPath, ISoulsFile? file, bool recursive)
    {
        var mqb = (file as MQB)!;
        var filename = Path.GetFileName(srcPath);

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
        var customDataXml = new XElement("Resource_CustomData");
        foreach (var customdata in resource.CustomData)
            UnpackCustomData(customDataXml, customdata);
        resXml.Add(customDataXml);
        xml.Add(resXml);
    }

    public static void UnpackCustomData(XElement xml, MQB.CustomData customdata)
    {
        var cdXml = new XElement("CustomData");
        cdXml.Add(new XElement("Name", customdata.Name));
        cdXml.Add(new XElement("Type", customdata.Type));

        switch (customdata.Type)
        {
            case MQB.CustomData.DataType.Vector:
                switch (customdata.MemberCount)
                {
                    case 2:
                        cdXml.Add(new XElement("Value", ((Vector2)customdata.Value).Vector2ToString()));
                        break;
                    case 3:
                        cdXml.Add(new XElement("Value", ((Vector3)customdata.Value).Vector3ToString()));
                        break;
                    case 4:
                        cdXml.Add(new XElement("Value", ((Vector4)customdata.Value).Vector4ToString()));
                        break;
                    default:
                        throw new NotImplementedException($"{nameof(MQB.CustomData.MemberCount)} {customdata.MemberCount} not implemented for: {nameof(MQB.CustomData.DataType.Vector)}");
                }
                break;
            case MQB.CustomData.DataType.Custom:
                cdXml.Add(new XElement("Value", ((byte[])customdata.Value).ToHexString()));
                break;
            case MQB.CustomData.DataType.Color:
            case MQB.CustomData.DataType.IntColor:
                Color color = (Color)customdata.Value;
                switch (customdata.MemberCount)
                {
                    case 3:
                        // Prevent showing alpha when it really isn't there
                        cdXml.Add(new XElement("Value", $"Color [R={color.R}, G={color.G}, B={color.B}]"));
                        break;
                    case 4:
                    default:
                        cdXml.Add(new XElement("Value", customdata.Value.ToString()));
                        break;
                }
                break;
            default:
                cdXml.Add(new XElement("Value", customdata.Value.ToString()));
                break;
        }

        cdXml.Add(new XElement("MemberCount", customdata.MemberCount.ToString()));
        var seqXml = new XElement("Sequences");
        foreach (MQB.CustomData.Sequence sequence in customdata.Sequences)
            UnpackSequence(seqXml, sequence);
        cdXml.Add(seqXml);
        xml.Add(cdXml);
    }

    public static void UnpackSequence(XElement xml, MQB.CustomData.Sequence sequence)
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

    public static void UnpackPoint(XElement xml, MQB.CustomData.Sequence.Point point)
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
        var dispXml = new XElement("Dispositions");
        foreach (var disposition in timeline.Dispositions)
            UnpackDisposition(dispXml, disposition);
        var customDataXml = new XElement("Timeline_CustomData");
        foreach (var customdata in timeline.CustomData)
            UnpackCustomData(customDataXml, customdata);
        timeXml.Add(dispXml);
        timeXml.Add(customDataXml);
        xml.Add(timeXml);
    }

    public static void UnpackDisposition(XElement xml, MQB.Disposition disposition)
    {
        var dispXml = new XElement("Disposition");
        dispXml.Add(new XElement("ID", $"{disposition.ID}"));
        dispXml.Add(new XElement("Duration", $"{disposition.Duration}"));
        dispXml.Add(new XElement("ResourceIndex", $"{disposition.ResourceIndex}"));
        dispXml.Add(new XElement("StartFrame", $"{disposition.StartFrame}"));
        dispXml.Add(new XElement("Unk08", $"{disposition.Unk08}"));
        dispXml.Add(new XElement("Unk14", $"{disposition.Unk14}"));
        dispXml.Add(new XElement("Unk18", $"{disposition.Unk18}"));
        dispXml.Add(new XElement("Unk1C", $"{disposition.Unk1C}"));
        dispXml.Add(new XElement("Unk20", $"{disposition.Unk20}"));
        dispXml.Add(new XElement("Unk28", $"{disposition.Unk28}"));
        
        var tfXml = new XElement("Transforms");
        foreach (var transform in disposition.Transforms)
            UnpackTransform(tfXml, transform);
        dispXml.Add(tfXml);
        var cdXml = new XElement("Disposition_CustomData");
        foreach (var customdata in disposition.CustomData)
            UnpackCustomData(cdXml, customdata);
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