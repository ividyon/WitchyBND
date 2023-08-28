using SoulsFormats;
using SoulsFormats.AC4;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using System.Xml;
using CommandLine;
using PPlus;
using WitchyBND.CliModes;
using WitchyBND.Parsers;
using WitchyFormats;
using WitchyLib;
using GPARAM = WitchyFormats.GPARAM;
using MATBIN = WitchyFormats.MATBIN;
using MTD = WitchyFormats.MTD;
using TPF = WitchyFormats.TPF;
using PARAM = WitchyFormats.FsParam;
using MQB = WitchyFormats.MQB;

namespace WitchyBND;

[SupportedOSPlatform("windows")]

public enum CliMode
{
    Parse,
    Config
}

internal class Configuration
{
    public bool Dry { get; set; }
    public bool Bnd { get; set; }
    public bool Dcx { get; set; }
}
public class CliOptions
{
    // [Option('v', "verbose", Group = "verbosity", Default = false, HelpText = "Set output to verbose messages.")]
    // public bool Verbose { get; set; }
    //
    // [Option('q', "quiet", Group = "verbosity", Default = false,
    //     HelpText = "Set output to quiet, reporting only errors.")]
    // public bool Quiet { get; set; }

    [Option('b', "default-bnd", HelpText = "Perform basic unpacking of BND instead of using special Witchy methods, where present")]
    public bool Bnd { get; set; }

    [Option('n', "dry-run", HelpText = "Perform the actions as a \"dry run\", meaning that files will not actually be written or modified.")]
    public bool Dry { get; set; }

    [Option('d', "dcx", HelpText = "Simply decompress DCX files instead of unpacking their content.")]
    public bool Dcx { get; set; }

    [Value(0, HelpText = "The paths that should be parsed by Witchy.")]
    public IEnumerable<string> Paths { get; set; }
}

static class Program
{
    public static Configuration Configuration;
    private static List<string> AccruedErrors;
    static WBUtil.GameType? game;

    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

        Assembly assembly = Assembly.GetExecutingAssembly();
        PromptPlus.DoubleDash($"{assembly.GetName().Name} {assembly.GetName().Version}");

        var parser = new Parser(config => {
            config.AutoHelp = true;
        });
        parser.ParseArguments<CliOptions>(args)
            .WithParsed(opt => {
                // Override configuration
                if (opt.Dcx)
                    Configuration.Dcx = true;
                if (opt.Bnd)
                    Configuration.Bnd = true;

                // Set CLI mode
                CliMode mode = CliMode.Parse;
                if (!opt.Paths.Any())
                    mode = CliMode.Config;

                // Execute
                switch (mode)
                {
                    case CliMode.Parse:
                        Parse.CliParseMode(opt);
                        break;
                    case CliMode.Config:
                        Config.CliConfigMode(opt);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            })
            .WithNotParsed(errors => { });
    }

    private static bool UnpackFile(string sourceFile, IProgress<float> progress)
    {
        string sourceDir = new FileInfo(sourceFile).Directory?.FullName;
        string fileName = Path.GetFileName(sourceFile);
        string targetDir = $"{sourceDir}\\{fileName.Replace('.', '-')}";
        if (File.Exists(targetDir))
            targetDir += "-ybr";

        if (fileName.Contains("regulation.bnd.dcx") || fileName.Contains("Data0") ||
            fileName.Contains("regulation.bin") || fileName.Contains("regulation.bnd"))
            return UnpackRegulationFile(fileName, sourceDir, targetDir, progress);

            if (DCX.Is(sourceFile))
            {
                Console.WriteLine($"Decompressing DCX: {fileName}...");
                byte[] bytes = WBUtil.TryDecompressBytes(sourceFile, out DCX.Type compression);

                if (BND3.Is(bytes))
                {
                    Console.WriteLine($"Unpacking BND3: {fileName}...");
                    using (var bnd = new BND3Reader(bytes))
                    {
                        bnd.Compression = compression;
                        bnd.Unpack(fileName, targetDir, progress);
                    }
                }
                // else if (WFFXBND.Is(bytes, fileName))
                // {
                //     Console.WriteLine($"Unpacking FFXBND: {fileName}...");
                //     using (var bnd = new BND4Reader(bytes))
                //     {
                //         bnd.Compression = compression;
                //         bnd.UnpackFFXBND(fileName, targetDir, progress);
                //     }
                // }
                else if (BND4.Is(bytes))
                {
                    Console.WriteLine($"Unpacking BND4: {fileName}...");
                    using (var bnd = new BND4Reader(bytes))
                    {
                        bnd.Compression = compression;
                        bnd.Unpack(fileName, targetDir, progress);
                    }
                }
                else if (FFXDLSE.Is(bytes))
                {
                    Console.WriteLine($"Unpacking FFX: {fileName}...");
                    var ffx = FFXDLSE.Read(bytes);
                    ffx.Compression = compression;
                    ffx.Unpack(sourceFile);
                }
                else if (sourceFile.EndsWith(".fmg.dcx"))
                {
                    Console.WriteLine($"Unpacking FMG: {fileName}...");
                    FMG fmg = FMG.Read(bytes);
                    fmg.Compression = compression;
                    fmg.Unpack(sourceFile);
                }
                else if (GPARAM.Is(bytes))
                {
                    Console.WriteLine($"Unpacking GPARAM: {fileName}...");
                    GPARAM gparam = GPARAM.Read(bytes);
                    gparam.Compression = compression;
                    gparam.Unpack(sourceFile);
                }
                else if (TPF.Is(bytes))
                {
                    Console.WriteLine($"Unpacking TPF: {fileName}...");
                    TPF tpf = TPF.Read(bytes);
                    tpf.Compression = compression;
                    tpf.Unpack(fileName, targetDir, progress);
                }
                else if (MSBE.Is(bytes))
                {
                    Console.WriteLine($"Unpacking MSB: {fileName}...");
                    MSBE msb = MSBE.Read(bytes);
                    msb.Unpack(fileName, targetDir, progress);
                }
                else
                {
                    Console.WriteLine($"File format not recognized: {fileName}");
                    return true;
                }
            }
            else
            {
                if (BND3.Is(sourceFile))
                {
                    Console.WriteLine($"Unpacking BND3: {fileName}...");
                    using (var bnd = new BND3Reader(sourceFile))
                    {
                        bnd.Unpack(fileName, targetDir, progress);
                    }
                }
                // else if (WFFXBND.Is(sourceFile))
                // {
                //     Console.WriteLine($"Unpacking FFXBND: {fileName}...");
                //     using (var bnd = new BND4Reader(sourceFile))
                //     {
                //         bnd.UnpackFFXBND(fileName, targetDir, progress);
                //     }
                // }
                else if (BND4.Is(sourceFile))
                {
                    Console.WriteLine($"Unpacking BND4: {fileName}...");
                    using (var bnd = new BND4Reader(sourceFile))
                    {
                        bnd.Unpack(fileName, targetDir, progress);
                    }
                }
                else if (BXF3.IsBHD(sourceFile))
                {
                    string bdtExtension = Path.GetExtension(fileName).Replace("bhd", "bdt");
                    string bdtFilename = $"{Path.GetFileNameWithoutExtension(fileName)}{bdtExtension}";
                    string bdtPath = $"{sourceDir}\\{bdtFilename}";
                    if (File.Exists(bdtPath))
                    {
                        Console.WriteLine($"Unpacking BXF3: {fileName}...");
                        using (var bxf = new BXF3Reader(sourceFile, bdtPath))
                        {
                            bxf.Unpack(fileName, bdtFilename, targetDir, progress);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"BDT not found for BHD: {fileName}");
                        return true;
                    }
                }
                else if (BXF4.IsBHD(sourceFile))
                {
                    string bdtExtension = Path.GetExtension(fileName).Replace("bhd", "bdt");
                    string bdtFilename = $"{Path.GetFileNameWithoutExtension(fileName)}{bdtExtension}";
                    string bdtPath = $"{sourceDir}\\{bdtFilename}";
                    if (File.Exists(bdtPath))
                    {
                        Console.WriteLine($"Unpacking BXF4: {fileName}...");
                        using (var bxf = new BXF4Reader(sourceFile, bdtPath))
                        {
                            bxf.Unpack(fileName, bdtFilename, targetDir, progress);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"BDT not found for BHD: {fileName}");
                        return true;
                    }
                }
                else if (FFXDLSE.Is(sourceFile))
                {
                    Console.WriteLine($"Unpacking FFX: {fileName}...");
                    var ffx = FFXDLSE.Read(sourceFile);
                    ffx.Unpack(sourceFile);
                }
                else if (sourceFile.EndsWith(".ffx.xml") || sourceFile.EndsWith(".ffx.dcx.xml"))
                {
                    Console.WriteLine($"Repacking FFX: {fileName}...");
                    WFFX.Repack(sourceFile);
                }
                else if (sourceFile.EndsWith(".fmg"))
                {
                    Console.WriteLine($"Unpacking FMG: {fileName}...");
                    FMG fmg = FMG.Read(sourceFile);
                    fmg.Unpack(sourceFile);
                }
                else if (sourceFile.EndsWith(".fmg.xml") || sourceFile.EndsWith(".fmg.dcx.xml"))
                {
                    Console.WriteLine($"Repacking FMG: {fileName}...");
                    WFMG.Repack(sourceFile);
                }
                else if (GPARAM.Is(sourceFile))
                {
                    Console.WriteLine($"Unpacking GPARAM: {fileName}...");
                    GPARAM gparam = GPARAM.Read(sourceFile);
                    gparam.Unpack(sourceFile);
                }
                else if (sourceFile.EndsWith(".gparam.xml") || sourceFile.EndsWith(".gparam.dcx.xml")
                    || sourceFile.EndsWith(".fltparam.xml") ||
                    sourceFile.EndsWith(".fltparam.dcx.xml"))
                {
                    Console.WriteLine($"Repacking GPARAM: {fileName}...");
                    WGPARAM.Repack(sourceFile);
                }
                else if (sourceFile.EndsWith(".luagnl"))
                {
                    Console.WriteLine($"Unpacking LUAGNL: {fileName}...");
                    LUAGNL gnl = LUAGNL.Read(sourceFile);
                    gnl.Unpack(sourceFile);
                }
                else if (sourceFile.EndsWith(".luagnl.xml"))
                {
                    Console.WriteLine($"Repacking LUAGNL: {fileName}...");
                    WLUAGNL.Repack(sourceFile);
                }
                else if (LUAINFO.Is(sourceFile))
                {
                    Console.WriteLine($"Unpacking LUAINFO: {fileName}...");
                    LUAINFO info = LUAINFO.Read(sourceFile);
                    info.Unpack(sourceFile);
                }
                else if (sourceFile.EndsWith(".luainfo.xml"))
                {
                    Console.WriteLine($"Repacking LUAINFO: {fileName}...");
                    WLUAINFO.Repack(sourceFile);
                }
                else if (TPF.Is(sourceFile))
                {
                    Console.WriteLine($"Unpacking TPF: {fileName}...");
                    TPF tpf = TPF.Read(sourceFile);
                    return tpf.Unpack(fileName, targetDir, progress);
                }
                else if (Zero3.Is(sourceFile))
                {
                    Console.WriteLine($"Unpacking 000: {fileName}...");
                    Zero3 z3 = Zero3.Read(sourceFile);
                    z3.Unpack(targetDir);
                }
                else if (Fxr3.Is(sourceFile))
                {
                    Console.WriteLine($"Unpacking FXR: {fileName}...");
                    Fxr3 fxr = Fxr3.Read(sourceFile);
                    fxr.Unpack(fileName);
                }
                else if (sourceFile.EndsWith(".fxr.xml"))
                {
                    Console.WriteLine($"Repacking FXR: {fileName}...");
                    return WFXR.Repack(sourceFile);
                }
                else if (MATBIN.Is(sourceFile))
                {
                    Console.WriteLine($"Unpacking MATBIN: {fileName}...");
                    MATBIN matbin = MATBIN.Read(sourceFile);
                    matbin.Unpack(fileName);
                }
                else if (sourceFile.EndsWith(".matbin.xml"))
                {
                    Console.WriteLine($"Repacking MATBIN: {fileName}...");
                    return WMATBIN.Repack(sourceFile);
                }
                else if (MTD.Is(sourceFile))
                {
                    Console.WriteLine($"Unpacking MTD: {fileName}...");
                    MTD mtd = MTD.Read(sourceFile);
                    mtd.Unpack(fileName);
                }
                else if (sourceFile.EndsWith(".mtd.xml"))
                {
                    Console.WriteLine($"Repacking MTD: {fileName}...");
                    return WMTD.Repack(sourceFile);
                }
                else if (sourceFile.EndsWith(".param"))
                {
                    if (game == null)
                    {
                        game = WBUtil.DetermineParamdexGame(sourceDir);
                    }

                Console.WriteLine($"Unpacking PARAM: {fileName}...");
                PARAM p = PARAM.Read(sourceFile);

                    return p.Unpack(sourceFile, sourceDir, game.Value);
                }
                else if (sourceFile.EndsWith(".param.xml"))
                {
                    Console.WriteLine($"Repacking PARAM: {fileName}...");
                    return WPARAM.Repack(sourceFile, sourceDir);
                }
                else if (MQB.Is(sourceFile))
                {
                    Console.WriteLine($"Converting MQB: {fileName}...");
                    MQB mqb = MQB.Read(sourceFile);
                    mqb.Unpack(fileName, sourceDir, progress);
                }
                else if (sourceFile.EndsWith(".mqb.xml"))
                {
                    Console.WriteLine($"Converting XML to MQB: {fileName}...");
                    WMQB.Repack(sourceFile);
                }
                else
                {
                    Console.WriteLine($"File format not recognized: {fileName}");
                    return true;
                }
            }

            return false;
        }

    private static bool UnpackRegulationFile(string fileName, string sourceDir, string targetDir,
        IProgress<float> progress)
    {
        if (fileName.Contains("regulation.bin"))
        {
            string destPath = Path.Combine(sourceDir, fileName);
            BND4 bnd = SFUtil.DecryptERRegulation(destPath);
            Console.WriteLine($"ER Regulation Bin: {fileName}...");
            using (var bndReader = new BND4Reader(bnd.Write()))
            {
                bndReader.Unpack(fileName, targetDir, progress);
            }

            return false;
        }

        if (fileName.Contains("Data0"))
        {
            string destPath = Path.Combine(sourceDir, "Data0.bdt");
            BND4 bnd = SFUtil.DecryptDS3Regulation(destPath);
            Console.WriteLine($"Unpacking DS3 Regulation Bin: {fileName}...");
            using (var bndReader = new BND4Reader(bnd.Write()))
            {
                bndReader.Unpack(fileName, targetDir, progress);
            }

            return false;
        }

        if (fileName.Contains("enc_regulation.bnd.dcx"))
        {
            string destPath = Path.Combine(sourceDir, fileName);
            BND4 bnd;
            if (!BND4.IsRead(destPath, out bnd))
            {
                bnd = WBUtil.DecryptDS2Regulation(destPath);
            }

            Console.WriteLine($"Unpacking DS2 Regulation Bin: {fileName}...");
            using (var bndReader = new BND4Reader(bnd.Write()))
            {
                bndReader.Unpack(fileName, targetDir, progress);
            }

            return false;
        }

        throw new InvalidOperationException(
            "This state is unreachable. Please contact Nordgaren about this regulation.bin.");
    }

    public static bool Confirm(string message)
    {
        ConsoleKey response;
        do
        {
            Console.Write($"{message} [y/n] ");
            response = Console.ReadKey(false).Key;
            if (response != ConsoleKey.Enter)
            {
                Console.WriteLine();
            }
        } while (response != ConsoleKey.Y && response != ConsoleKey.N);

        return (response == ConsoleKey.Y);
    }

    private static bool RepackDir(string sourceDir, IProgress<float> progress)
    {
        string sourceName = new DirectoryInfo(sourceDir).Name;
        string targetDir = new DirectoryInfo(sourceDir).Parent.FullName;


        if (File.Exists($"{sourceDir}\\_witchy-bnd3.xml") || File.Exists($"{sourceDir}\\_yabber-bnd3.xml"))
        {
            Console.WriteLine($"Repacking BND3: {sourceName}...");
            WBND3.Repack(sourceDir, targetDir);
        }
        else if (File.Exists($"{sourceDir}\\_witchy-ffxbnd.xml"))
        {
            Console.WriteLine($"Repacking FFXBND: {sourceName}...");
            WFFXBND.Repack(sourceDir, targetDir);
        }
        else if (File.Exists($"{sourceDir}\\_witchy-bnd4.xml") || File.Exists($"{sourceDir}\\_yabber-bnd4.xml"))
        {
            Console.WriteLine($"Repacking BND4: {sourceName}...");
            WBND4.Repack(sourceDir, targetDir);
        }
        else if (File.Exists($"{sourceDir}\\_witchy-bxf3.xml") || File.Exists($"{sourceDir}\\_yabber-bxf3.xml"))
        {
            Console.WriteLine($"Repacking BXF3: {sourceName}...");
            WBXF3.Repack(sourceDir, targetDir);
        }
        else if (File.Exists($"{sourceDir}\\_witchy-bxf4.xml") || File.Exists($"{sourceDir}\\_yabber-bxf4.xml"))
        {
            Console.WriteLine($"Repacking BXF4: {sourceName}...");
            WBXF4.Repack(sourceDir, targetDir);
        }
        else if (File.Exists($"{sourceDir}\\_witchy-tpf.xml") || File.Exists($"{sourceDir}\\_yabber-tpf.xml"))
        {
            Console.WriteLine($"Repacking TPF: {sourceName}...");
            return WTPF.Repack(sourceDir, targetDir);
        }
        else
        {
            Console.WriteLine($"WitchyBND or Yabber XML not found in: {sourceName}");
            return true;
        }

        if (sourceName.Contains("regulation-bnd-dcx") || sourceName.Contains("Data0") ||
            sourceName.Contains("regulation-bin"))
            return ReEncryptRegulationFile(sourceName, sourceDir, targetDir, progress);

        return false;
    }

    private static bool ReEncryptRegulationFile(string sourceName, string sourceDir, string targetDir,
        IProgress<float> progress)
    {
        XmlDocument xml = new XmlDocument();

        xml.Load(WBUtil.GetXmlPath("bnd4", sourceDir));

        string filename = xml.SelectSingleNode("bnd4/filename").InnerText;
        string regFile = $"{targetDir}\\{filename}";

        if (filename.Contains("regulation.bin"))
        {
            BND4 bnd = BND4.Read(regFile);
            SFUtil.EncryptERRegulation(regFile, bnd);
            return false;
        }

        if (filename.Contains("Data0"))
        {
            BND4 bnd = BND4.Read(regFile);
            SFUtil.EncryptDS3Regulation(regFile, bnd);
            return false;
        }

        if (filename.Contains("enc_regulation.bnd.dcx"))
        {
            if (!Confirm(
                    "DS2 files cannot be re-encrypted, yet, so re-packing this folder might ruin your encrypted bnd."))
            {
                return false;
            }

            string destPath = Path.Combine(sourceDir, sourceName);
            BND4
                bnd = BND4.Read(
                    destPath); //WBUtil.DecryptDS2Regulation(destPath); I will have to investigate re-encrypting DS2 regulation later.
            Console.WriteLine($"Repacking DS2 Regulation Bin: {sourceName}...");
            WBND4.Repack(sourceDir, targetDir);
            return false;
        }

        throw new InvalidOperationException(
            "This state is unreachable. If your regulation bin is named correctly, please contact Nordgaren about this regulation.bin. Otherwise" +
            "make sure your bnd contains the original bnd name.");
    }

    public static void WriteError(string message)
    {
        PromptPlus.Error.WriteLine(message);
        AccruedErrors.Add(message);
    }

    static Program()
    {
        Configuration = new Configuration
        {
            Dry = false,
            Bnd = false,
            Dcx = false
        };
        AccruedErrors = new List<string>();
    }
}