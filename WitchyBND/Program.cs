using SoulsFormats;
using SoulsFormats.AC4;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using System.Xml;
using Microsoft.Extensions.FileSystemGlobbing;
using WitchyFormats;
using Yabber;
using GPARAM = WitchyFormats.GPARAM;
using MATBIN = WitchyFormats.MATBIN;
using MTD = WitchyFormats.MTD;
using TPF = WitchyFormats.TPF;
using PARAM = WitchyFormats.PARAM;

namespace WitchyBND
{
    [SupportedOSPlatform("windows")]
    class Program
    {

        static WBUtil.GameType? game;

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            if (args.Length == 0)
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                Console.WriteLine(
                    $"{assembly.GetName().Name} {assembly.GetName().Version}\n\n" +
                    "WitchyBND has no GUI.\n" +
                    "Drag and drop a file onto the exe to unpack it,\n" +
                    "or an unpacked folder to repack it.\n\n" +
                    "DCX files will be transparently decompressed and recompressed;\n" +
                    "If you need to decompress or recompress an unsupported format,\n" +
                    "use WitchyBND.DCX instead.\n\n" +
                    "Press any key to exit."
                );
                Console.ReadKey();
                return;
            }

            bool error = false;
            int errorcode = 0;

            var paths = new List<string>();
            foreach (string arg in args)
            {
                if (arg.Contains('*'))
                {
                    var matcher = new Matcher();
                    var rootParts = arg.Split(Path.DirectorySeparatorChar).TakeWhile(part => !part.Contains('*')).ToList();
                    var root = string.Join(Path.DirectorySeparatorChar, rootParts);
                    var rest = arg.Substring(root.Length + 1);

                    matcher = matcher.AddInclude(rest.Replace(Path.DirectorySeparatorChar.ToString(), "/"));
                    var files = Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, root), "*", SearchOption.AllDirectories);
                    var match = matcher.Match(Path.Combine(Environment.CurrentDirectory, root), files);
                    if (match.HasMatches)
                    {
                        paths.AddRange(match.Files.Select(m => Path.Combine(root, m.Path)).ToList());
                    }
                }
                else
                {
                    paths.Add(arg);
                }
            }
            
            bool pause = false;
            
            foreach (string halfPath in paths)
            {
                try
                {
                    string path = Path.GetFullPath(halfPath);
                 
                    // int maxProgress = Console.WindowWidth - 1;
                    // int lastProgress = 0;
                    //
                    // void report(float value)
                    // {
                    //     int nextProgress = (int)Math.Ceiling(value * maxProgress);
                    //     if (nextProgress > lastProgress)
                    //     {
                    //         for (int i = lastProgress; i < nextProgress; i++)
                    //         {
                    //             if (i == 0)
                    //                 Console.Write('[');
                    //             else if (i == maxProgress - 1)
                    //                 Console.Write(']');
                    //             else
                    //                 Console.Write('=');
                    //         }
                    //
                    //         lastProgress = nextProgress;
                    //     }
                    // }
                    //
                    // IProgress<float> progress = new Progress<float>(report);
                    IProgress<float> progress = new Progress<float>();

                    if (Directory.Exists(path))
                    {
                        error |= RepackDir(path, progress);

                    }

                    if (Directory.Exists(path))
                    {
                        pause |= ManageDir(path, progress);

                    }

                    else if (File.Exists(path))
                    {
                        error |= UnpackFile(path, progress);
                    }
                    else
                    {
                        Console.Error.WriteLine($"ERROR: File or directory not found: {path}");
                        errorcode = 2;
                        error = true;
                    }
                }
                catch (DllNotFoundException ex) when (ex.Message.Contains("oo2core_6_win64.dll"))
                {
                    Console.Error.WriteLine(
                        "ERROR: oo2core_6_win64.dll not found. Please copy this library from the game directory to WitchyBND's directory.");
                    errorcode = 3;
                    error = true;
                }
                catch (UnauthorizedAccessException)
                {
                    using (Process current = Process.GetCurrentProcess())
                    {
                        var admin = new Process();
                        admin.StartInfo = current.StartInfo;
                        admin.StartInfo.FileName = current.MainModule.FileName;
                        admin.StartInfo.Arguments =
                            Environment.CommandLine.Replace($"\"{Environment.GetCommandLineArgs()[0]}\"", "");
                        admin.StartInfo.Verb = "runas";
                        admin.Start();
                        return;
                    }
                }
                catch (FriendlyException ex)
                {
                    Console.WriteLine();
                    Console.Error.WriteLine($"ERROR: {ex.Message}");
                    errorcode = 4;
                    error = true;
                }
                #if (!DEBUG)
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.Error.WriteLine($"ERROR: Unhandled exception: {ex}");
                    errorcode = 1;
                    error = true;
                }
                #endif

                Console.WriteLine();
            }

            if (error)
            {
                Console.WriteLine("One or more errors were encountered and displayed above.\nPress any key to exit.");
                Console.ReadKey();
                Environment.Exit(errorcode);
            }
        }

        private static bool UnpackFile(string sourceFile, IProgress<float> progress)
        {
            string sourceDir = new FileInfo(sourceFile).Directory.FullName;
            string fileName = Path.GetFileName(sourceFile);
            string targetDir = $"{sourceDir}\\{fileName.Replace('.', '-')}";
            if (File.Exists(targetDir))
                targetDir += "-ybr";
            
            DCX.Type compression = DCX.Type.Unknown;

            if (fileName.Contains("regulation.bnd.dcx") || fileName.Contains("Data0") || fileName.Contains("regulation.bin") || fileName.Contains("regulation.bnd"))
                return UnpackRegulationFile(fileName, sourceDir, targetDir, progress);

            if (DCX.Is(sourceFile))
            {
                Console.WriteLine($"Decompressing DCX: {fileName}...");
                byte[] bytes = TryDecompressBytes(sourceFile, out DCX.Type compr);
                compression = compr;

                if (BND3.Is(bytes))
                {
                    Console.WriteLine($"Unpacking BND3: {fileName}...");
                    using (var bnd = new BND3Reader(bytes))
                    {
                        bnd.Compression = compression;
                        bnd.Unpack(fileName, targetDir, progress);
                    }
                }
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
                else if (sourceFile.EndsWith(".btl") || sourceFile.EndsWith(".btl.dcx"))
                {
                    Console.WriteLine($"Unpacking BTL: {fileName}...");
                    BTL btl = BTL.Read(sourceFile);
                    btl.Compression = compression;
                    btl.Unpack(sourceFile);
                }
                else if (sourceFile.EndsWith(".btl.json") || sourceFile.EndsWith(".btl.dcx.json"))
                {
                    Console.WriteLine($"Repacking BTL: {fileName}...");
                    YBTL.Repack(sourceFile);
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
                    YMQB.Repack(sourceFile);
                }
                
                else if (sourceFile.EndsWith(".zip"))
                {

                    Console.WriteLine(@"&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&
&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&
&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&
&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&#####&&&&&&&##&&&&&&&&######&&###&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&
&&&&&&&&&&&&&&&#&&&&&&&&&&&&&&&&#####&&&########&&&#######&&&####&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&
&&&&&&&&&&&&&&&#&&&&&&&&&&&&##&&###########################&&#&&&###&&&&&&####&&&&&&&&&&&&&&&&&&&&&&
&&&&&&&&&&&&&####&####################################################&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&
&&&&&###&&&###############################################################&&&&&&&&&&&&&&&&&&&&&&&&&&
###&#############################################################################&&&&&&&&&&&&&&&&&&&
#######&##########################################################################&&&##&&&&&&&&&&&&&
###############################################B#####################################&&#&&&&&&&&&&&&
##########################################BBBBBBBB##B###BB#BB#######################&&&&###&&&&&&&&&
#############BBBBBBBBBBB#######BBBBBBB#BBBBBBBBBBBBBBBBBBBBBBBBBBBB##################&&&&###&###&&&&
BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBGGGGGGBBBBBBBBBBBBB##########&###&&&##&#####&&
GGGGGGGPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPGGBB############&&&&##&&&#&&&&
PPPPPPPPPPPP55555555555555555555555555555555555555555555555555555555555GBBB#########&&&&&&&##&###&&&
PPPPPPPPPPPP5555555555555555555555555555555555YYYYY55555555555555555555PBBB#########&&##&&&&#&&&#&&#
PPPPPP5PP555555555555555555555555555555YY5555YYYYYY555555555555555YYYY5PBB##########&&###&&&&&&&#&##
PPPP5555555555555555555555555555555555Y5JJ55YY5YYYYYY5555555555YYYYYYYYPBB###########&##&&&&&&&&&&##
PPPP5555555555555555555555555555555555YG5^7YYYYY55YYY5555555YYYYYYYJYYYPB################&&&&&&&&&&#
PPP5555555555555555555555555555555555YYGG~:~PP5YYYY55555555YYYYJJJJJJYYPBB###############&&&&&&&&&&#
5555555555555555555555555555555555555J5GBJ~5##BGP55YYYYYYYYJJJYJJYYJ7JYPB##################&&&&&&&&&
555555555555555555555555555555555YYJJ?YPGPP5YJJY7!!!~~7JJ?YY5JJYJ??7?YYPB####&&##&#########&&&&&&&&&
555555555555555555555555555YYJJJ???JY5YJJ???7~^:...:..:?JJYY?JYJ?777YYYPB####&&##&&#&&####&&&&&&&&&&
55555555555555555555555YYJ????JJYYY55PP5YJJ?~^^:~^^^:..^7????7??J?????J5B#######&&&&&&&&#&&&&&#&&&&&
555555555555555555YYYJJJJ??Y555YYYYYY5PPPP5Y?^::YJY5Y7^::^~!7777777777!7?YG#&&###&&&&&&&&&&&###&&&&&
5555555555555YYYYY55GBBPY5555YYJJJJJJYJJJY5J^.^.^..::..  ..:~7?77!~7?7!~^^^!JP#&&&&&&&&&&&&&BBB##&&&
55555555555YYY5PB#&&&&B5555YYYJJJJJJJYJJYYPJ7?7.....::.. ....:~!7~::~^:......:!YG#####&&&&&&BBB##&&#
555555555YYPB#&&&&#BGP5YYYJJJJJJJJJJYY5GGPB#BGYY?7~:^^::.......::.............::~?PGBB##&&&&#B###&&#
5555555YJG&&##GPGBG5YJJ?????77????JYYYY55YG##GYY?!:^~~^::....................::.::^JPGGGB#&#B###&&&&
5555555?P#BGG55YYYYYJJ??77!!~~~~!7??YYYJ?77!~~^::.:^~~^::.......................:::^JPPPGGB#####&&&&
55555557PGPPP555YJJJJJ?77!!~^^^^^^!???JYJJ?7!!!!~~~^^::..........................::^~YPPGBBB#&######
PPPPPPY!?JY5PP55YJJ?????7!!~^^^^^^^^~~!777777!!~^^:::.......................:.....:^~7PGGBBBB#######
PPPGPJ7JJJ?J5PP5YYJ??????7!~~^^^^^^:::::::::::::::..........:...........:...::.:.^?PP?YGGGGBBB#&####
GGBP??5555YYY555YYJJ???77!!!!~~~~^^^^::..:...........:...::::::.:::.:.:::....:::.?##B!~YGGGGBBB#&&&#
B#BJJPPPPPP55555YYYJJ???77!!!!!!~~~^^^^:::::::::.::.::::::^^^:::::::::::::::::::.!5P!::~PGGGBBBB#&&#
##BYJ555PPPP5P555YYYJJJ???777!!!!!!!~~^~^^^^^^^^::^:^^^^:^^^^^^^^^:::^^:^^^^^^^^~7!?~^^~YBBBBBBB&&&#
###BPYYY5PPPPP55555YYYJJJ????777!!77!!!~!~~~!!!!~~~^~~~~~~~~~~~~^^^~~~~~~~~!~~~7?~!?!~~7PBBBBBB#&&&#
B###&#BP55PPPGGPP555YYYYJYJJJJJ?7777777777777!7!!!!77!!!!!!~!!!~~~~!!!!!!7!!!777~:!??7?YGBBBB##&####
GBB###&&#BGPGGGGPPP5555555YYYYJJJJJ?J??J??????77777777777777777!!!!777J5Y7!777!~^^?JJY5GBBBB#&######
PGGGGBBB######BBBGGGPPPPPP5555YYYYYYYYYYYJJJJJJJJJJJJJ??J?7?J?????????5P5JJ?7!~^~?YJYPGBBB#&########
PPPPPPPPPGGGBBB#####BBBGGGGPPPPP5P55555PPP5555555555YYYYYYJYYY5YYYYYY5PGP5J!~~^~5YYJ5BBB##&#########
555555555555555PPPPGGGBBBBBGGGGGGGGGGGGGGGGGGGGGPPPPPPPPPPPPPGGPPPPGGBGPJ7~~^^~Y5YYYYYY5GB##########
P5555555555555YYYYYYYY55555PPPPGGGGGGGGGGGGBBBBBBGGGGGGGBGGGGGGGGGGGPY?!~~^^^755YYYYJJJJYPBBBB######
PPPPPP5555555555YYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYPGYJ????????????7!~~~^^^~!J5YYYYYJJYYYYY5GBBB#####
PPPPPPPPP55555555555YYYYYYYYYYYYYYYYYYYYYYYYYYYYYYPG5?!~^^^^^~~^^^^~~!777?Y5YYY5YYJYYYYYYYY5GBBB####
PPPPPPPPPP5555555555555555555555YYYYYYYYYYYYYYYYYYYY5P55YJJ??777?JJY5P555YYY55YYJYYYYYYYYYYY5PGBB###
PPPPPPPPPPP555555555555555555555555YYYYY55555PPPPPPPPGP5YY5555YY5YYYYYY55555YYYYYYYYYYYYY55555PGBB##
GGGPPPPPPPPPPPPPPP5555555555555555555555555555PPPGGGBBBBBGGPPP55555PPPP555YYYYYYYYYYYY55555555PPGGB#
GGGGGGGGPPPPPPPPPPPP55PPP5555555555555555555555555555PPPPPPPPPPPPP5555YYYYYYYYYYY5555555555555PPPGGB
GGGGGGGGGGGPPPPPPPPPPPPPPPPPPPP55555555555555555555555555555555Y555555555555555555555555PPPP55PPPGGG
GGGGGGGGGGGGGGGPPPPPPPPPPPPPPPPPPPPPPPPPPP555555555555555555555555555555555555555555PPPPPPPPPPPPGGGG
BBGGGGGGGGGGGGGGGGPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPP55555555555555555555PPPP5PPPPPPPPPPPPPPPPPPGGGGGGG
BBBBBBGGGGGGGGGGGGGGGGGGGGGGGPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPGGGGGGGGGGGGB
BBBBBBBBBGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPGGGGGGGGGGGGGGGGBBBBBB
BBBBBBBBBBBBBBGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGBBBBBBBBB
BBBBBBBBBBBBBBBBBBBBBGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGBBGBBBBBBBBBBBB
####BBBBBBBBBBBBBBBBBBBBBBBBBBGBBBBGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGBBBBBBBBBBBBBBBBBBB##BB#
########BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB#P7JB##
#############BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB#GJ5###
###############BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB#########");
                    System.Threading.Thread.Sleep(50000);
                }


                    else
                {
                    Console.WriteLine($"File format not recognized: {fileName}");
                    return true;
                }


            }

            return false; 
            
           
            
        }
        private static byte[] TryDecompressBytes(string sourceFile, out DCX.Type compression)
        {
            try
            {
                return DCX.Decompress(sourceFile, out compression);
            }
            catch (DllNotFoundException ex) when (ex.Message.Contains("oo2core_6_win64.dll"))
            {
                string oo2corePath = WBUtil.GetOodlePath();
                if (oo2corePath == null)
                    throw;

                IntPtr handle = Kernel32.LoadLibrary(oo2corePath);
                byte[] bytes = DCX.Decompress(sourceFile, out compression);
                Kernel32.FreeLibrary(handle);
                return bytes;
            }
        }

        private static bool UnpackRegulationFile(string fileName, string sourceDir, string targetDir, IProgress<float> progress)
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

            throw new InvalidOperationException("This state is unreachable. Please contact Nordgaren about this regulation.bin.");
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

            if (sourceName.Contains("regulation-bnd-dcx") || sourceName.Contains("Data0") || sourceName.Contains("regulation-bin"))
                return ReEncryptRegulationFile( sourceName, sourceDir, targetDir, progress);

            return false;
        }

        private static bool ReEncryptRegulationFile(string sourceName, string sourceDir, string targetDir, IProgress<float> progress)
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
                if (!Confirm("DS2 files cannot be re-encrypted, yet, so re-packing this folder might ruin your encrypted bnd."))
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

            throw new InvalidOperationException("This state is unreachable. If your regulation bin is named correctly, please contact Nordgaren about this regulation.bin. Otherwise" +
                "make sure your bnd contains the original bnd name.");
        }
        private static bool ManageDir(string sourceDir, IProgress<float> progress)
        {
            string sourceDirName = new DirectoryInfo(sourceDir).Name;
            string targetDir = new DirectoryInfo(sourceDir).Parent.FullName;

            if (File.Exists($"{sourceDir}\\_witchy-bnd3.xml"))
            {
                Console.WriteLine($"Repacking BND3: {sourceDirName}...");
                WBND3.Repack(sourceDir, targetDir);
            }
            else if (File.Exists($"{sourceDir}\\_witchy-bnd4.xml"))
            {
                Console.WriteLine($"Repacking BND4: {sourceDirName}...");
                WBND4.Repack(sourceDir, targetDir);
            }
            else if (File.Exists($"{sourceDir}\\_witchy-bxf3.xml"))
            {
                Console.WriteLine($"Repacking BXF3: {sourceDirName}...");
                WBXF3.Repack(sourceDir, targetDir);
            }
            else if (File.Exists($"{sourceDir}\\_witchy-bxf4.xml"))
            {
                Console.WriteLine($"Repacking BXF4: {sourceDirName}...");
                WBXF4.Repack(sourceDir, targetDir);
            }
            else if (File.Exists($"{sourceDir}\\_witchy-tpf.xml"))
            {
                Console.WriteLine($"Repacking TPF: {sourceDirName}...");
                WTPF.Repack(sourceDir, targetDir);
            }
            else
            {
                foreach (string sourceFile in Directory.EnumerateFiles(sourceDir))
                {
                    UnpackFile(sourceFile, progress);
                }

                foreach (string dir in Directory.EnumerateDirectories(sourceDir))
                {
                    string dirName = new DirectoryInfo(dir).Name;

                    if (!dirName.EndsWith("bak"))
                    {
                        ManageDir(dir, progress);
                    }
                }
            }

            return false;
        }
    }
}
