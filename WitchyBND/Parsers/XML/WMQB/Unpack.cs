﻿using System.IO;
using System.Numerics;
using System.Xml;
using WitchyFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public partial class WMQB
{

    public override void Unpack(string srcPath)
    {
        var mqb = MQB.Read(srcPath);
        var targetPath = GetUnpackDestPath(srcPath);
        var filename = Path.GetFileName(srcPath);
        Directory.CreateDirectory(targetPath);
        var xws = new XmlWriterSettings();
        xws.Indent = true;
        var xw = XmlWriter.Create(targetPath, xws);
        xw.WriteStartElement("MQB");
        xw.WriteElementString("Name", mqb.Name);

        if (!string.IsNullOrEmpty(Configuration.Args.Location))
            xw.WriteElementString("sourcePath", Path.GetFullPath(Path.GetDirectoryName(srcPath)));

        xw.WriteElementString("Version", mqb.Version.ToString());
        xw.WriteElementString("Filename", filename);
        xw.WriteElementString("Framerate", mqb.Framerate.ToString());
        xw.WriteElementString("BigEndian", mqb.BigEndian.ToString());
        xw.WriteElementString("Compression", mqb.Compression.ToString());
        xw.WriteElementString("ResourceDirectory", mqb.ResourceDirectory);
        xw.WriteStartElement("Resources");
        foreach (var resource in mqb.Resources)
            UnpackResource(xw, resource);
        xw.WriteEndElement();
        xw.WriteStartElement("Cuts");
        foreach (var cut in mqb.Cuts)
            UnpackCut(xw, cut);
        xw.WriteEndElement();
        xw.WriteEndElement();
        xw.Close();
    }

    #region UnpackHelpers

        public static void UnpackResource(XmlWriter xw, MQB.Resource resource)
        {
            xw.WriteStartElement($"Resource");
            xw.WriteElementString("Name", $"{resource.Name}");
            xw.WriteElementString("Path", $"{resource.Path}");
            xw.WriteElementString("ParentIndex", $"{resource.ParentIndex}");
            xw.WriteElementString("Unk48", $"{resource.Unk48}");
            xw.WriteStartElement("Resource_CustomData");
            foreach (var customdata in resource.CustomData)
                UnpackCustomData(xw, customdata);
            xw.WriteEndElement();
            xw.WriteEndElement();
        }

        public static void UnpackCustomData(XmlWriter xw, MQB.CustomData customdata)
        {
            xw.WriteStartElement($"CustomData");
            xw.WriteElementString("Name", $"{customdata.Name}");
            xw.WriteElementString("Type", $"{customdata.Type}");

            switch (customdata.Type)
            {
                case MQB.CustomData.DataType.Vector3:
                    xw.WriteElementString("Value", ((Vector3)customdata.Value).Vector3ToString());
                    break;
                case MQB.CustomData.DataType.Custom:
                    xw.WriteElementString("Value", ((byte[])customdata.Value).ToHexString());
                    break;
                default:
                    xw.WriteElementString("Value", $"{customdata.Value}");
                    break;
            }

            xw.WriteElementString("Unk44", $"{customdata.Unk44}");
            xw.WriteStartElement("Sequences");
            foreach (var sequence in customdata.Sequences)
                UnpackSequence(xw, sequence);
            xw.WriteEndElement();
            xw.WriteEndElement();
        }

        public static void UnpackSequence(XmlWriter xw, MQB.CustomData.Sequence sequence)
        {
            xw.WriteStartElement($"Sequence");
            xw.WriteElementString("ValueIndex", $"{sequence.ValueIndex}");
            xw.WriteElementString("ValueType", $"{sequence.ValueType}");
            xw.WriteElementString("PointType", $"{sequence.PointType}");
            xw.WriteStartElement("Points");
            foreach (var point in sequence.Points)
                UnpackPoint(xw, point);
            xw.WriteEndElement();
            xw.WriteEndElement();
        }

        public static void UnpackPoint(XmlWriter xw, MQB.CustomData.Sequence.Point point)
        {
            xw.WriteStartElement($"Point");
            xw.WriteElementString("Value", $"{point.Value}");
            xw.WriteElementString("Unk08", $"{point.Unk08}");
            xw.WriteElementString("Unk10", $"{point.Unk10}");
            xw.WriteElementString("Unk14", $"{point.Unk14}");
            xw.WriteEndElement();
        }

        public static void UnpackCut(XmlWriter xw, MQB.Cut cut)
        {
            xw.WriteStartElement($"Cut");
            xw.WriteElementString("Name", $"{cut.Name}");
            xw.WriteElementString("Duration", $"{cut.Duration}");
            xw.WriteElementString("Unk44", $"{cut.Unk44}");
            xw.WriteStartElement($"Timelines");
            foreach (var timeline in cut.Timelines)
                UnpackTimeline(xw, timeline);
            xw.WriteEndElement();
            xw.WriteEndElement();
        }

        public static void UnpackTimeline(XmlWriter xw, MQB.Timeline timeline)
        {
            xw.WriteStartElement($"Timeline");
            xw.WriteElementString("Unk10", $"{timeline.Unk10}");
            xw.WriteStartElement($"Dispositions");
            foreach (var disposition in timeline.Dispositions)
                UnpackDisposition(xw, disposition);
            xw.WriteEndElement();
            xw.WriteStartElement($"Timeline_CustomData");
            foreach (var customdata in timeline.CustomData)
                UnpackCustomData(xw, customdata);
            xw.WriteEndElement();
            xw.WriteEndElement();
        }

        public static void UnpackDisposition(XmlWriter xw, MQB.Disposition disposition)
        {
            xw.WriteStartElement($"Disposition");
            xw.WriteElementString("ID", $"{disposition.ID}");
            xw.WriteElementString("Duration", $"{disposition.Duration}");
            xw.WriteElementString("ResourceIndex", $"{disposition.ResourceIndex}");
            xw.WriteElementString("StartFrame", $"{disposition.StartFrame}");
            xw.WriteElementString("Unk08", $"{disposition.Unk08}");
            xw.WriteElementString("Unk14", $"{disposition.Unk14}");
            xw.WriteElementString("Unk18", $"{disposition.Unk18}");
            xw.WriteElementString("Unk1C", $"{disposition.Unk1C}");
            xw.WriteElementString("Unk20", $"{disposition.Unk20}");
            xw.WriteElementString("Unk28", $"{disposition.Unk28}");
            xw.WriteStartElement($"Transforms");
            foreach (var transform in disposition.Transforms)
                UnpackTransform(xw, transform);
            xw.WriteEndElement();
            xw.WriteStartElement($"Disposition_CustomData");
            foreach (var customdata in disposition.CustomData)
                UnpackCustomData(xw, customdata);
            xw.WriteEndElement();
            xw.WriteEndElement();
        }

        public static void UnpackTransform(XmlWriter xw, MQB.Transform transform)
        {
            xw.WriteStartElement($"Transform");
            xw.WriteElementString("Frame", $"{transform.Frame}");
            xw.WriteElementString("Translation", transform.Translation.Vector3ToString());
            xw.WriteElementString("Rotation", transform.Rotation.Vector3ToString());
            xw.WriteElementString("Scale", transform.Scale.Vector3ToString());
            xw.WriteElementString("Unk10", transform.Unk10.Vector3ToString());
            xw.WriteElementString("Unk1C", transform.Unk1C.Vector3ToString());
            xw.WriteElementString("Unk34", transform.Unk34.Vector3ToString());
            xw.WriteElementString("Unk40", transform.Unk40.Vector3ToString());
            xw.WriteElementString("Unk58", transform.Unk58.Vector3ToString());
            xw.WriteElementString("Unk64", transform.Unk64.Vector3ToString());
            xw.WriteEndElement();
        }

        #endregion
}