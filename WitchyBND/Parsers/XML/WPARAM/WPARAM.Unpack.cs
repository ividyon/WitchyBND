using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using SoulsFormats;
using WitchyBND.Errors;
using WitchyBND.Services;
using WitchyFormats;
using WitchyLib;

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
        string paramName = OSPath.GetFileNameWithoutExtension(srcPath);

        // Fixed cell style for now.
        CellStyle cellStyle = Configuration.Active.ParamCellStyle;

        if (game == WBUtil.GameType.AC6)
        {
            if (gameService.Ac6TentativeParamTypes.TryGetValue(paramName, out string? newParamType))
            {
                paramTypeToParamdef = newParamType;
            }
            else if (string.IsNullOrWhiteSpace(paramTypeToParamdef))
            {
                errorService.RegisterError(new WitchyError(
                    @$"No tentative param type alternative found for ""{paramTypeToParamdef}"" -> ""{paramName}"" in {srcPath}.",
                    srcPath));
                return;
            }
        }

        if (string.IsNullOrWhiteSpace(paramTypeToParamdef))
        {
            errorService.RegisterError($"Could not determine param type of param {paramName}. It may contain no rows.");
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
        var xml = PrepareXmlManifest(srcPath, recursive, false, param.Compression, out XDocument xDoc, null);
        
        // Meta data
        if (!string.IsNullOrEmpty(param.ParamType))
            xml.Add(new XElement("type", param.ParamType));

        xml.Add(new XElement("game", game.ToString()));


        xml.Add(new XElement("cellStyle", ((int)cellStyle).ToString()));
        xml.Add(new XElement("format2D", ((byte)param.Format2D).ToString()));
        xml.Add(new XElement("format2E", ((byte)param.Format2E).ToString()));
        xml.Add(new XElement("dataVersion", param.ParamdefDataVersion.ToString()));
        xml.Add(new XElement("formatVersion", param.ParamdefFormatVersion.ToString()));
        xml.Add(new XElement("unk06", param.Unk06.ToString()));

        // Embed paramdef in the XML so it serves as its own paramdef.
        {
            var paramdefXml = new XElement("paramdef");
            xml.Add(paramdefXml);
            paramdefXml.Add(new XElement("type", paramdef.ParamType));
            paramdefXml.Add(new XElement("bigEndian", Convert.ToInt32(paramdef.BigEndian).ToString()));
            WriteCompressionInfoToXml(paramdefXml, paramdef.Compression);
            paramdefXml.Add(new XElement("unicode", Convert.ToInt32(paramdef.Unicode).ToString()));
            paramdefXml.Add(new XElement("dataVersion", paramdef.DataVersion.ToString()));
            paramdefXml.Add(new XElement("formatVersion", paramdef.FormatVersion.ToString()));
            // paramdefXml.Add(new XElement("variableEditorValueTypes", paramdef.VariableEditorValueTypes.ToString()));
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
        var fieldsXml = new XElement("fields");
        xml.Add(fieldsXml);
        foreach (var field in paramdef.Fields.FilterByGameVersion(gameInfo.Item2))
        {
            var fieldName = field.InternalName;

            var fieldXml = new XElement("field");
            fieldsXml.Add(fieldXml);
            // fieldXml.Add(new XAttribute("idx", paramdef.Fields.IndexOf(field).ToString()));
            fieldXml.Add(new XAttribute("name", fieldName));
            fieldXml.Add(new XAttribute("type", field.DisplayType.ToString()));
            // fieldXml.Add(new XElement("displayname", field.DisplayName));
            // fieldXml.Add(new XElement("displayformat", field.DisplayFormat));
            // fieldXml.Add(new XElement("description", field.Description));
            fieldXml.Add(new XAttribute("arraylength", field.ArrayLength.ToString()));
            fieldXml.Add(new XAttribute("bitsize", field.BitSize.ToString()));
            fieldXml.Add(new XAttribute("sortid", field.SortID.ToString()));
            if (field.UnkB8 != null)
                fieldXml.Add(new XAttribute("unkb8", field.UnkB8));
            if (field.UnkC0 != null)
                fieldXml.Add(new XAttribute("unkc0", field.UnkC0));

            // Store common "default" values
            if (Configuration.ParamDefaultValues && defaultValues.TryGetValue(fieldName, out var value))
            {
                fieldXml.Add(new XAttribute("defaultValue", value.Key));
                if (defaultValuesAboveThreshold.TryGetValue(fieldName, out _))
                    fieldXml.Add(new XAttribute("defaultThreshold", true.ToString()));
            }
        }
        // Rows
        var rowsXml = new XElement("rows");
        xml.Add(rowsXml);

        foreach (WPARAMRow row in rows)
        {
            var rowXml = new XElement("row");
            rowsXml.Add(rowXml);
            rowXml.Add(new XAttribute("id", row.ID.ToString()));
            if (!string.IsNullOrEmpty(row.Name))
            {
                rowXml.Add(new XAttribute("name", row.Name));
            }
            else if (!string.IsNullOrEmpty(row.ParamdexName))
            {
                rowXml.Add(new XAttribute("paramdexName", row.ParamdexName));
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
                                var fieldXml = new XElement("field");
                                rowXml.Add(fieldXml);
                                fieldXml.Add(new XAttribute("name", fieldName));
                                fieldXml.Value = value;
                                break;
                            case CellStyle.Attribute:
                                rowXml.Add(new XAttribute(fieldName, value));
                                break;
                            case CellStyle.CSV:
                                csvValues.Add(value);
                                break;
                        }
                    }
                }

                if (cellStyle == CellStyle.CSV)
                    rowXml.Value = WBUtil.DelimitedString.Join(csvValues);
            }
        }
        
        WriteXmlManifest(xDoc, srcPath, recursive, false);
    }
}