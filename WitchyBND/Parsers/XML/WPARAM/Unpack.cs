using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using PPlus;
using WitchyFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public partial class WPARAM
{
    public override void Unpack(string srcPath)
    {
        if (Game == null)
            Game = WBUtil.DetermineParamdexGame(Path.GetDirectoryName(srcPath), Configuration.Args.Passive);

        if (Game == null)
            throw new InvalidDataException("Could not locate game type of PARAM.");

        var game = Game.Value;

        var param = FsParam.Read(srcPath);
        string paramTypeToParamdef = param.ParamType;
        string paramName = Path.GetFileNameWithoutExtension(srcPath);

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
            PromptPlus.Error.WriteLine($"Param type {paramTypeToParamdef} not found in {game.ToString()} paramdefs.");
            // Don't hard-fail this because it can happen, just proceed to the next file.
            return;
        }

        PARAMDEF paramdef = ParamdefStorage[game][paramTypeToParamdef];

        try
        {
            param.ApplyParamdef(paramdef);
        }
        catch (Exception e)
        {
            PromptPlus.Error.WriteLine(
                $"Could not carefully apply paramdef {paramTypeToParamdef} to {Path.GetFileName(srcPath)}.");
            // Nothing happened yet, so can just proceed to the next file.
            return;
        }

        // Begin writing the XML
        var xws = new XmlWriterSettings();
        xws.Indent = true;
        XmlWriter xw = XmlWriter.Create(GetUnpackDestPath(srcPath), xws);
        xw.WriteStartElement("param");

        // Meta data
        xw.WriteElementString("fileName", Path.GetFileName(srcPath));
        if (!string.IsNullOrEmpty(param.ParamType))
            xw.WriteElementString("type", param.ParamType);
        xw.WriteElementString("game", game.ToString());
        xw.WriteElementString("cellStyle", ((int)cellStyle).ToString());
        xw.WriteElementString("compression", param.Compression.ToString());
        xw.WriteElementString("format2D", ((byte)param.Format2D).ToString());
        xw.WriteElementString("format2E", ((byte)param.Format2E).ToString());
        xw.WriteElementString("dataVersion", param.ParamdefDataVersion.ToString());
        xw.WriteElementString("formatVersion", param.ParamdefFormatVersion.ToString());
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
            if (Configuration.ParamDefaultValues && defaultValues.TryGetValue(fieldName, out string value) && value != null)
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
    }
}