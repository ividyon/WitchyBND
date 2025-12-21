#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Microsoft.Extensions.FileSystemGlobbing;
using PromptPlusLibrary;
using SoulsFormats;
using SoulsFormats.Cryptography;

namespace WitchyLib;

public enum DsmsGameType
{
    Undefined = 0,
    DemonsSouls = 1,
    DarkSoulsPTDE = 2,
    DarkSoulsRemastered = 3,
    DarkSoulsIISOTFS = 4,
    DarkSoulsIII = 5,
    Bloodborne = 6,
    Sekiro = 7,
    EldenRing = 8,
    ArmoredCoreVI = 9,
    Nightreign = 15
}

public static class WBUtil
{
    public static string[] MorphemeExtensions =
    [
        ".nsa",
        ".mba",
        ".asa",
        ".qsa",
        ".nmb"
    ];

    internal static readonly object ConsoleWriterLock = new object();
    public static string ExeLocation;
    public static readonly Dictionary<GameType, ulong?> LatestKnownRegulationVersions;

    public static int WitchyVersionToInt(string version)
    {
        // 2010200
        var split = version.Split(".").Select(s => int.Parse(s)).ToArray();
        return split[0] * 1000000 + split[1] * 10000 + split[2] * 100 + split[3];
    }

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
        foreach (string path in paths.Select(p => {
                     try
                     {
                         return Path.GetFullPath(p);
                     }
                     catch (Exception e)
                     {
                         Console.WriteLine($"Invalid path: {p} ({e.Message})");
                         return null;
                     }
                 }).Where(p => p != null).Cast<string>()) {
            if (path.Contains('*'))
            {
                var matcher = new Matcher();
                var rootParts = path.Split(Path.DirectorySeparatorChar).TakeWhile(part => !part.Contains('*')).ToList();
                var root = string.Join(Path.DirectorySeparatorChar, rootParts);
                var rest = path.Substring(root.Length + 1);

                matcher = matcher.AddInclude(rest.Replace(Path.DirectorySeparatorChar.ToString(), "/"));

                var rootPath = Path.Combine(Environment.CurrentDirectory, root);
                if (!Directory.Exists(rootPath))
                {
                    Console.Error.WriteLine($"Invalid path: {rootPath}");
                    continue;
                }
                var files = Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, root), "*",
                    SearchOption.AllDirectories);
                var fileMatch = matcher.Match(Path.Combine(Environment.CurrentDirectory, root), files);
                if (fileMatch.HasMatches)
                {
                    processedPaths.AddRange(fileMatch.Files.Select(m => Path.Combine(root, m.Path)).Where(globFilter).ToList());
                }

                var dirs = Directory.GetDirectories(Path.Combine(Environment.CurrentDirectory, root), "*",
                    SearchOption.AllDirectories);
                var dirMatch = matcher.Match(Path.Combine(Environment.CurrentDirectory, root), dirs);
                if (dirMatch.HasMatches)
                {
                    processedPaths.AddRange(dirMatch.Files.Select(m => Path.Combine(root, m.Path)).Where(globFilter).ToList());
                }
            }
            else
            {
                processedPaths.Add(path);
            }
        }

        return processedPaths.Select(path => Path.GetFullPath(path)).ToList();

        bool globFilter(string path)
        {
            return !path.EndsWith(".bak");
        }
    }

    [DllImport("kernel32.dll")]
    public static extern IntPtr GetConsoleWindow();

    public static string GetExeLocation(params string[]? parts)
    {
        if (parts != null && parts.Any())
        {
            return Path.Combine(new [] { ExeLocation }.Union(parts).ToArray());
        }
        return ExeLocation;
    }

    public static string GetExeLocation()
    {
        return GetExeLocation(Array.Empty<string>());
    }

    public static string GetExecutablePath()
    {
        return System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;
    }

    public enum GameType
    {
        [Display(Name = "Armored Core 4")] AC4,

        [Display(Name = "Armored Core For Answer")]
        ACFA,
        [Display(Name = "Bloodborne")] BB,
        [Display(Name = "Demon's Souls")] DES,
        [Display(Name = "Dark Souls")] DS1,

        [Display(Name = "Dark Souls Remastered")]
        DS1R,
        [Display(Name = "Dark Souls 2")] DS2,

        [Display(Name = "Dark Souls 2: Scholar of the First Sin")]
        DS2S,
        [Display(Name = "Dark Souls 3")] DS3,
        [Display(Name = "ELDEN RING")] ER,
        [Display(Name = "Sekiro")] SDT,
        [Display(Name = "Armored Core VI")] AC6,
        [Display(Name = "ELDEN RING NIGHTREIGN")] NR
    }

    public static string GetAssetsPath()
    {
        return Path.Combine(GetExeLocation(), "Assets");
    }

    public static string GetAssetsPath(params string[] parts)
    {
        return Path.Combine(new[] { GetAssetsPath() }.Union(parts).ToArray());
    }

    public static string GetParamdexPath()
    {
        return Path.Combine(GetAssetsPath(), "Paramdex");
    }

    public static string GetParamdexPath(params string[] parts)
    {
        return Path.Combine(new[] { GetParamdexPath() }.Union(parts).ToArray());
    }

    public static string GetParamdexPath(string path = null)
    {
        return path == null ? GetParamdexPath() : Path.Combine(GetParamdexPath(), path);
    }

    public static string GetParamdexPath(GameType game, params string[]? path)
    {
        return path == null
            ? Path.Combine(GetParamdexPath(), game.ToString())
            : Path.Combine(new[] { GetParamdexPath(), game.ToString() }.Union(path).ToArray());
    }

    public static ulong? GetLatestKnownRegulationVersion(GameType game)
    {
        if (LatestKnownRegulationVersions.ContainsKey(game)) return LatestKnownRegulationVersions[game];

        string latestVerPath = GetParamdexPath(game, "Upgrader", "version.txt");
        if (File.Exists(latestVerPath))
            LatestKnownRegulationVersions[game] =
                ulong.Parse(File.ReadAllText(latestVerPath).Replace("_", "").Replace("L", ""));
        else
            LatestKnownRegulationVersions[game] = null;

        return LatestKnownRegulationVersions[game];
    }

    public static string? TraverseFindFile(string filename, string initPath, int levels = 999)
    {
        string filePath = Path.Combine(initPath, filename);
        for (int i = 0; i < levels; i++)
        {
            if (File.Exists(filePath)) return filePath;
            string dirName = Path.GetDirectoryName(Path.GetDirectoryName(filePath));
            if (dirName == null) return null;
            filePath = Path.Combine(dirName, filename);
        }

        return null;
    }

    /// <summary>
    /// Decrypts and unpacks a regulation BND4 from the specified path, and also outputs the game it's from.
    /// </summary>
    public static BND4 DecryptRegulationBin(string path, out GameType game)
    {
        try
        {
            game = GameType.ER;
            return RegulationDecryptor.DecryptERRegulation(path);
        }
        catch (Exception e) when (e is InvalidDataException or CryptographicException)
        {
            try
            {
                game = GameType.AC6;
                return RegulationDecryptor.DecryptAC6Regulation(path);
            }
            catch (Exception e2) when (e2 is InvalidDataException or CryptographicException)
            {
                try
                {
                    game = GameType.NR;
                    return RegulationDecryptor.DecryptERNRRegulation(path);
                }
                catch (InvalidDataException e3)
                {
                    throw new InvalidDataException($"Could not read sane data using either ER, NR or AC6 decryption keys.");
                }
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
                RegulationDecryptor.EncryptERRegulation(path, bnd);
                break;
            case GameType.AC6:
                RegulationDecryptor.EncryptAC6Regulation(path, bnd);
                break;
            case GameType.NR:
                RegulationDecryptor.EncryptERNRRegulation(path, bnd);
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
            GameType.ER => RegulationDecryptor.DecryptERRegulation(path),
            GameType.NR => RegulationDecryptor.DecryptERNRRegulation(path),
            GameType.AC6 => RegulationDecryptor.DecryptAC6Regulation(path),
            _ => throw new InvalidOperationException("Only Elden Ring, Nightreign and Armored Core VI have a regulation.bin")
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

    private static readonly Regex DriveRx = new Regex(@"^(\w\:\\)(.+)$");
    private static readonly Regex TraversalRx = new Regex(@"^([(..)\\\/]+)(.+)?$");
    private static readonly Regex SlashRx = new Regex(@"^(\\+)(.+)$");


    /// <summary>
    /// Finds common path prefix in a list of strings.
    /// </summary>
    public static string FindCommonBndRootPath(IEnumerable<string> paths)
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
    public static string UnrootBNDPath(string path, string? root)
    {
        path = path.Substring(root?.Length ?? 0);

        Match drive = DriveRx.Match(path);

        if (drive.Success)
        {
            path = drive.Groups[2].Value;
        }

        if (string.IsNullOrWhiteSpace(root))
            return RemoveLeadingBackslashes(path);

        Match traversal = TraversalRx.Match(path);
        if (traversal.Success)
        {
            path = traversal.Groups[2].Value;
        }

        if (path.Contains("..\\") || path.Contains("../"))
            throw new InvalidDataException(
                $"the path {path} contains invalid data, attempting to extract to a different folder.");
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

    public static bool IsInGit(string? path)
    {
        if (!Directory.Exists(path))
            path = Path.GetDirectoryName(path)!;

        // return Repository.IsValid(path);

        while (!string.IsNullOrEmpty(path))
        {
            if (Directory.Exists(Path.Combine(path, ".git")))
                return true;
            path = Path.GetDirectoryName(path);
        }

        return false;
    }
    public enum BackupMethod
    {
        [Display(Name = "Write once")]
        WriteOnce,
        [Display(Name = "Always overwrite")]
        OverwriteAlways,
        [Display(Name = "Create copies")]
        CreateCopies,
        [Display(Name = "None")]
        None
    }
    public static void Backup(string path, BackupMethod method)
    {
        if (method == BackupMethod.None) return;
        if (!File.Exists(path)) return;
        switch (method)
        {
            case BackupMethod.WriteOnce:
                if (!File.Exists(path + ".bak"))
                    File.Move(path, path + ".bak");
                return;
            case BackupMethod.OverwriteAlways:
                if (File.Exists(path + ".bak"))
                    File.Delete(path + ".bak");
                File.Move(path, path + ".bak");
                return;
            case BackupMethod.CreateCopies:
                var dest = NextAvailableFilename(path + ".bak");
                File.Move(path, dest);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(method), method, null);
        }

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

    public static void XmlSerialize<T>(object obj, string targetFile)
    {
        var dir = Path.GetDirectoryName(targetFile)!;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

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

    public static bool ObnoxiousWarning(List<string> lines)
    {
        PromptPlus.Console.WriteLine("");

        foreach (string line in lines)
        {
            PromptPlus.Console.WriteLine(line);
            var cursor = PromptPlus.Console.GetCursorPosition();
            PromptPlus.Console.WriteLine("");
            PromptPlus.Controls.WaitTimer(TimeSpan.FromSeconds(1), "Please read carefully, then press any key...");
            // ClearLine();
            PromptPlus.Console.SetCursorPosition(cursor.Left, cursor.Top);
            PromptPlus.Console.WriteLineColor("");
            PromptPlus.Controls.KeyPress("Please read carefully, then press any key...")
                .Options(a => a.EnabledAbortKey(false))
                .Run();
            // PromptPlus.Console.ClearLine();
            PromptPlus.Console.SetCursorPosition(cursor.Left, cursor.Top);
        }

        PromptPlus.Console.WriteLine("");
        var confirm = PromptPlus.Controls.Confirm(@"Do you still wish to proceed?").Run();

        if (confirm.Content.Value.IsAbortKeyPress() || confirm.IsAborted)
            return false;

        return true;
    }

    public static string GetValidFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(fileName.Where(c => !invalid.Contains(c)).ToArray());
    }

    private static string numberPattern = " ({0})";

    public static string NextAvailableFilename(string path)
    {
        // Short-cut if already available
        if (!File.Exists(path))
            return path;

        // If path has extension then insert the number pattern just before the extension and return next filename
        if (Path.HasExtension(path))
            return GetNextFilename(path.Insert(path.LastIndexOf(Path.GetExtension(path)), numberPattern));

        // Otherwise just append the pattern to the path and return next filename
        return GetNextFilename(path + numberPattern);
    }

    private static string GetNextFilename(string pattern)
    {
        string tmp = string.Format(pattern, 1);
        if (tmp == pattern)
            throw new ArgumentException("The pattern must include an index place-holder", "pattern");

        if (!File.Exists(tmp))
            return tmp; // short-circuit if no matches

        int min = 1, max = 2; // min is inclusive, max is exclusive/untested

        while (File.Exists(string.Format(pattern, max)))
        {
            min = max;
            max *= 2;
        }

        while (max != min + 1)
        {
            int pivot = (max + min) / 2;
            if (File.Exists(string.Format(pattern, pivot)))
                min = pivot;
            else
                max = pivot;
        }

        return string.Format(pattern, max);
    }

    static WBUtil()
    {
        ExeLocation = Path.GetDirectoryName(AppContext.BaseDirectory);
        LatestKnownRegulationVersions = new();
    }

    public static string GetFileNameWithoutAnyExtensions(string path)
    {
        return Path.GetFileName(path).Split(".").First();
    }

    public static string GetFullExtensions(string path)
    {
        var split = Path.GetFileName(path).Split(".");
        if (split.Length > 1)
            return "." + string.Join(".", Path.GetFileName(path).Split(".").Skip(1));
        return "";
    }
}