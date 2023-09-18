﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Win32;
using Newtonsoft.Json;
using PPlus;
using SoulsFormats;
using PARAMDEF = WitchyFormats.PARAMDEF;

namespace WitchyLib;

public static class WBUtil
{
    public static string ExeLocation;

    public static string FirstCharToUpper(this string input) =>
        input switch
        {
            null => throw new ArgumentNullException(nameof(input)),
            "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
            _ => string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1))
        };

    public static string SanitizeFilename(string path)
    {
        return Path.GetInvalidFileNameChars()
            .Aggregate(path, (current, c) => current.Replace(c, '_'));
    }

    public static void WriteSanitizedBinderFilePath(this XElement element, string path, string pathElementName = "path")
    {
        string dir = Path.GetDirectoryName(path) ?? "";
        string filename = Path.GetFileName(path);
        string sanitized = SanitizeFilename(path);

        if (filename == sanitized)
        {
            element.Add(new XElement(pathElementName, path));
        }
        else
        {
            element.Add(new XElement("in" + pathElementName.FirstCharToUpper(), path));
            element.Add(new XElement("out" + pathElementName.FirstCharToUpper(), Path.Combine(dir, sanitized)));
        }
    }

    public static string GetSanitizedBinderFilePath(this XElement element, string pathElementName = "path",
        bool outName = false)
    {
        if (element.Element(pathElementName) != null)
            return element.Element(pathElementName)!.Value;
        var otherName =
            outName ? "out" + pathElementName.FirstCharToUpper() : "in" + pathElementName.FirstCharToUpper();
        if (element.Element(otherName) != null)
            return element.Element(otherName)!.Value;

        throw new InvalidDataException("File element is missing path.");
    }

    public static List<string> ProcessPathGlobs(List<string> paths)
    {
        var processedPaths = new List<string>();
        foreach (string path in paths)
        {
            if (path.Contains('*'))
            {
                var matcher = new Matcher();
                var rootParts = path.Split(Path.DirectorySeparatorChar).TakeWhile(part => !part.Contains('*')).ToList();
                var root = string.Join(Path.DirectorySeparatorChar, rootParts);
                var rest = path.Substring(root.Length + 1);

                matcher = matcher.AddInclude(rest.Replace(Path.DirectorySeparatorChar.ToString(), "/"));
                var files = Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, root), "*",
                    SearchOption.AllDirectories);
                var match = matcher.Match(Path.Combine(Environment.CurrentDirectory, root), files);
                if (match.HasMatches)
                {
                    processedPaths.AddRange(match.Files.Select(m => Path.Combine(root, m.Path)).ToList());
                }
            }
            else
            {
                processedPaths.Add(path);
            }
        }

        return processedPaths.Select(path => Path.GetFullPath(path)).ToList();
    }

    public static string GetExeLocation()
    {
        return ExeLocation;
    }

    public enum GameType
    {
        [Display(Name = "Armored Core 4")]
        AC4,
        [Display(Name = "Armored Core For Answer")]
        ACFA,
        [Display(Name = "Bloodborne")]
        BB,
        [Display(Name = "Demon's Souls")]
        DES,
        [Display(Name = "Dark Souls")]
        DS1,
        [Display(Name = "Dark Souls Remastered")]
        DS1R,
        [Display(Name = "Dark Souls 2")]
        DS2,
        [Display(Name = "Dark Souls 2: Scholar of the First Sin")]
        DS2S,
        [Display(Name = "Dark Souls 3")]
        DS3,
        [Display(Name = "Elden Ring")]
        ER,
        [Display(Name = "Sekiro")]
        SDT,
        [Display(Name = "Armored Core VI")]
        AC6
    }

    public static GameType DetermineParamdexGame(string path, bool passive)
    {
        GameType? gameNullable = null;

        // Determine what kind of PARAM we're dealing with here
        var witchyXmlPath = $@"{path}\_witchy-bnd4.xml";
        if (File.Exists(witchyXmlPath))
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(witchyXmlPath);

            string filename = xml.SelectSingleNode("bnd4/filename").InnerText;
            if (filename == "regulation.bin" && xml.SelectSingleNode("bnd4/game")?.InnerText != null)
            {
                Enum.TryParse(xml.SelectSingleNode("bnd4/game")!.InnerText, out GameType regGame);
                gameNullable = regGame;
            }
        }

        if (gameNullable != null)
        {
            PromptPlus.Error.WriteLine($"Determined game for Paramdex: {gameNullable.Value.ToString()}".PromptPlusEscape());
        }
        else
        {
            PromptPlus.Error.WriteLine("Could not determine param game version.");
            if (!passive)
            {
                var select = PromptPlus.Select<GameType>("Please select the Paramdex of one of the following games")
                    .Run();
                if (select.IsAborted)
                {
                    throw new Exception("Could not determine PARAM type.");
                }

                gameNullable = select.Value;
            }
            else
            {
                throw new Exception("Could not determine PARAM type.");
            }
        }

        return gameNullable.Value;
    }

    /// <summary>
    /// Decrypts and unpacks a regulation BND4 from the specified path, and also outputs the game it's from.
    /// </summary>
    public static BND4 DecryptRegulationBin(string path, out GameType game)
    {
        try
        {
            game = GameType.ER;
            return SFUtil.DecryptERRegulation(path);
        }
        catch (Exception)
        {
            try
            {
                game = GameType.AC6;
                return SFUtil.DecryptAC6Regulation(path);
            }
            catch (Exception e2)
            {
                throw new InvalidDataException($"File is not a regulation.bin BND for Elden Ring or Armored Core VI.",
                    e2);
            }
        }
    }

    /// <summary>
    /// Repacks and encrypts an regulation BND4 for a specified game to the specified path.
    /// </summary>
    public static void EncryptRegulationBin(string path, GameType game, BND4 bnd)
    {
        switch (game)
        {
            case GameType.ER:
                SFUtil.EncryptERRegulation(path, bnd);
                break;
            case GameType.AC6:
                SFUtil.EncryptAC6Regulation(path, bnd);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(game), game, null);
        }
    }

    /// <summary>
    /// Determines whether the output of a decrypted regulation.bin file actually
    /// gives a proper BND4 or not.
    /// </summary>
    public static BND4 IsRegulationBin(string path, GameType game)
    {
        return game switch
        {
            GameType.ER => SFUtil.DecryptERRegulation(path),
            GameType.AC6 => SFUtil.DecryptAC6Regulation(path),
            _ => throw new InvalidOperationException("Only Elden Ring and Armored Core VI have a regulation.bin")
        };
    }

    public static Type TypeForParamDefType(PARAMDEF.DefType type, bool isArray)
    {
        switch (type)
        {
            case PARAMDEF.DefType.s8:
                return typeof(sbyte);
            case PARAMDEF.DefType.u8:
                return typeof(byte);
            case PARAMDEF.DefType.s16:
                return typeof(short);
            case PARAMDEF.DefType.u16:
                return typeof(ushort);
            case PARAMDEF.DefType.s32:
            case PARAMDEF.DefType.b32:
                return typeof(int);
            case PARAMDEF.DefType.u32:
                return typeof(uint);
            case PARAMDEF.DefType.f32:
            case PARAMDEF.DefType.angle32:
                return typeof(float);
            case PARAMDEF.DefType.f64:
                return typeof(double);
            case PARAMDEF.DefType.dummy8:
                return isArray ? typeof(byte[]) : typeof(byte);
            case PARAMDEF.DefType.fixstr:
            case PARAMDEF.DefType.fixstrW:
                return typeof(string);
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public static string GetXmlPath(string type, string dir = "")
    {
        dir = string.IsNullOrEmpty(dir) ? dir : $"{dir}\\";

        if (File.Exists($"{dir}_witchy-{type}.xml"))
        {
            return $"{dir}_witchy-{type}.xml";
        }

        if (File.Exists($"{dir}_yabber-{type}.xml"))
        {
            return $"{dir}_yabber-{type}.xml";
        }

        throw new Exception($"Could not find WitchyBND or Yabber {type.ToUpper()} XML");
    }

    private static readonly Regex DriveRx = new Regex(@"^(\w\:\\)(.+)$");
    private static readonly Regex TraversalRx = new Regex(@"^([(..)\\\/]+)(.+)?$");
    private static readonly Regex SlashRx = new Regex(@"^(\\+)(.+)$");


    /// <summary>
    /// Finds common path prefix in a list of strings.
    /// </summary>
    public static string FindCommonRootPath(IEnumerable<string> paths)
    {
        string root = "";

        if (paths.Count() == 0) return root;

        var rootPath = new string(
            paths.First().Substring(0, paths.Min(s => s.Length))
                .TakeWhile((c, i) => paths.All(s => s[i] == c)).ToArray());

        // For safety, truncate this shared string down to the last slash/backslash.
        var rootPathIndex = Math.Max(rootPath.LastIndexOf('\\'), rootPath.LastIndexOf('/'));

        if (rootPath != "" && rootPathIndex != -1)
        {
            root = rootPath.Substring(0, rootPathIndex);
        }

        return root;
    }

    /// <summary>
    /// Removes common network path roots if present.
    /// </summary>
    public static string UnrootBNDPath(string path, string root)
    {
        if (string.IsNullOrEmpty(root))
            return path;

        path = path.Substring(root.Length);

        Match drive = DriveRx.Match(path);
        if (drive.Success)
        {
            path = drive.Groups[2].Value;
        }

        Match traversal = TraversalRx.Match(path);
        if (traversal.Success)
        {
            path = traversal.Groups[2].Value;
        }

        if (path.Contains("..\\") || path.Contains("../"))
            throw new InvalidDataException(
                $"the path {path} contains invalid data, attempting to extract to a different folder. Please report this file to Nordgaren.");
        return RemoveLeadingBackslashes(path);
    }

    private static string RemoveLeadingBackslashes(string path)
    {
        Match slash = SlashRx.Match(path);
        if (slash.Success)
        {
            path = slash.Groups[2].Value;
        }

        return path;
    }

    public static void Backup(string path)
    {
        if (File.Exists(path) && !File.Exists(path + ".bak"))
            File.Move(path, path + ".bak");
    }

    private static byte[] ds2RegulationKey =
        { 0x40, 0x17, 0x81, 0x30, 0xDF, 0x0A, 0x94, 0x54, 0x33, 0x09, 0xE1, 0x71, 0xEC, 0xBF, 0x25, 0x4C };

    /// <summary>
    /// Decrypts and unpacks DS2's regulation BND4 from the specified path.
    /// </summary>
    public static BND4 DecryptDS2Regulation(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        byte[] iv = new byte[16];
        iv[0] = 0x80;
        Array.Copy(bytes, 0, iv, 1, 11);
        iv[15] = 1;
        byte[] input = new byte[bytes.Length - 32];
        Array.Copy(bytes, 32, input, 0, bytes.Length - 32);
        using (var ms = new MemoryStream(input))
        {
            byte[] decrypted = CryptographyUtil.DecryptAesCtr(ms, ds2RegulationKey, iv);
            return BND4.Read(decrypted);
        }
    }

    static (string, string)[] _pathValueTuple = new (string, string)[]
    {
        (@"HKEY_CURRENT_USER\SOFTWARE\Valve\Steam", "SteamPath"),
        (@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Valve\Steam", "InstallPath"),
        (@"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "InstallPath"),
        (@"HKEY_CURRENT_USER\SOFTWARE\Wow6432Node\Valve\Steam", "SteamPath"),
    };

    // Improved detection from Gideon
    public static string TryGetGameInstallLocation(string gamePath)
    {
        if (!gamePath.StartsWith("\\") && !gamePath.StartsWith("/"))
            return null;

        string steamPath = GetSteamInstallPath();

        if (string.IsNullOrWhiteSpace(steamPath) || !File.Exists($@"{steamPath}\SteamApps\libraryfolders.vdf"))
            return null;

        string[] libraryFolders = File.ReadAllLines($@"{steamPath}\SteamApps\libraryfolders.vdf");

        var pathStrings = libraryFolders.Where(str => str.Contains("\"path\""));
        var paths = pathStrings.Select(str => {
            var split = str.Split('"').Where((s, i) => i % 2 == 1).ToList();
            if (split.Count == 2)
                return split[1];

            return null;
        }).ToList();

        foreach (string path in paths)
        {
            string libraryPath = path.Replace(@"\\", @"\") + gamePath;
            if (File.Exists(libraryPath))
                return libraryPath;
        }

        return null;
    }

    public static string GetSteamInstallPath()
    {
        string installPath = null;

        foreach ((string Path, string Value) pathValueTuple in _pathValueTuple)
        {
            string registryKey = pathValueTuple.Path;
            installPath = (string)Registry.GetValue(registryKey, pathValueTuple.Value, null);

            if (installPath != null)
                break;
        }

        return installPath;
    }

    private static string[] Oodle6Games =
    {
        "Sekiro",
        "ELDEN RING",
    };

    private static string[] Oodle8Games =
    {
        "ARMORED CORE VI FIRES OF RUBICON",
    };


    public static string GetOodlePath()
    {
        foreach (string game in Oodle6Games)
        {
            string path = TryGetGameInstallLocation($"\\steamapps\\common\\{game}\\Game\\oo2core_6_win64.dll");
            if (path != null)
                return path;
        }

        foreach (string game in Oodle8Games)
        {
            string path = TryGetGameInstallLocation($"\\steamapps\\common\\{game}\\Game\\oo2core_8_win64.dll");
            if (path != null)
                return path;
        }

        return null;
    }

    public static string JsonSerialize(object obj)
    {
        return JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        });
    }

    public static T JsonDeserialize<T>(string text)
    {
        return JsonConvert.DeserializeObject<T>(text, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        });
    }

    public static void XmlSerialize<T>(object obj, string targetFile)
    {
        using (var xw = XmlWriter.Create(targetFile, new XmlWriterSettings() { Indent = true }))
        {
            var xmlSer = new XmlSerializer(typeof(T));

            xmlSer.Serialize(xw, obj);
        }
    }

    public static byte[] XmlSerialize<T>(object obj)
    {
        var stream = new MemoryStream();
        using (var xw = XmlWriter.Create(stream, new XmlWriterSettings() { Indent = true }))
        {
            var xmlSer = new XmlSerializer(typeof(T));

            xmlSer.Serialize(xw, obj);
        }

        return stream.ToArray();
    }

    public static T XmlDeserialize<T>(string sourceFile)
    {
        using (var xw = XmlReader.Create(sourceFile))
        {
            var xmlSer = new XmlSerializer(typeof(T));

            return (T)xmlSer.Deserialize(xw);
        }
    }

    /// <summary>
    /// Helpers for delimited string, with support for escaping the delimiter
    /// character.
    /// https://coding.abel.nu/2016/06/string-split-and-join-with-escaping/
    /// </summary>
    public static class DelimitedString
    {
        const string DelimiterString = ",";
        const char DelimiterChar = ',';

        // Use a single / as escape char, avoid \ as that would require
        // all escape chars to be escaped in the source code...
        const char EscapeChar = '/';
        const string EscapeString = "/";

        /// <summary>
        /// Join strings with a delimiter and escape any occurence of the
        /// delimiter and the escape character in the string.
        /// </summary>
        /// <param name="strings">Strings to join</param>
        /// <returns>Joined string</returns>
        public static string Join(params string[] strings)
        {
            return string.Join(
                DelimiterString,
                strings.Select(
                    s => s
                        .Replace(EscapeString, EscapeString + EscapeString)
                        .Replace(DelimiterString, EscapeString + DelimiterString)));
        }

        public static string Join(IEnumerable<string> strings)
        {
            return Join(strings.ToArray());
        }

        /// <summary>
        /// Split strings delimited strings, respecting if the delimiter
        /// characters is escaped.
        /// </summary>
        /// <param name="source">Joined string from <see cref="Join(string[])"/></param>
        /// <returns>Unescaped, split strings</returns>
        public static string[] Split(string source)
        {
            var result = new List<string>();

            int segmentStart = 0;
            for (int i = 0; i < source.Length; i++)
            {
                bool readEscapeChar = false;
                if (source[i] == EscapeChar)
                {
                    readEscapeChar = true;
                    i++;
                }

                if (!readEscapeChar && source[i] == DelimiterChar)
                {
                    result.Add(UnEscapeString(
                        source.Substring(segmentStart, i - segmentStart)));
                    segmentStart = i + 1;
                }

                if (i == source.Length - 1)
                {
                    result.Add(UnEscapeString(source.Substring(segmentStart)));
                }
            }

            return result.ToArray();
        }

        static string UnEscapeString(string src)
        {
            return src.Replace(EscapeString + DelimiterString, DelimiterString)
                .Replace(EscapeString + EscapeString, EscapeString);
        }
    }

    public static byte[] TryDecompressBytes(string sourceFile, out DCX.Type compression)
    {
        try
        {
            return DCX.Decompress(sourceFile, out compression);
        }
        catch (NoOodleFoundException)
        {
            string oo2corePath = GetOodlePath();
            if (oo2corePath == null)
                throw;

            IntPtr handle = Kernel32.LoadLibrary(oo2corePath);
            byte[] bytes = DCX.Decompress(sourceFile, out compression);
            Kernel32.FreeLibrary(handle);
            return bytes;
        }
    }

    public static void TryCompressBytes(byte[] data, DCX.Type type, string path)
    {
        try
        {
            DCX.Compress(data, type, path);
        }
        catch (NoOodleFoundException)
        {
            string oo2corePath = WBUtil.GetOodlePath();
            if (oo2corePath == null)
                throw;

            IntPtr handle = Kernel32.LoadLibrary(oo2corePath);
            DCX.Compress(data, type, path);
            Kernel32.FreeLibrary(handle);
        }
    }

    public static void TryWriteSoulsFile(this ISoulsFile file, string path)
    {
        try
        {
            file.Write(path);
        }
        catch (NoOodleFoundException)
        {
            string oo2corePath = GetOodlePath();
            if (oo2corePath == null)
                throw;

            IntPtr handle = Kernel32.LoadLibrary(oo2corePath);
            file.Write(path);
            Kernel32.FreeLibrary(handle);
        }
    }

    public static void TryWriteBXF(this BXF4 file, string bhdPath, string bdtPath)
    {
        try
        {
            file.Write(bhdPath, bdtPath);
        }
        catch (NoOodleFoundException)
        {
            string oo2corePath = GetOodlePath();
            if (oo2corePath == null)
                throw;

            IntPtr handle = Kernel32.LoadLibrary(oo2corePath);
            file.Write(bhdPath, bdtPath);
            Kernel32.FreeLibrary(handle);
        }
    }

    public static void TryWriteBXF(this BXF3 file, string bhdPath, string bdtPath)
    {
        try
        {
            file.Write(bhdPath, bdtPath);
        }
        catch (NoOodleFoundException)
        {
            string oo2corePath = GetOodlePath();
            if (oo2corePath == null)
                throw;

            IntPtr handle = Kernel32.LoadLibrary(oo2corePath);
            file.Write(bhdPath, bdtPath);
            Kernel32.FreeLibrary(handle);
        }
    }

    static WBUtil()
    {
        ExeLocation = Path.GetDirectoryName(AppContext.BaseDirectory);
    }
}