using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using SoulsFormats;
using WitchyBND.Errors;
using WitchyBND.Services;
using WitchyFormats;
using WitchyLib;
using PARAMDEF = WitchyFormats.PARAMDEF;

namespace WitchyBND.Parsers;

public partial class WPARAM
{
    public override void Unpack(string srcPath, ISoulsFile? file, bool recursive)
    {
        Unpack(srcPath, file, recursive, false);
    }

    public void Unpack(string srcPath, ISoulsFile? file, bool recursive, bool dry, (WBUtil.GameType, ulong)? passedGameInfo = null)
    {
        if (dry && file == null)
        {
            Is(srcPath, null, out file);
        }

        var gameInfo = passedGameInfo ?? gameService.DetermineGameType(srcPath, IGameService.GameDeterminationType.PARAM);
        var game = gameInfo.Item1;
        var regVer = gameInfo.Item2;

        var param = (file as FsParam)!;
        string? paramTypeToParamdef = param.ParamType;
        string paramName = Path.GetFileNameWithoutExtension(srcPath);

        // Fixed cell style for now.
        CellStyle cellStyle = Configuration.Active.ParamCellStyle;

        if (game == WBUtil.GameType.AC6 && string.IsNullOrWhiteSpace(paramTypeToParamdef))
        {
            if (gameService.Ac6TentativeParamTypes.TryGetValue(paramName, out string? newParamType))
            {
                paramTypeToParamdef = newParamType;
            }
            else
            {
                errorService.RegisterError(new WitchyError(
                    @$"No tentative param type alternative found for ""{paramTypeToParamdef}"" -> ""{paramName}"" in {srcPath}.",
                    srcPath));
                return;
            }

            if (string.IsNullOrWhiteSpace(newParamType))
            {
            }
        }

        if (!gameService.ParamdefStorage[game].ContainsKey(paramTypeToParamdef))
        {
            errorService.RegisterError(
                new WitchyError($"Param type {paramTypeToParamdef} not found in {game} paramdefs.",
                    srcPath));
            // Don't hard-fail this because it can happen, just proceed to the next file.
            return;
        }

        PARAMDEF paramdef = gameService.ParamdefStorage[game][paramTypeToParamdef];

        if (param.AppliedParamdef == null)
        {
            try
            {
                if (regVer == 0)
                    param.ApplyParamdef(paramdef);
                else
                    param.ApplyParamdef(paramdef, regVer);
            }
            catch (Exception e)
            {
                if (regVer == 0)
                    errorService.RegisterError(new WitchyError(
                        @$"Could not carefully apply paramdef {paramTypeToParamdef}.
The param may be out of date, or an incorrect regulation version may have been supplied.

The error was:
{e}",
                        srcPath));
                else
                    errorService.RegisterError(new WitchyError(
                        @$"Could not carefully apply paramdef {paramTypeToParamdef}.
The param may be out of date for the regulation version.

The error was:
{e}",
                        srcPath));
                // Nothing happened yet, so can just proceed to the next file.
                return;
            }
        }

        if (dry) return;

        // Begin writing the XML
        var xws = new XmlWriterSettings();
        xws.Indent = true;
        XmlWriter xw = XmlWriter.Create(GetUnpackDestPath(srcPath, recursive), xws);
        xw.WriteStartElement(XmlTag);
        if (Version > 0) xw.WriteAttributeString(VersionAttributeName, Version.ToString());

        // Meta data
        AddLocationToXml(srcPath, recursive, xw);
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

        gameService.PopulateNames(game, paramName);

        var fieldNames = paramdef.Fields.FilterByGameVersion(gameInfo.Item2).Select(field => field.InternalName)
            .ToList();

        var fieldCounts = new Dictionary<string, ConcurrentBag<string>>();
        foreach (string fieldName in fieldNames)
        {
            fieldCounts.TryAdd(fieldName, new());
        }


        var rowDict = new ConcurrentDictionary<long, WPARAMRow>();

        void ParallelCallback(FsParam.Row row, ParallelLoopState state, long i)
        {
            Callback(row, i);
        }

        void Callback(FsParam.Row row, long i)
        {
            int id = row.ID;
            string name = row.Name;
            string paramdexName = null;
            if (string.IsNullOrEmpty(name))
            {
                if (gameService.NameStorage[game][paramName].ContainsKey(id))
                    paramdexName = gameService.NameStorage[game][paramName][id];
            }

            var prepRow = new WPARAMRow
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

                var value = CellValueToString(cell.Value);

                fieldCounts[fieldName].Add(value);

                prepRow.Fields[fieldName] = value;
            }

            rowDict.TryAdd(i, prepRow);
        }

        if (Configuration.Active.Parallel)
        {
            Parallel.ForEach(param.Rows, ParallelCallback);
        }
        else
        {
            for (int i = 0; i < param.Rows.Count; i++)
            {
                Callback(param.Rows[i], i);
            }
        }

        var defaultValues = fieldCounts.ToDictionary(a => a.Key, b => {
            var maxGroup = b.Value.GroupBy(s => s)
                .OrderByDescending(s => s.Count())
                .First();

            return (maxGroup.Key, maxGroup.Count());
        });

        var rows = rowDict.OrderBy(p => p.Key).Select(p => p.Value).ToList();

        int threshold = int.Max((int)(rows.Count * Configuration.Active.ParamDefaultValueThreshold), 100);

        var defaultValuesAboveThreshold = defaultValues.Where(a => a.Value.Item2 >= threshold)
            .ToDictionary(a => a.Key, b => b.Value);

        // Field info (def & default values)
        xw.WriteStartElement("fields");
        foreach (var field in paramdef.Fields.FilterByGameVersion(gameInfo.Item2))
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
            if (Configuration.ParamDefaultValues && defaultValues.TryGetValue(fieldName, out var value))
            {
                xw.WriteAttributeString("defaultValue", value.Key);
                if (defaultValuesAboveThreshold.TryGetValue(fieldName, out _))
                    xw.WriteAttributeString("defaultThreshold", true.ToString());
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

                    var hasDefaultValueAboveThreshold =
                        defaultValuesAboveThreshold.TryGetValue(fieldName, out var defValuePair);
                    if (!(hasDefaultValueAboveThreshold && defValuePair.Key == value))
                    {
                        switch (cellStyle)
                        {
                            case CellStyle.Element:
                                xw.WriteStartElement("field");
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