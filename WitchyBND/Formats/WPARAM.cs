using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using SoulsFormats;

namespace WitchyBND
{
    static class WPARAM
    {
        private class YPARAMRow
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public string ParamdexName { get; set; }
            public Dictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();
        }

        /// <summary>
        /// The style in which cells will be read/written.
        /// CSV: Store all row cells as a CSV-style concatenated string with delimiters (min. readability).
        /// Attribute: Store all row cells as attributes on the row element. (readability compromise)
        /// Element: Store all row cells as separate elements (max. readability, max lines).
        /// </summary>
        private enum CellStyle
        {
            CSV,
            Attribute,
            Element
        }

        // Dictionary housing paramdefs for batched usage.
        private static Dictionary<WBUtil.GameType, Dictionary<string, PARAMDEF>> ParamdefStorage { get; set; } =
            new Dictionary<WBUtil.GameType, Dictionary<string, PARAMDEF>>();

        // Dictionary housing param row names for batched usage.
        private static Dictionary<WBUtil.GameType, Dictionary<string, Dictionary<int, string>>>
            NameStorage { get; set; } = new Dictionary<WBUtil.GameType, Dictionary<string, Dictionary<int, string>>>();

        public static bool Unpack(this PARAM param, string sourceFile, string sourceDir, WBUtil.GameType game)
        {
            string paramType = param.ParamType;

            // Fixed cell style for now.
            CellStyle cellStyle = CellStyle.Attribute;

            PopulateParamdef(game);

            if (!ParamdefStorage[game].ContainsKey(paramType))
            {
                Console.WriteLine($"Param type {paramType} not found in {WBUtil.GameNames[game]} paramdefs.");
                // Don't hard-fail this because it can happen, just proceed to the next file.
                return true;
            }

            PARAMDEF paramdef = ParamdefStorage[game][paramType];

            try
            {
                if (!param.ApplyParamdefLessCarefully(paramdef))
                {
                    Console.WriteLine($"Could not carefully apply paramdef {paramType} to {Path.GetFileName(sourceFile)}.");
                    // Nothing happened yet, so can just proceed to the next file.
                    return true;
                }

            }
            catch (Exception e)
            {
                throw new Exception($"Param type {paramType} could not be applied to {Path.GetFileName(sourceFile)}.",
                    e);
            }

            string paramName = Path.GetFileNameWithoutExtension(sourceFile);

            // Begin writing the XML
            var xws = new XmlWriterSettings();
            xws.Indent = true;
            XmlWriter xw = XmlWriter.Create(Path.Combine(sourceDir, $"{sourceFile}.xml"), xws);
            xw.WriteStartElement("param");

            // Meta data
            xw.WriteElementString("fileName", Path.GetFileName(sourceFile));
            xw.WriteElementString("game", WBUtil.GameNames[game]);
            xw.WriteElementString("cellStyle", ((int)cellStyle).ToString());
            xw.WriteElementString("compression", param.Compression.ToString());
            xw.WriteElementString("format2D", ((byte)param.Format2D).ToString());
            xw.WriteElementString("format2E", ((byte)param.Format2E).ToString());
            xw.WriteElementString("unk06", param.Unk06.ToString());

            // Embed paramdef in the XML so it serves as its own paramdef.
            {
                xw.WriteStartElement("paramdef");
                xw.WriteElementString("type", paramdef.ParamType);
                xw.WriteElementString("bigEndian", Convert.ToInt32(paramdef.BigEndian).ToString());
                xw.WriteElementString("compression", paramdef.Compression.ToString());
                xw.WriteElementString("unicode", Convert.ToInt32(paramdef.Unicode).ToString());
                xw.WriteElementString("dataVersion", paramdef.DataVersion.ToString());
                xw.WriteElementString("formatVersion", paramdef.FormatVersion.ToString());
                // xw.WriteElementString("variableEditorValueTypes", paramdef.VariableEditorValueTypes.ToString());
                xw.WriteEndElement();
            }

            // Prepare rows
            var allValues = new Dictionary<string, List<string>>();
            var rows = new List<YPARAMRow>();
            var fieldNames = paramdef.Fields.Select(field => {
                allValues[field.InternalName] = new List<string>();
                return field.InternalName;
            });

            // No row re-sorting to preserve byte perfection
            // param.Rows.Sort((e1, e2) => e1.ID.CompareTo(e2.ID));
            foreach (PARAM.Row row in param.Rows)
            {
                int id = row.ID;
                string name = row.Name;
                string paramdexName = null;
                if (string.IsNullOrEmpty(name))
                {
                    PopulateNames(game, paramName);
                    if (NameStorage[game][paramName].ContainsKey(id))
                        paramdexName = NameStorage[game][paramName][id];
                }

                var prepRow = new YPARAMRow()
                {
                    ID = id,
                    Name = name,
                    ParamdexName = paramdexName,
                };

                foreach (string fieldName in fieldNames)
                {
                    PARAM.Cell cell = row[fieldName];
                    if (cell == null)
                    {
                        throw new Exception($"Row {id} is missing cell {fieldName}.");
                    }

                    var value = CellValueToString(cell);
                    allValues[fieldName].Add(value);
                    prepRow.Fields[fieldName] = value;
                }

                rows.Add(prepRow);
            }

            // Count cell values and create defaults
            var defaultValues = new Dictionary<string, string>();
            foreach (KeyValuePair<string, List<string>> pair in allValues)
            {
                var fieldName = pair.Key;
                var values = pair.Value;

                var maxValue = values.GroupBy(x => x).Select(x => new { x.Key, Count = x.Count() })
                    .OrderByDescending(x => x.Count).First();
                if (maxValue.Count >= pair.Value.Count() * 0.8)
                {
                    defaultValues[fieldName] = maxValue.Key;
                }
            }
            // var fieldValues = allValues.
            // var maxValue = fieldValues.OrderByDescending(pair => pair.Value).First();
            // if (maxValue.Value >= (fieldValues.Count * 0.8))
            // {
            // xw.WriteAttributeString("defaultValue", maxValue.Key);
            // commonFieldValues[field.InternalName] = maxValue.Key;
            // }

            // Field info (def & default values)
            xw.WriteStartElement("fields");
            foreach (var field in paramdef.Fields)
            {
                var fieldName = field.InternalName;

                xw.WriteStartElement("field");
                // xw.WriteAttributeString("idx", paramdef.Fields.IndexOf(field).ToString());
                xw.WriteAttributeString("name", fieldName);
                xw.WriteAttributeString("type", field.DisplayType.ToString());
                // xw.WriteElementString("displayname", field.DisplayName);
                // xw.WriteElementString("displayformat", field.DisplayFormat);
                // xw.WriteElementString("description", field.Description);
                xw.WriteAttributeString("arraylength", field.ArrayLength.ToString());
                xw.WriteAttributeString("bitsize", field.BitSize.ToString());
                xw.WriteAttributeString("sortid", field.SortID.ToString());
                xw.WriteAttributeString("unkb8", field.UnkB8);
                xw.WriteAttributeString("unkc0", field.UnkC0);

                // Store common "default" values
                // var fieldValues = param.Rows.Select(row => row[field.InternalName]).GroupBy(CellValueToString)
                // .ToDictionary(x => x.Key, x => x.Count());
                // var maxValue = fieldValues.OrderByDescending(pair => pair.Value).First();
                // if (maxValue.Value >= (fieldValues.Count * 0.8))
                // {
                // xw.WriteAttributeString("defaultValue", maxValue.Key);
                // commonFieldValues[field.InternalName] = maxValue.Key;
                // }
                if (defaultValues.ContainsKey(fieldName))
                {
                    xw.WriteAttributeString("defaultValue", defaultValues[fieldName]);
                }

                xw.WriteEndElement();
            }

            xw.WriteEndElement();
            // Rows
            xw.WriteStartElement("rows");
            foreach (YPARAMRow row in rows)
            {
                xw.WriteStartElement("row");
                xw.WriteAttributeString("id", row.ID.ToString());
                if (row.Name != null)
                {
                    xw.WriteAttributeString("name", row.Name);
                }
                else if (row.ParamdexName != null)
                {
                    xw.WriteAttributeString("paramdexName", row.ParamdexName);
                }

                var csvValues = new List<string>();
                {
                    foreach (KeyValuePair<string, string> fieldPair in row.Fields)
                    {
                        string fieldName = fieldPair.Key;
                        string value = fieldPair.Value;

                        bool isDefaultValue = defaultValues.ContainsKey(fieldName) &&
                                              defaultValues[fieldName] == value;

                        if (!isDefaultValue)
                        {
                            switch (cellStyle)
                            {
                                case CellStyle.Element:
                                    xw.WriteStartElement("cell");
                                    // xw.WriteAttributeString("idx", paramdef.Fields.FindIndex(field => field.InternalName == cell.Def.InternalName && field.SortID == cell.Def.SortID).ToString());
                                    xw.WriteAttributeString("name", fieldName);
                                    xw.WriteString(value);
                                    xw.WriteEndElement();
                                    break;
                                case CellStyle.Attribute:
                                    xw.WriteAttributeString(fieldName, value);
                                    break;
                                case CellStyle.CSV:
                                    csvValues.Add(value);
                                    break;
                            }
                        }
                    }

                    if (cellStyle == CellStyle.CSV)
                        xw.WriteString(WBUtil.DelimitedString.Join(csvValues));
                }
                xw.WriteEndElement();
            }

            xw.WriteEndElement();

            xw.WriteEndElement();
            xw.Close();

            return false;
        }

        public static bool Repack(string sourceFile, string sourceDir)
        {
            var param = new PARAM();

            XmlDocument xml = new XmlDocument();
            xml.Load(sourceFile);

            Enum.TryParse(xml.SelectSingleNode("param/cellStyle")?.InnerText ?? "None", out CellStyle cellStyle);

            // Enum.TryParse(xml.SelectSingleNode("param/game")?.InnerText ?? "", out WBUtil.GameType game);
            Enum.TryParse(xml.SelectSingleNode("param/compression")?.InnerText ?? "None", out DCX.Type compression);
            param.Compression = compression;

            Enum.TryParse(xml.SelectSingleNode("param/format2D")?.InnerText ?? "0",
                out PARAM.FormatFlags1 formatFlags1);
            param.Format2D = formatFlags1;

            Enum.TryParse(xml.SelectSingleNode("param/format2E")?.InnerText ?? "0",
                out PARAM.FormatFlags2 formatFlags2);
            param.Format2E = formatFlags2;

            param.Unk06 = Convert.ToInt16(xml.SelectSingleNode("param/unk06").InnerText);

            // PopulateParamdef(game);
            //
            // XmlNode paramTypeNode = xml.SelectSingleNode("param/type");
            // if (paramTypeNode == null)
            // {
            //     throw new Exception("Param XML does not contain type.");
            // }
            //
            // string paramType = paramTypeNode.InnerText;
            //
            // if (!ParamdefStorage[game].ContainsKey(paramType))
            // {
            //     throw new Exception($"Paramdef does not contain param type {paramType}.");
            // }

            // PARAMDEF paramdef = ParamdefStorage[game][paramType];

            var paramdef = new PARAMDEF();

            var paramType = xml.SelectSingleNode("param/paramdef/type").InnerText;
            param.ParamType = paramType;
            paramdef.ParamType = paramType;

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
            param.ParamdefDataVersion = paramdef.DataVersion;

            paramdef.FormatVersion = Convert.ToInt16(xml.SelectSingleNode("param/paramdef/formatVersion").InnerText);
            // param.ParamdefFormatVersion = (byte)paramdef.FormatVersion;

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

                var defaultValue = xmlRow.Attributes["defaultValue"]?.InnerText ?? "";
                defaultValues[field.InternalName] = defaultValue;

                paramdef.Fields.Add(field);
            }

            foreach (XmlNode xmlRow in xml.SelectNodes("param/rows/row"))
            {
                var id = int.Parse(xmlRow.Attributes["id"].InnerText);
                var name = xmlRow.Attributes["name"]?.InnerText ?? "";

                var row = new PARAM.Row(id, name, paramdef);

                var csv = cellStyle == CellStyle.CSV ? WBUtil.DelimitedString.Split(xmlRow.InnerText) : null;

                foreach (PARAMDEF.Field field in paramdef.Fields)
                {
                    string fieldName = field.InternalName;
                    string defaultValue = defaultValues[field.InternalName];
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
                            var fieldIdx = paramdef.Fields.IndexOf(field);
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

                    row[fieldName].Value = StringToCellValue(value, field);
                }

                param.Rows.Add(row);
            }

            try
            {
                // We're creating a new param, so applying carefully is not an option due to
                // missing values auto-generated during Write.
                // param.ApplyParamdef(paramdef);
                if (!param.ApplyParamdefLessCarefully(paramdef))
                {
                    throw new Exception($"Paramdef did not apply carefully");
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Could not parse param {sourceFile} with paramdef {paramType}.", e);
            }

            string outPath = Path.Combine(sourceDir, sourceFile.Replace(".param.xml", ".param"));
            WBUtil.Backup(outPath);
            param.Write(outPath);

            return false;
        }

        public static void PopulateParamdef(WBUtil.GameType game)
        {
            if (ParamdefStorage[game].Count > 0)
                return;

            var gameName = WBUtil.GameNames[game];
            var paramdefPath = $@"{WBUtil.GetExeLocation()}\Assets\Paramdex\{gameName}\Defs";

            if (!Directory.Exists(paramdefPath))
            {
                throw new Exception($"Paramdef path not found for {gameName}.");
            }

            var patchParamdefPaths = Directory.GetDirectories($@"{WBUtil.GetExeLocation()}\Assets\Paramdex\{gameName}\DefsPatch").OrderByDescending(path => path);

            // Reading XML paramdefs
            foreach (string path in Directory.GetFiles(paramdefPath, "*.xml"))
            {
                PARAMDEF paramdef = PARAMDEF.XmlDeserialize(path);

                foreach (string patchPath in patchParamdefPaths)
                {
                    var patchParamdefPath = $@"{patchPath}\{Path.GetFileName(path)}";
                    if (!File.Exists(patchParamdefPath)) continue;
                    PARAMDEF? patchParamdef = PARAMDEF.XmlDeserialize(patchParamdefPath);
                    if (patchParamdef == null) continue;
                    paramdef = patchParamdef;
                    break;
                }

                var dupes = paramdef.Fields.GroupBy(x => x.InternalName).Where(x => x.Count() > 1)
                    .ToDictionary(x => x.Key, x => x.ToList());
                foreach (KeyValuePair<string, List<PARAMDEF.Field>> pair in dupes)
                {
                    foreach (var dupe in pair.Value)
                    {
                        dupe.InternalName += $"_sid{dupe.SortID}";
                    }
                    // for (int i = 0; i < pair.Value.Count(); i++)
                    // {
                    // pair.Value[i].InternalName += $"_xid{i}";
                    // }
                }

                ParamdefStorage[game][paramdef.ParamType] = paramdef;
            }
        }

        public static void PopulateNames(WBUtil.GameType game, string paramName)
        {
            if (NameStorage[game].ContainsKey(paramName) && NameStorage[game][paramName].Count > 0)
                return;

            var gameName = WBUtil.GameNames[game];
            var namePath = $@"{WBUtil.GetExeLocation()}\Assets\Paramdex\{gameName}\Names\{paramName}.txt";

            if (!File.Exists(namePath))
            {
                // Quietly fail, it's just names after all.
                Console.WriteLine($"Could not find names for {gameName} param {paramName} in Paramdex.");
                // Write something to the storage so the population process isn't repeated.
                NameStorage[game][paramName] = new Dictionary<int, string>()
                {
                    { -9000, "" }
                };
                return;
            }

            var names = File.ReadAllLines(namePath);
            // var dupes = names.GroupBy(name => name.Split(' ')[0]).Where(x => x.Count() > 1).OrderByDescending(x => x.Count()).ToDictionary(x => x.Key, x => x.Count());
            NameStorage[game][paramName] = names.ToDictionary(name => { return int.Parse(name.Split(' ')[0]); },
                name => string.Join(" ", name.Split(' ').Skip(1)));
        }

        public static string CellValueToString(PARAM.Cell cell)
        {
            var value = cell.Value.ToString();
            if (cell.Def.DisplayType == PARAMDEF.DefType.dummy8)
            {
                byte[] bytes;
                if (cell.Value.GetType() == typeof(byte))
                {
                    bytes = new[] { (byte)cell.Value };
                }
                else
                {
                    bytes = (byte[])cell.Value;
                }

                value = $"[{String.Join("|", bytes.Select(myByte => myByte.ToString()))}]";
            }

            return value;
        }

        public static object StringToCellValue(string valueString, PARAMDEF.Field pdefField)
        {
            object value;
            if (pdefField.DisplayType == PARAMDEF.DefType.dummy8)
            {
                var bytes = valueString.Substring(0, valueString.Length - 1).Substring(1).Split('|')
                    .Select(byteString => Convert.ToByte(byteString));

                if (pdefField.BitSize == -1)
                {
                    value = bytes.ToArray();
                }
                else
                {
                    value = bytes.First();
                }
            }
            else
            {
                value = valueString;
            }

            return value;
        }

        static WPARAM()
        {
            foreach (WBUtil.GameType game in (WBUtil.GameType[])Enum.GetValues(typeof(WBUtil.GameType)))
            {
                ParamdefStorage[game] = new Dictionary<string, PARAMDEF>();
                NameStorage[game] = new Dictionary<string, Dictionary<int, string>>();
            }
        }
    }
}