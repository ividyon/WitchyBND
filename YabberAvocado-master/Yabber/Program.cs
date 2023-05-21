using SoulsFormats;
using SoulsFormats.AC4;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Yabber
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                Console.WriteLine(
                    $"{assembly.GetName().Name} {assembly.GetName().Version}\n\n" +
                    "Yabber has no GUI.\n" +
                    "Drag and drop a file onto the exe to unpack it,\n" +
                    "or an unpacked folder to repack it.\n\n" +
                    "DCX files will be transparently decompressed and recompressed;\n" +
                    "If you need to decompress or recompress an unsupported format,\n" +
                    "use Yabber.DCX instead.\n\n" +
                    "Press any key to exit."
                    );
                Console.ReadKey();
                return;
            }

            bool pause = false;

            foreach (string path in args)
            {
                try
                {
                    int maxProgress = Console.WindowWidth - 1;
                    int lastProgress = 0;
                    void report(float value)
                    {
                        int nextProgress = (int)Math.Ceiling(value * maxProgress);
                        if (nextProgress > lastProgress)
                        {
                            for (int i = lastProgress; i < nextProgress; i++)
                            {
                                if (i == 0)
                                    Console.Write('[');
                                else if (i == maxProgress - 1)
                                    Console.Write(']');
                                else
                                    Console.Write('=');
                            }
                            lastProgress = nextProgress;
                        }
                    }
                    IProgress<float> progress = new Progress<float>(report);

                    if (Directory.Exists(path))
                    {
                        pause |= ManageDir(path, progress);

                    }
                    else if (File.Exists(path))
                    {
                        pause |= ManageFile(path, progress, false);
                    }
                    else
                    {
                        Console.WriteLine($"File or directory not found: {path}");
                        pause = true;
                    }

                    if (lastProgress > 0)
                    {
                        progress.Report(1);
                        Console.WriteLine();
                    }
                }
                catch (DllNotFoundException ex) when (ex.Message.Contains("oo2core_6_win64.dll"))
                {
                    Console.WriteLine("In order to decompress .dcx files from Sekiro, you must copy oo2core_6_win64.dll from Sekiro into Yabber's lib folder.");
                    pause = true;
                }
                catch (UnauthorizedAccessException)
                {
                    using (Process current = Process.GetCurrentProcess())
                    {
                        var admin = new Process();
                        admin.StartInfo = current.StartInfo;
                        admin.StartInfo.FileName = current.MainModule.FileName;
                        admin.StartInfo.Arguments = Environment.CommandLine.Replace($"\"{Environment.GetCommandLineArgs()[0]}\"", "");
                        admin.StartInfo.Verb = "runas";
                        admin.Start();
                        return;
                    }
                }
                catch (FriendlyException ex)
                {
                    Console.WriteLine();
                    Console.WriteLine($"Error: {ex.Message}");
                    pause = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine($"Unhandled exception: {ex}");
                    pause = true;
                }

                Console.WriteLine();
            }

            if (pause)
            {
                Console.WriteLine("One or more errors were encountered and displayed above.\nPress any key to exit.");
                Console.ReadKey();
            }
        }

        private static bool ManageFile(string sourceFile, IProgress<float> progress, bool shouldMoveToBak)
        {
            string sourceDir = Path.GetDirectoryName(sourceFile);
            string filename = Path.GetFileName(sourceFile);
            string targetDir = $"{sourceDir}\\{filename.Replace('.', '-')}";
            if (File.Exists(targetDir))
                targetDir += "-ybr";

            bool isDCX = DCX.Is(sourceFile);
            DCX.Type compression = DCX.Type.Unknown;

            if (isDCX)
            {
                Console.WriteLine($"Decompressing DCX: {filename}...");


                byte[] bytes = DCX.Decompress(sourceFile, out DCX.Type compr);
                compression = compr;

                File.Move(sourceFile, $"{sourceFile}.temp");
                File.WriteAllBytes(sourceFile, bytes);
            }

            if (BND3.Is(sourceFile))
            {
                Console.WriteLine($"Unpacking BND3: {filename}...");
                using (var bnd = new BND3Reader(sourceFile))
                {
                    if (isDCX) bnd.Compression = compression;
                    bnd.Unpack(filename, targetDir, progress);
                }
            }
            else if (BND4.Is(sourceFile))
            {
                Console.WriteLine($"Unpacking BND4: {filename}...");
                using (var bnd = new BND4Reader(sourceFile))
                {
                    if (isDCX) bnd.Compression = compression;
                    bnd.Unpack(filename, targetDir, progress);
                }
            }
            else if (BXF3.IsBHD(sourceFile))
            {
                string bdtExtension = Path.GetExtension(filename).Replace("bhd", "bdt");
                string bdtFilename = $"{Path.GetFileNameWithoutExtension(filename)}{bdtExtension}";
                string bdtPath = $"{sourceDir}\\{bdtFilename}";
                if (File.Exists(bdtPath))
                {
                    Console.WriteLine($"Unpacking BXF3: {filename}...");
                    using (var bxf = new BXF3Reader(sourceFile, bdtPath))
                    {
                        bxf.Unpack(filename, bdtFilename, targetDir, progress);
                    }
                    if (shouldMoveToBak) YBUtil.Backup(bdtFilename);
                }
                else
                {
                    Console.WriteLine($"BDT not found for BHD: {filename}");
                    return true;
                }
            }
            else if (BXF4.IsBHD(sourceFile))
            {
                string bdtExtension = Path.GetExtension(filename).Replace("bhd", "bdt");
                string bdtFilename = $"{Path.GetFileNameWithoutExtension(filename)}{bdtExtension}";
                string bdtPath = $"{sourceDir}\\{bdtFilename}";
                if (File.Exists(bdtPath))
                {
                    Console.WriteLine($"Unpacking BXF4: {filename}...");
                    using (var bxf = new BXF4Reader(sourceFile, bdtPath))
                    {
                        bxf.Unpack(filename, bdtFilename, targetDir, progress);
                    }
                    if (shouldMoveToBak) YBUtil.Backup(bdtFilename);
                }
                else
                {
                    Console.WriteLine($"BDT not found for BHD: {filename}");
                    return true;
                }
            }
            else if (FFXDLSE.Is(sourceFile))
            {
                Console.WriteLine($"Unpacking FFX: {filename}...");
                var ffx = FFXDLSE.Read(sourceFile);
                if (isDCX) ffx.Compression = compression;
                ffx.Unpack(sourceFile);
            }
            else if (sourceFile.EndsWith(".ffx.xml") || sourceFile.EndsWith(".ffx.dcx.xml"))
            {
                Console.WriteLine($"Repacking FFX: {filename}...");
                YFFX.Repack(sourceFile);
            }
            else if (sourceFile.EndsWith(".fmg") || sourceFile.EndsWith(".fmg.dcx"))
            {
                Console.WriteLine($"Unpacking FMG: {filename}...");
                FMG fmg = FMG.Read(sourceFile);
                if (isDCX) fmg.Compression = compression;
                fmg.Unpack(sourceFile);
            }
            else if (sourceFile.EndsWith(".fmg.xml") || sourceFile.EndsWith(".fmg.dcx.xml"))
            {
                Console.WriteLine($"Repacking FMG: {filename}...");
                YFMG.Repack(sourceFile);
            }
            else if (GPARAM.Is(sourceFile))
            {
                Console.WriteLine($"Unpacking GPARAM: {filename}...");
                GPARAM gparam = GPARAM.Read(sourceFile);
                if (isDCX) gparam.Compression = compression;
                gparam.Unpack(sourceFile);
            }
            else if (sourceFile.EndsWith(".gparam.xml") || sourceFile.EndsWith(".gparam.dcx.xml")
                    || sourceFile.EndsWith(".fltparam.xml") || sourceFile.EndsWith(".fltparam.dcx.xml"))
            {
                Console.WriteLine($"Repacking GPARAM: {filename}...");
                YGPARAM.Repack(sourceFile);
            }
            else if (FXR3.Is(sourceFile))
            {
                Console.WriteLine($"Unpacking FXR3: {filename}...");
                FXR3 fxr = FXR3.Read(sourceFile);
                if (isDCX) fxr.Compression = compression;
                fxr.Unpack(sourceFile);
            }
            else if (sourceFile.EndsWith(".fxr.xml") || sourceFile.EndsWith(".fxr.dcx.xml"))
            {
                Console.WriteLine($"Repacking FXR3: {filename}...");
                YFXR3.Repack(sourceFile);
            }
            else if (sourceFile.EndsWith(".btab") || sourceFile.EndsWith(".btab.dcx"))
            {
                Console.WriteLine($"Unpacking BTAB: {filename}...");
                BTAB btab = BTAB.Read(sourceFile);
                if (isDCX) btab.Compression = compression;
                btab.Unpack(sourceFile);
            }
            else if (sourceFile.EndsWith(".btab.json") || sourceFile.EndsWith(".btab.dcx.json"))
            {
                Console.WriteLine($"Repacking BTAB: {filename}...");
                YBTAB.Repack(sourceFile);
            }
            else if (sourceFile.EndsWith(".matbin"))
            {
                Console.WriteLine($"Unpacking MATBIN: {filename}...");
                MATBIN matbin = MATBIN.Read(sourceFile);
                if (isDCX) matbin.Compression = compression;
                matbin.Unpack(sourceFile);
            }
            else if (sourceFile.EndsWith(".matbin.xml"))
            {
                Console.WriteLine($"Repacking MATBIN: {filename}...");
                YMATBIN.Repack(sourceFile);
            }
            else if (sourceFile.EndsWith(".mtd"))
            {
                Console.WriteLine($"Unpacking MTD: {filename}...");
                MTD mtd = MTD.Read(sourceFile);
                if (isDCX) mtd.Compression = compression;
                mtd.Unpack(sourceFile);
            }
            else if (sourceFile.EndsWith(".mtd.xml"))
            {
                Console.WriteLine($"Repacking MTD: {filename}...");
                YMTD.Repack(sourceFile);
            }
            else if (sourceFile.EndsWith(".msb") || sourceFile.EndsWith(".msb.dcx"))
            {

                Console.WriteLine($"Unpacking MSB: {filename}...");

                if (File.Exists($"{sourceDir}\\_er"))
                {
                    var msb = MSBE.Read(sourceFile);
                    if (isDCX) msb.Compression = compression;
                    msb.Unpack(sourceFile);
                }
                else if (File.Exists($"{sourceDir}\\_sekiro"))
                {
                    var msb = MSBS.Read(sourceFile);
                    if (isDCX) msb.Compression = compression;
                    msb.Unpack(sourceFile);
                }
                else if (File.Exists($"{sourceDir}\\_bb"))
                {
                    var msb = MSBB.Read(sourceFile);
                    if (isDCX) msb.Compression = compression;
                    msb.Unpack(sourceFile);
                }
                else if (File.Exists($"{sourceDir}\\_des"))
                {
                    var msb = MSBD.Read(sourceFile);
                    if (isDCX) msb.Compression = compression;
                    msb.Unpack(sourceFile);
                }
                else if (File.Exists($"{sourceDir}\\_ds3"))
                {
                    var msb = MSB3.Read(sourceFile);
                    if (isDCX) msb.Compression = compression;
                    msb.Unpack(sourceFile);
                }
                else if (File.Exists($"{sourceDir}\\_ds2"))
                {
                    var msb = MSB2.Read(sourceFile);
                    if (isDCX) msb.Compression = compression;
                    msb.Unpack(sourceFile);
                }
                else if (File.Exists($"{sourceDir}\\_ds1"))
                {
                    var msb = MSB1.Read(sourceFile);
                    if (isDCX) msb.Compression = compression;
                    msb.Unpack(sourceFile);
                }
                else
                {
                    Console.WriteLine($"Create a file with name corresponding to the game.");
                    Console.WriteLine($"Valid names: _er, _sekiro, _bb, _des, _ds3, _ds2, _ds1");
                    return true;
                }
            }
            else if (sourceFile.EndsWith(".msb.json") || sourceFile.EndsWith(".msb.dcx.json"))
            {
                Console.WriteLine($"Repacking MSB: {filename}...");

                if (File.Exists($"{sourceDir}\\_er"))
                {
                    YMSBE.Repack(sourceFile);
                }
                else if (File.Exists($"{sourceDir}\\_sekiro"))
                {
                    YMSBS.Repack(sourceFile);
                }
                else if (File.Exists($"{sourceDir}\\_bb"))
                {
                    YMSBB.Repack(sourceFile);
                }
                else if (File.Exists($"{sourceDir}\\_des"))
                {
                    YMSBD.Repack(sourceFile);
                }
                else if (File.Exists($"{sourceDir}\\_ds3"))
                {
                    YMSB3.Repack(sourceFile);
                }
                else if (File.Exists($"{sourceDir}\\_ds2"))
                {
                    YMSB2.Repack(sourceFile);
                }
                else if (File.Exists($"{sourceDir}\\_ds1"))
                {
                    YMSB1.Repack(sourceFile);
                }
                else
                {
                    Console.WriteLine($"Create a file with name corresponding to the game.");
                    Console.WriteLine($"Valid names: _er, _sekiro, _bb, _des, _ds3, _ds2, _ds1");
                    return true;
                }
            }
            else if (sourceFile.EndsWith(".btl") || sourceFile.EndsWith(".btl.dcx"))
            {
                Console.WriteLine($"Unpacking BTL: {filename}...");
                BTL btl = BTL.Read(sourceFile);
                if (isDCX) btl.Compression = compression;
                btl.Unpack(sourceFile);
            }
            else if (sourceFile.EndsWith(".btl.json") || sourceFile.EndsWith(".btl.dcx.json"))
            {
                Console.WriteLine($"Repacking BTL: {filename}...");
                YBTL.Repack(sourceFile);
            }
            else if (sourceFile.EndsWith(".luagnl"))
            {
                Console.WriteLine($"Unpacking LUAGNL: {filename}...");
                LUAGNL gnl = LUAGNL.Read(sourceFile);
                if (isDCX) gnl.Compression = compression;
                gnl.Unpack(sourceFile);
            }
            else if (sourceFile.EndsWith(".luagnl.xml"))
            {
                Console.WriteLine($"Repacking LUAGNL: {filename}...");
                YLUAGNL.Repack(sourceFile);
            }
            else if (LUAINFO.Is(sourceFile))
            {
                Console.WriteLine($"Unpacking LUAINFO: {filename}...");
                LUAINFO info = LUAINFO.Read(sourceFile);
                if (isDCX) info.Compression = compression;
                info.Unpack(sourceFile);
            }
            else if (sourceFile.EndsWith(".luainfo.xml"))
            {
                Console.WriteLine($"Repacking LUAINFO: {filename}...");
                YLUAINFO.Repack(sourceFile);
            }
            else if (TPF.Is(sourceFile))
            {
                Console.WriteLine($"Unpacking TPF: {filename}...");
                TPF tpf = TPF.Read(sourceFile);
                if (isDCX) tpf.Compression = compression;
                tpf.Unpack(filename, targetDir, progress);
            }
            else if (Zero3.Is(sourceFile))
            {
                Console.WriteLine($"Unpacking 000: {filename}...");
                Zero3 z3 = Zero3.Read(sourceFile);
                z3.Unpack(targetDir);
            }
            else if (MQB.Is(sourceFile))
            {
                Console.WriteLine($"Converting MQB: {filename}...");
                MQB mqb = MQB.Read(sourceFile);
                mqb.Unpack(filename, sourceDir, progress);
            }
            else if (sourceFile.EndsWith(".mqb.xml"))
            {
                Console.WriteLine($"Converting XML to MQB: {filename}...");
                YMQB.Repack(sourceFile);
            }
            else
            {
                Console.WriteLine($"File format not recognized: {filename}");
                return true;
            }

            if (isDCX)
            {
                File.Delete(sourceFile);
                File.Move($"{sourceFile}.temp", sourceFile);
            }

            if (shouldMoveToBak) YBUtil.Backup(sourceFile);

            return false;
        }

        private static bool ManageDir(string sourceDir, IProgress<float> progress)
        {
            string sourceDirName = new DirectoryInfo(sourceDir).Name;
            string targetDir = new DirectoryInfo(sourceDir).Parent.FullName;

            if (File.Exists($"{sourceDir}\\_witchy-bnd3.xml"))
            {
                Console.WriteLine($"Repacking BND3: {sourceDirName}...");
                YBND3.Repack(sourceDir, targetDir);
                Path.Combine(Directory.GetCurrentDirectory(), filename)
            }
            else if (File.Exists($"{sourceDir}\\_witchy-bnd4.xml"))
            {
                Console.WriteLine($"Repacking BND4: {sourceDirName}...");
                YBND4.Repack(sourceDir, targetDir);
            }
            else if (File.Exists($"{sourceDir}\\_witchy-bxf3.xml"))
            {
                Console.WriteLine($"Repacking BXF3: {sourceDirName}...");
                YBXF3.Repack(sourceDir, targetDir);
            }
            else if (File.Exists($"{sourceDir}\\_witchy-bxf4.xml"))
            {
                Console.WriteLine($"Repacking BXF4: {sourceDirName}...");
                YBXF4.Repack(sourceDir, targetDir);
            }
            else if (File.Exists($"{sourceDir}\\_witchy-tpf.xml"))
            {
                Console.WriteLine($"Repacking TPF: {sourceDirName}...");
                YTPF.Repack(sourceDir, targetDir);
                Path.Combine(Directory.GetCurrentDirectory(), filename)
            }
            else
            {
                foreach (string sourceFile in Directory.EnumerateFiles(sourceDir))
                {
                    ManageFile(sourceFile, progress, true);
                }

                foreach (string dir in Directory.EnumerateDirectories(sourceDir))
                {
                    string dirName = new DirectoryInfo(dir).Name;

                    if (!dirName.StartsWith("BAK"))
                    {
                        ManageDir(dir, progress);
                    }
                }
            }

            return false;
        }
    }
}
