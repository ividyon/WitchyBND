using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml;
using SoulsFormats;
using WitchyFormats;
using WitchyLib;

namespace WitchyBND
{
    static class WPARAM
    {
        private class WPARAMRow
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
        private static Dictionary<WBUtil.GameType, Dictionary<string, PARAMDEF>> ParamdefStorage { get; } = new();

        // Dictionary housing param row names for batched usage.
        private static Dictionary<WBUtil.GameType, Dictionary<string, Dictionary<int, string>>>
            NameStorage { get; } = new();

        // Temporary mapping of param file names to AC6 param types.
        private static readonly Dictionary<string, string> ac6ParamMappings = new()
        {
            { "EquipParamWeapon", "EquipParamWeapon_TENTATIVE" }, //
            { "EquipParamProtector", "EQUIP_PARAM_PROTECTOR_ST" },
            { "EquipParamAccessory", "EQUIP_PARAM_ACCESSORY_ST" },
            { "ReinforceParamProtector", "REINFORCE_PARAM_PROTECTOR_ST" },
            { "NpcParam", "NPC_PARAM_ST" },
            { "NpcTransformParam", "NpcTransformParam_TENTATIVE" }, //
            { "AtkParam_Npc", "ATK_PARAM_ST" },
            { "WepAbsorpPosParam", "WEP_ABSORP_POS_PARAM_ST" },
            { "DirectionCameraParam", "DIRECTION_CAMERA_PARAM_ST" },
            { "MovementAcTypeParam", "MovementAcTypeParam_TENTATIVE" }, //
            { "MovementRideObjParam", "MovementRideObjParam_TENTATIVE" }, //
            { "MovementFlyEnemyParam", "MovementFlyEnemyParam_TENTATIVE" }, //
            { "ChrModelParam", "CHR_MODEL_PARAM_ST" },
            { "MissionParam", "MissionParam_TENTATIVE" }, //
            { "MailParam", "MAIL_PARAM_ST" },
            { "EquipParamBooster", "EquipParamBooster_TENTATIVE" }, //
            { "EquipParamGenerator", "EquipParamGenerator_TENTATIVE" }, //
            { "EquipParamFcs", "EquipParamFcs_TENTATIVE" }, //
            { "RuntimeSoundParam_Npc", "RuntimeSoundParam_TENTATIVE" }, //
            { "RuntimeSoundParam_Pc", "RuntimeSoundParam_TENTATIVE" }, //
            { "CutsceneGparamTimeParam", "CUTSCENE_GPARAM_TIME_PARAM_ST" },
            { "CutsceneTimezoneConvertParam", "CUTSCENE_TIMEZONE_CONVERT_PARAM_ST" },
            { "CutsceneMapIdParam", "CUTSCENE_MAP_ID_PARAM_ST" },
            { "MapAreaParam", "MapAreaParam_TENTATIVE" }, //
            { "RuntimeSoundGlobalParam", "RuntimeSoundGlobalParam_TENTATIVE" }, //
        };

        public static bool Unpack(this FsParam param, string sourceFile, string sourceDir, WBUtil.GameType game)
        {
            string paramTypeToParamdef = param.ParamType;
            string paramName = Path.GetFileNameWithoutExtension(sourceFile);

            // Fixed cell style for now.
            CellStyle cellStyle = CellStyle.Attribute;

            PopulateParamdef(game);

            // Temporary solution to handle AC6 paramdefs without paramtype.
            if (string.IsNullOrWhiteSpace(paramTypeToParamdef) &&
                ac6ParamMappings.TryGetValue(paramName, out string newParamType))
            {
                paramTypeToParamdef = newParamType;
            }

            if (!ParamdefStorage[game].ContainsKey(paramTypeToParamdef))
            {
                Console.WriteLine($"Param type {paramTypeToParamdef} not found in {WBUtil.GameNames[game]} paramdefs.");
                // Don't hard-fail this because it can happen, just proceed to the next file.
                return true;
            }

            PARAMDEF paramdef = ParamdefStorage[game][paramTypeToParamdef];

            try
            {
                param.ApplyParamdef(paramdef);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not carefully apply paramdef {paramTypeToParamdef} to {Path.GetFileName(sourceFile)}.");
                // Nothing happened yet, so can just proceed to the next file.
                return true;
            }

            // Begin writing the XML
            var xws = new XmlWriterSettings();
            xws.Indent = true;
            XmlWriter xw = XmlWriter.Create(Path.Combine(sourceDir, $"{sourceFile}.xml"), xws);
            xw.WriteStartElement("param");

            // Meta data
            xw.WriteElementString("fileName", Path.GetFileName(sourceFile));
            if (!string.IsNullOrEmpty(param.ParamType))
                xw.WriteElementString("type", param.ParamType);
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
            var rows = new List<WPARAMRow>();
            var fieldNames = paramdef.Fields.Select(field => field.InternalName);

            var fieldCounts = new Dictionary<string, Dictionary<string, int>>();
            var fieldMaxes = new Dictionary<string, (string, int)>();

            foreach (FsParam.Row row in param.Rows)
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

                var prepRow = new WPARAMRow()
                {
                    ID = id,
                    Name = name,
                    ParamdexName = paramdexName,
                };

                foreach (string fieldName in fieldNames)
                {
                    FsParam.Cell? cell = row[fieldName];
                    if (cell == null)
                    {
                        throw new Exception($"Row {id} is missing cell {fieldName}.");
                    }

                    if (!fieldCounts.ContainsKey(fieldName))
                        fieldCounts[fieldName] = new Dictionary<string, int>();

                    var value = CellValueToString(cell.Value);

                    if (!fieldCounts[fieldName].ContainsKey(value))
                        fieldCounts[fieldName][value] = 0;
                    fieldCounts[fieldName][value]++;

                    if (!fieldMaxes.ContainsKey(fieldName) ||
                        fieldCounts[fieldName][value] > fieldMaxes[fieldName].Item2)
                        fieldMaxes[fieldName] = (value, fieldCounts[fieldName][value]);

                    prepRow.Fields[fieldName] = value;
                }

                rows.Add(prepRow);
            }

            int threshold = (int)(rows.Count * 0.6);
            string GetMajorityValue(KeyValuePair<string, int> c) => c.Value > threshold ? c.Key : null;
            var defaultValues =
                fieldCounts.ToDictionary(e => e.Key, e => GetMajorityValue(e.Value.MaxBy(c => c.Value)));

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
                if (defaultValues.TryGetValue(fieldName, out string value) && value != null)
                {
                    xw.WriteAttributeString("defaultValue", value);
                }

                xw.WriteEndElement();
            }

            xw.WriteEndElement();
            // Rows
            xw.WriteStartElement("rows");
            foreach (WPARAMRow row in rows)
            {
                xw.WriteStartElement("row");
                xw.WriteAttributeString("id", row.ID.ToString());
                if (!string.IsNullOrEmpty(row.Name))
                {
                    xw.WriteAttributeString("name", row.Name);
                }
                else if (!string.IsNullOrEmpty(row.ParamdexName))
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
            var param = new FsParam();

            XmlDocument xml = new XmlDocument();
            xml.Load(sourceFile);

            Enum.TryParse(xml.SelectSingleNode("param/cellStyle")?.InnerText ?? "None", out CellStyle cellStyle);

            param.ParamType = xml.SelectSingleNode("param/type")?.InnerText;

            // Enum.TryParse(xml.SelectSingleNode("param/game")?.InnerText ?? "", out WBUtil.GameType game);
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

            var paramdefType = xml.SelectSingleNode("param/paramdef/type").InnerText;
            paramdef.ParamType = paramdefType;

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

                var defaultValue = xmlRow.Attributes["defaultValue"]?.InnerText ?? string.Empty;
                defaultValues[field.InternalName] = defaultValue;

                paramdef.Fields.Add(field);
            }

            param.ApplyParamdef(paramdef);

            foreach (XmlNode xmlRow in xml.SelectNodes("param/rows/row"))
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

                param.AddRow(row);
            }

            // try
            // {
            //     // We're creating a new param, so applying carefully is not an option due to
            //     // missing values auto-generated during Write.
            //     if (!param.ApplyParamdefLessCarefully(paramdef))
            //     {
            //         throw new Exception($"Paramdef did not apply carefully");
            //     }
            // }
            // catch (Exception e)
            // {
            //     throw new Exception($"Could not parse param {sourceFile} with paramdef {paramType}.", e);
            // }

            string outPath = Path.Combine(sourceDir, sourceFile.Replace(".param.xml", ".param"));
            WBUtil.Backup(outPath);
            param.TryWriteSoulsFile(outPath);

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

            List<string> patchParamdefPaths = new();

            var defsPatchPath = $@"{WBUtil.GetExeLocation()}\Assets\Paramdex\{gameName}\DefsPatch";
            if (Directory.Exists(defsPatchPath))
            {
                patchParamdefPaths = Directory.GetDirectories(defsPatchPath).OrderByDescending(path => path).ToList();
            }

            // Reading XML paramdefs
            foreach (string path in Directory.GetFiles(paramdefPath, "*.xml"))
            {
                PARAMDEF paramdef = PARAMDEF.XmlDeserialize(path);

                foreach (string patchPath in patchParamdefPaths)
                {
                    var patchParamdefPath = $@"{patchPath}\{Path.GetFileName(path)}";
                    if (!File.Exists(patchParamdefPath)) continue;
                    var patchParamdef = PARAMDEF.XmlDeserialize(patchParamdefPath);
                    if (patchParamdef == null) continue;
                    paramdef = patchParamdef;
                    break;
                }

                var dupes = paramdef.Fields.GroupBy(x => x.InternalName).Where(x => x.Count() > 1)
                    .ToDictionary(x => x.Key, x => x.ToList());
                foreach (KeyValuePair<string, List<PARAMDEF.Field>> pair in dupes)
                {
                    // foreach (var dupe in pair.Value)
                    // {
                        // dupe.InternalName += $"_sid{dupe.SortID}";
                    // }
                    // for (int i = 0; i < pair.Value.Count(); i++)
                    // {
                    //     PARAMDEF.Field fieldDef = pair.Value[i];
                    //     fieldDef.InternalName += $"_xid{i}";
                    // }
                    for (var i = 0; i < pair.Value.Count; i++)
                    {
                        int offset = 0;
                        PARAMDEF.Field fieldDef = pair.Value[i];
                        var index = paramdef.Fields.IndexOf(fieldDef);
                        for (int j = 0; j < index; j++)
                        {
                            var prevDef = paramdef.Fields[j];
                            var type = WBUtil.TypeForParamDefType(prevDef.DisplayType, prevDef.ArrayLength > 1);
                            if (type == typeof(byte[]))
                            {
                                offset += prevDef.ArrayLength;
                            }
                            else
                                offset += Marshal.SizeOf(type);
                        }
                        fieldDef.InternalName += $"_offset{offset}";
                    }
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
                    { -9000, string.Empty }
                };
                return;
            }

            var nameDict = new Dictionary<int, string>();
            var names = File.ReadAllLines(namePath);
            foreach (string name in names)
            {
                var splitted = name.Split(' ', 2);
                try
                {
                    var result = nameDict.TryAdd(int.Parse(splitted[0]), splitted[1]);
                    if (result == false)
                    {
                        Console.WriteLine($"Paramdex: Duplicate name for ID {splitted[0]}");
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidDataException($"There was something wrong with the Paramdex names at \"{name}\"",
                        e);
                }
            }

            NameStorage[game][paramName] = nameDict;
        }

        public static Byte[] Dummy8Read(string dummy8, int expectedLength)
        {
            Byte[] nval = new Byte[expectedLength];
            if (!(dummy8.StartsWith('[') && dummy8.EndsWith(']')))
                return null;
            string[] spl = dummy8.Substring(1, dummy8.Length - 2).Split('|');
            if (nval.Length != spl.Length)
            {
                return null;
            }

            for (int i = 0; i < nval.Length; i++)
            {
                if (!byte.TryParse(spl[i], out nval[i]))
                    return null;
            }

            return nval;
        }

        public static string CellValueToString(FsParam.Cell cell)
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
                    if (bytes.Count() == 1)
                        value = bytes.First();
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

        public static object ConvertValueFromString(PARAMDEF.Field def, object value)
        {
            if (value == null)
                throw new NullReferenceException($"Cell value may not be null.");

            switch (def.DisplayType)
            {
                case PARAMDEF.DefType.s8: return Convert.ToSByte(value);
                case PARAMDEF.DefType.u8: return Convert.ToByte(value);
                case PARAMDEF.DefType.s16: return Convert.ToInt16(value);
                case PARAMDEF.DefType.u16: return Convert.ToUInt16(value);
                case PARAMDEF.DefType.s32: return Convert.ToInt32(value);
                case PARAMDEF.DefType.u32: return Convert.ToUInt32(value);
                case PARAMDEF.DefType.b32: return Convert.ToInt32(value);
                case PARAMDEF.DefType.f32: return Convert.ToSingle(value);
                case PARAMDEF.DefType.angle32: return Convert.ToSingle(value);
                case PARAMDEF.DefType.f64: return Convert.ToDouble(value);
                case PARAMDEF.DefType.fixstr: return Convert.ToString(value);
                case PARAMDEF.DefType.fixstrW: return Convert.ToString(value);
                case PARAMDEF.DefType.dummy8:
                    if (def.ArrayLength > 1)
                        return (byte[])value;
                    return Convert.ToByte(value);

                default:
                    throw new NotImplementedException($"Conversion not specified for type {def.DisplayType}");
            }
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