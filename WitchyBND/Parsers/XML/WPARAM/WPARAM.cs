using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml;
using PPlus;
using SoulsFormats;
using WitchyFormats;
using WitchyLib;
using PARAM = WitchyFormats.PARAM;
using PARAMDEF = WitchyFormats.PARAMDEF;

namespace WitchyBND.Parsers;

public partial class WPARAM : WXMLParser
{
    public WBUtil.GameType? Game;

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

    public override string Name => "PARAM";

    public override bool Is(string path)
    {
        return Path.GetExtension(path) == ".param";
    }

    public static void PopulateParamdef(WBUtil.GameType game)
    {
        if (ParamdefStorage[game].Count > 0)
            return;

        var gameName = game.ToString();
        var paramdefPath = $@"{WBUtil.GetExeLocation()}\Assets\Paramdex\{gameName}\Defs";

        if (!Directory.Exists(paramdefPath))
        {
            throw new Exception($"Paramdef path not found for {gameName}.");
        }

        // Reading XML paramdefs
        foreach (string path in Directory.GetFiles(paramdefPath, "*.xml"))
        {
            PARAMDEF paramdef = PARAMDEF.XmlDeserialize(path);

            var dupes = paramdef.Fields.GroupBy(x => x.InternalName).Where(x => x.Count() > 1)
                .ToDictionary(x => x.Key, x => x.ToList());
            foreach (KeyValuePair<string, List<PARAMDEF.Field>> pair in dupes)
            {
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

        var gameName = game.ToString();
        var namePath = $@"{WBUtil.GetExeLocation()}\Assets\Paramdex\{gameName}\Names\{paramName}.txt";

        if (!File.Exists(namePath))
        {
            // Write something to the storage so the population process isn't repeated.
            NameStorage[game][paramName] = new Dictionary<int, string>()
            {
                { -9000, string.Empty }
            };
            // Quietly fail, it's just names after all.
            Program.RegisterNotice($"Could not find names for {gameName} param {paramName} in Paramdex.");
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
                    Program.RegisterNotice($"Paramdex: Duplicate name for ID {splitted[0]}");
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

    public WPARAM()
    {
        foreach (WBUtil.GameType game in (WBUtil.GameType[])Enum.GetValues(typeof(WBUtil.GameType)))
        {
            ParamdefStorage[game] = new Dictionary<string, PARAMDEF>();
            NameStorage[game] = new Dictionary<string, Dictionary<int, string>>();
        }
    }
}