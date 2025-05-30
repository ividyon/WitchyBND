using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using SoulsFormats;
using WitchyFormats;
using WitchyLib;
using PARAMDEF = WitchyFormats.PARAMDEF;

namespace WitchyBND.Parsers;

public partial class WPARAM
{
    public override void Repack(string srcPath, bool recursive)
    {
            var param = new FsParam();

            XElement xml = LoadXml(srcPath);

            Enum.TryParse(xml.Element("cellStyle")?.Value ?? "None", out CellStyle cellStyle);

            param.Compression = ReadCompressionInfoFromXml(xml);

            Enum.TryParse(xml.Element("format2D")?.Value ?? "0",
                out FsParam.FormatFlags1 formatFlags1);
            param.Format2D = formatFlags1;

            Enum.TryParse(xml.Element("format2E")?.Value ?? "0",
                out FsParam.FormatFlags2 formatFlags2);
            param.Format2E = formatFlags2;

            param.Unk06 = Convert.ToInt16(xml.Element("unk06")!.Value);

            var paramdef = new PARAMDEF();

            var paramdefXml = xml.Element("paramdef")!;

            paramdef.ParamType = paramdefXml.Element("type")!.Value;
            string? paramTypeText = xml.Element("type")?.Value;
            param.ParamType = !string.IsNullOrEmpty(paramTypeText)
                ? paramTypeText
                : paramdef.ParamType;

            var bigEndian =
                Convert.ToBoolean(Convert.ToInt32(paramdefXml.Element("bigEndian")!.Value));
            param.BigEndian = bigEndian;
            paramdef.BigEndian = bigEndian;

            paramdef.Unicode =
                Convert.ToBoolean(Convert.ToInt32(paramdefXml.Element("unicode")!.Value));

            paramdef.Compression = ReadCompressionInfoFromXml(paramdefXml);

            paramdef.DataVersion = Convert.ToInt16(paramdefXml.Element("dataVersion")!.Value);
            string? dataVersionText = xml.Element("dataVersion")?.Value;
            param.ParamdefDataVersion = !string.IsNullOrEmpty(dataVersionText)
                ? Convert.ToInt16(dataVersionText)
                : paramdef.DataVersion;

            paramdef.FormatVersion = Convert.ToInt16(paramdefXml.Element("formatVersion")!.Value);
            string? formatVersionText = xml.Element("formatVersion")?.Value;
            param.ParamdefFormatVersion = !string.IsNullOrEmpty(formatVersionText)
                ? Convert.ToByte(formatVersionText)
                : Convert.ToByte(paramdef.FormatVersion);

            paramdef.Fields = new List<PARAMDEF.Field>();

            var defaultValues = new Dictionary<string, string?>();

            foreach (XElement xmlRow in xml.Element("fields")!.Elements("field"))
            {
                var field = new PARAMDEF.Field();
                field.InternalName = xmlRow.Attribute("name")!.Value;

                Enum.TryParse(xmlRow.Attribute("type")!.Value, out PARAMDEF.DefType displayType);
                field.DisplayType = displayType;

                field.ArrayLength = Convert.ToInt32(xmlRow.Attribute("arraylength")!.Value);
                field.BitSize = Convert.ToInt32(xmlRow.Attribute("bitsize")!.Value);
                field.SortID = Convert.ToInt32(xmlRow.Attribute("sortid")!.Value);
                field.UnkB8 = xmlRow.Attribute("unkb8")?.Value;
                field.UnkC0 = xmlRow.Attribute("unkc0")?.Value;

                defaultValues[field.InternalName] = xmlRow.Attribute("defaultValue")?.Value;

                paramdef.Fields.Add(field);
            }

            param.ApplyParamdef(paramdef);

            var rowDict = new ConcurrentDictionary<long, FsParam.Row>();
            var colList = param.Columns.ToList();

            void ParallelCallback(XElement xmlRow, ParallelLoopState state, long i)
            {
                Callback(xmlRow, i);
            }

            void Callback(XElement xmlRow, long i)
            {
                var id = int.Parse(xmlRow.Attribute("id")!.Value);
                var name = xmlRow.Attribute("name")?.Value ?? string.Empty;

                var row = new FsParam.Row(id, name, param);

                var csv = cellStyle == CellStyle.CSV ? WBUtil.DelimitedString.Split(xmlRow.Value) : null;

                foreach (FsParam.Column column in colList)
                {
                    string fieldName = column.Def.InternalName;
                    string defaultValue = defaultValues[column.Def.InternalName];
                    string? value = defaultValue;
                    switch (cellStyle)
                    {
                        case CellStyle.Element:
                            XElement? fieldNode = xmlRow.XPathSelectElement($"field[@name = '{fieldName}']");
                            if (fieldNode != null)
                            {
                                value = fieldNode.Value;
                            }

                            break;
                        case CellStyle.Attribute:
                            XAttribute? attribute = xmlRow.Attribute(fieldName);
                            if (attribute != null)
                            {
                                value = attribute.Value;
                            }

                            break;
                        case CellStyle.CSV:
                            var fieldIdx = colList.IndexOf(column);
                            if (csv != null && csv.Length > fieldIdx)
                            {
                                value = csv[fieldIdx];
                            }

                            break;
                    }

                    if (value == null || (string.IsNullOrWhiteSpace(value) && column.Def.DisplayType != PARAMDEF.DefType.fixstr && column.Def.DisplayType != PARAMDEF.DefType.fixstrW))
                    {
                        throw new Exception($"Row {id} {name} is missing value for cell {fieldName}.");
                    }

                    row[column.Def.InternalName]!.Value.SetValue(StringToCellValue(column.Def, value));
                }

                rowDict.TryAdd(i, row);
            }
            var rows = xml.Element("rows")!.Elements("row").ToList();

            // if (Configuration.Active.Parallel)
            // {
                // Parallel.ForEach(rows, ParallelCallback);
            // }
            // else
            // {
                for (int i = 0; i < rows.Count; i++)
                {
                    Callback(rows[i], i);
                }
            // }

            foreach (FsParam.Row row in rowDict.OrderBy(p => p.Key).Select(p => p.Value).ToList())
            {
                param.AddRow(row);
            }

            XElement xelem = LoadXml(srcPath);
            string outPath = GetRepackDestPath(srcPath, xelem);
            Backup(outPath);
            param.TryWriteSoulsFile(outPath);
    }
}