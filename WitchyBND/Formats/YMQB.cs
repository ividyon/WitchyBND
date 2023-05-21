using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Xml;
using WitchyBND;

namespace Yabber
{
    static class YMQB
    {
        public static void Unpack(this MQB mqb, string filename, string targetDir, IProgress<float> progress)
        {
            Directory.CreateDirectory(targetDir);
            var xws = new XmlWriterSettings();
            xws.Indent = true;
            var xw = XmlWriter.Create($"{targetDir}\\{Path.GetFileNameWithoutExtension(filename)}.mqb.xml", xws);
            xw.WriteStartElement("MQB");
            xw.WriteElementString("Name", mqb.Name);
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
                case MQB.CustomData.DataType.Vector3: xw.WriteElementString("Value", ((Vector3)customdata.Value).Vector3ToString()); break;
                default: xw.WriteElementString("Value", $"{customdata.Value}");  break;
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

        public static void Repack(string sourceFile)
        {
            MQB mqb = new MQB();
            XmlDocument xml = new XmlDocument();
            xml.Load(sourceFile);

            string name = xml.SelectSingleNode("MQB/Name").InnerText;
            var version = (MQB.MQBVersion)Enum.Parse(typeof(MQB.MQBVersion), xml.SelectSingleNode("MQB/Version").InnerText);
            float framerate = float.Parse(xml.SelectSingleNode("MQB/Framerate").InnerText);
            bool bigendian = bool.Parse(xml.SelectSingleNode("MQB/BigEndian").InnerText);
            Enum.TryParse(xml.SelectSingleNode("MQB/Compression")?.InnerText ?? "None", out DCX.Type compression);
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

            string outPath = sourceFile.Replace(".mqb.xml", ".mqb");
            WBUtil.Backup(outPath);
            mqb.Write(outPath);
        }

        #region RepackHelpers

        public static MQB.Resource RepackResource(XmlNode resNode)
        {
            MQB.Resource resource = new MQB.Resource();

            string name = resNode.SelectSingleNode("Name").InnerText;
            string path = resNode.SelectSingleNode("Path").InnerText;
            int parentIndex = int.Parse(resNode.SelectSingleNode("ParentIndex").InnerText);
            int unk48 = int.Parse(resNode.SelectSingleNode("Unk48").InnerText);
            List<MQB.CustomData> customData = new List<MQB.CustomData>();

            var resCusDataNode = resNode.SelectSingleNode("Resource_CustomData");
            foreach (XmlNode cusDataNode in resCusDataNode.SelectNodes("CustomData"))
                customData.Add(RepackCustomData(resNode));

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
            var type = (MQB.CustomData.DataType)Enum.Parse(typeof (MQB.CustomData.DataType), customdataNode.SelectSingleNode("Type").InnerText);
            object value = ConvertValueToDataType(customdataNode.SelectSingleNode("Value").InnerText, type);
            int unk44 = int.Parse(customdataNode.SelectSingleNode("Unk44").InnerText);
            List<MQB.CustomData.Sequence> sequences = new List<MQB.CustomData.Sequence>();

            var seqsNode = customdataNode.SelectSingleNode("Sequences");
            foreach (XmlNode seqNode in seqsNode.SelectNodes("Sequence"))
                sequences.Add(RepackSequence(seqNode));

            customdata.Name = name;
            customdata.Type = type;
            customdata.Value = value;
            customdata.Unk44 = unk44;
            customdata.Sequences = sequences;
            return customdata;
        }

        public static MQB.CustomData.Sequence RepackSequence(XmlNode seqNode)
        {
            MQB.CustomData.Sequence sequence = new MQB.CustomData.Sequence();

            int valueIndex = int.Parse(seqNode.SelectSingleNode("ValueIndex").InnerText);
            var type = (MQB.CustomData.DataType)Enum.Parse(typeof(MQB.CustomData.DataType), seqNode.SelectSingleNode("ValueType").InnerText);
            int pointType = int.Parse(seqNode.SelectSingleNode("PointType").InnerText);
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
                case MQB.CustomData.DataType.Byte: value = byte.Parse(valueStr); break;
                case MQB.CustomData.DataType.Float: value = float.Parse(valueStr); break;
                default: throw new NotSupportedException($"Unsupported sequence point value type: {type}");
            }

            int unk08 = int.Parse(pointNode.SelectSingleNode("Unk08").InnerText);
            float unk10 = float.Parse(pointNode.SelectSingleNode("Unk10").InnerText);
            float unk14 = float.Parse(pointNode.SelectSingleNode("Unk14").InnerText);

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
            int duration = int.Parse(cutNode.SelectSingleNode("Duration").InnerText);
            int unk44 = int.Parse(cutNode.SelectSingleNode("Unk44").InnerText);
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

            int unk10 = int.Parse(timelineNode.SelectSingleNode("Unk10").InnerText);
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

            int id = int.Parse(disNode.SelectSingleNode("ID").InnerText);
            int duration = int.Parse(disNode.SelectSingleNode("Duration").InnerText);
            int resIndex = int.Parse(disNode.SelectSingleNode("ResourceIndex").InnerText);
            int startFrame = int.Parse(disNode.SelectSingleNode("StartFrame").InnerText);
            int unk08 = int.Parse(disNode.SelectSingleNode("Unk08").InnerText);
            int unk14 = int.Parse(disNode.SelectSingleNode("Unk14").InnerText);
            int unk18 = int.Parse(disNode.SelectSingleNode("Unk18").InnerText);
            int unk1C = int.Parse(disNode.SelectSingleNode("Unk1C").InnerText);
            int unk20 = int.Parse(disNode.SelectSingleNode("Unk20").InnerText);
            int unk28 = int.Parse(disNode.SelectSingleNode("Unk28").InnerText);
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

            float frame = float.Parse(transNode.SelectSingleNode("Frame").InnerText);
            Vector3 translation = transNode.SelectSingleNode("Translation").InnerText.ToVector3();
            Vector3 rotation = transNode.SelectSingleNode("Rotation").InnerText.ToVector3();
            Vector3 scale = transNode.SelectSingleNode("Scale").InnerText.ToVector3();
            Vector3 unk10 = transNode.SelectSingleNode("Unk10").InnerText.ToVector3();
            Vector3 unk1C = transNode.SelectSingleNode("Unk1C").InnerText.ToVector3();
            Vector3 unk34 = transNode.SelectSingleNode("Unk34").InnerText.ToVector3();
            Vector3 unk40 = transNode.SelectSingleNode("Unk40").InnerText.ToVector3();
            Vector3 unk58 = transNode.SelectSingleNode("Unk58").InnerText.ToVector3();
            Vector3 unk64 = transNode.SelectSingleNode("Unk64").InnerText.ToVector3();

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

        public static string Vector3ToString(this Vector3 vector)
        {
            return $"X:{vector.X} Y:{vector.Y} Z:{vector.Z}";
        }

        public static Vector3 ToVector3(this string str)
        {
            int xStartIndex = str.IndexOf("X:") + 2;
            int yStartIndex = str.IndexOf("Y:") + 2;
            int zStartIndex = str.IndexOf("Z:") + 2;

            string xStr = str.Substring(xStartIndex, yStartIndex - xStartIndex - 3);
            string yStr = str.Substring(yStartIndex, zStartIndex - yStartIndex - 3);
            string zStr = str.Substring(zStartIndex, str.Length - zStartIndex);

            float x = float.Parse(xStr);
            float y = float.Parse(yStr);
            float z = float.Parse(zStr);

            return new Vector3(x,y,z);
        }

        public static object ConvertValueToDataType(string str, MQB.CustomData.DataType type)
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
                case MQB.CustomData.DataType.Custom: return (byte[])value;
                case MQB.CustomData.DataType.Color: return str.ToColor();
                case MQB.CustomData.DataType.Vector3: return str.ToVector3();
                default: throw new NotImplementedException($"Unimplemented custom data type: {type}");
            }
        }

        public static object ToColor(this string str)
        {
            int aStartIndex = str.IndexOf("A=") + 2;
            int rStartIndex = str.IndexOf("R=") + 2;
            int gStartIndex = str.IndexOf("G=") + 2;
            int bStartIndex = str.IndexOf("B=") + 2;

            string aStr = str.Substring(aStartIndex, rStartIndex - aStartIndex - 4);
            string rStr = str.Substring(rStartIndex, gStartIndex - rStartIndex - 4);
            string gStr = str.Substring(gStartIndex, bStartIndex - gStartIndex - 4);
            string bStr = str.Substring(bStartIndex, str.Length - bStartIndex - 1);

            int alpha = int.Parse(aStr);
            int red = int.Parse(rStr);
            int green = int.Parse(gStr);
            int blue = int.Parse(bStr);
            return Color.FromArgb(alpha, red, green, blue);
        }

        #endregion
    }
}
