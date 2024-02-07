using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using SoulsFormats;
using WitchyFormats;
using WitchyLib;
using PARAMDEF = WitchyFormats.PARAMDEF;

namespace WitchyBND.Parsers;

public partial class WPARAM
{
    public override void Repack(string srcPath)
    {
            var param = new FsParam();

            XmlDocument xml = new XmlDocument();
            xml.Load(srcPath);

            Enum.TryParse(xml.SelectSingleNode("param/cellStyle")?.InnerText ?? "None", out CellStyle cellStyle);

            Enum.TryParse(xml.SelectSingleNode("param/compression")?.InnerText ?? "None", out DCX.Type compression);
            param.Compression = compression;

            Enum.TryParse(xml.SelectSingleNode("param/format2D")?.InnerText ?? "0",
                out FsParam.FormatFlags1 formatFlags1);
            param.Format2D = formatFlags1;

            Enum.TryParse(xml.SelectSingleNode("param/format2E")?.InnerText ?? "0",
                out FsParam.FormatFlags2 formatFlags2);
            param.Format2E = formatFlags2;

            param.Unk06 = Convert.ToInt16(xml.SelectSingleNode("param/unk06").InnerText);

            var paramdef = new PARAMDEF();

            paramdef.ParamType = xml.SelectSingleNode("param/paramdef/type")!.InnerText;
            string paramTypeText = xml.SelectSingleNode("param/type")?.InnerText;
            param.ParamType = !string.IsNullOrEmpty(paramTypeText)
                ? paramTypeText
                : paramdef.ParamType;

            var bigEndian =
                Convert.ToBoolean(Convert.ToInt32(xml.SelectSingleNode("param/paramdef/bigEndian").InnerText));
            param.BigEndian = bigEndian;
            paramdef.BigEndian = bigEndian;

            paramdef.Unicode =
                Convert.ToBoolean(Convert.ToInt32(xml.SelectSingleNode("param/paramdef/unicode").InnerText));

            Enum.TryParse(xml.SelectSingleNode("param/paramdef/compression")?.InnerText ?? "None",
                out DCX.Type defCompression);
            paramdef.Compression = defCompression;

            paramdef.DataVersion = Convert.ToInt16(xml.SelectSingleNode("param/paramdef/dataVersion").InnerText);
            string dataVersionText = xml.SelectSingleNode("param/dataVersion")?.InnerText;
            param.ParamdefDataVersion = !string.IsNullOrEmpty(dataVersionText)
                ? Convert.ToInt16(dataVersionText)
                : paramdef.DataVersion;

            paramdef.FormatVersion = Convert.ToInt16(xml.SelectSingleNode("param/paramdef/formatVersion").InnerText);
            string formatVersionText = xml.SelectSingleNode("param/formatVersion")?.InnerText;
            param.ParamdefFormatVersion = !string.IsNullOrEmpty(formatVersionText)
                ? Convert.ToByte(formatVersionText)
                : Convert.ToByte(paramdef.FormatVersion);

            paramdef.Fields = new List<PARAMDEF.Field>();

            var defaultValues = new Dictionary<string, string>();

            foreach (XmlNode xmlRow in xml.SelectNodes("param/fields/field"))
            {
                var field = new PARAMDEF.Field();
                field.InternalName = xmlRow.Attributes["name"].InnerText;

                Enum.TryParse(xmlRow.Attributes["type"].InnerText, out PARAMDEF.DefType displayType);
                field.DisplayType = displayType;

                field.ArrayLength = Convert.ToInt32(xmlRow.Attributes["arraylength"].InnerText);
                field.BitSize = Convert.ToInt32(xmlRow.Attributes["bitsize"].InnerText);
                field.SortID = Convert.ToInt32(xmlRow.Attributes["sortid"].InnerText);
                field.UnkB8 = xmlRow.Attributes["unkb8"].InnerText;
                field.UnkC0 = xmlRow.Attributes["unkc0"].InnerText;

                var defaultValue = xmlRow.Attributes["defaultValue"]?.InnerText ?? string.Empty;
                defaultValues[field.InternalName] = defaultValue;

                paramdef.Fields.Add(field);
            }

            param.ApplyParamdef(paramdef);

            var rowDict = new ConcurrentDictionary<long, FsParam.Row>();

            void ParallelCallback(XmlNode xmlRow, ParallelLoopState state, long i)
            {
                Callback(xmlRow, i);
            }

            void Callback(XmlNode xmlRow, long i)
            {
                var id = int.Parse(xmlRow.Attributes["id"].InnerText);
                var name = xmlRow.Attributes["name"]?.InnerText ?? string.Empty;

                var row = new FsParam.Row(id, name, param);

                var csv = cellStyle == CellStyle.CSV ? WBUtil.DelimitedString.Split(xmlRow.InnerText) : null;

                foreach (FsParam.Column column in param.Columns)
                {
                    string fieldName = column.Def.InternalName;
                    string defaultValue = defaultValues[column.Def.InternalName];
                    string value = defaultValue;
                    switch (cellStyle)
                    {
                        case CellStyle.Element:
                            var fieldNode = xmlRow.SelectSingleNode(fieldName);
                            if (fieldNode != null)
                            {
                                value = fieldNode.InnerText;
                            }

                            break;
                        case CellStyle.Attribute:
                            XmlAttribute attribute = xmlRow.Attributes[fieldName];
                            if (attribute != null)
                            {
                                value = attribute.InnerText;
                            }

                            break;
                        case CellStyle.CSV:
                            var fieldIdx = param.Columns.ToList().IndexOf(column);
                            if (csv != null && csv.Length > fieldIdx)
                            {
                                value = csv[fieldIdx];
                            }

                            break;
                    }

                    if (value == null)
                    {
                        throw new Exception($"Row {id} is missing value for cell {fieldName}.");
                    }

                    column.SetValue(row, ConvertValueFromString(column.Def, StringToCellValue(value, column.Def)));
                }

                rowDict.TryAdd(i, row);
            }
            var rows = xml.SelectNodes("param/rows/row").Cast<XmlNode>().ToList();

            if (Configuration.Parallel)
            {
                Parallel.ForEach(rows, ParallelCallback);
            }
            else
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    Callback(rows[i], i);
                }
            }
            foreach (FsParam.Row row in rowDict.OrderBy(p => p.Key).Select(p => p.Value).ToList())
            {
                param.AddRow(row);
            }

            XElement xelem = LoadXml(srcPath);
            string outPath = GetRepackDestPath(srcPath, xelem);
            WBUtil.Backup(outPath);
            param.TryWriteSoulsFile(outPath);
    }
}