using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using SoulsFormats;
using WitchyBND.Parsers;
using WitchyBND.Services;
using WitchyLib;
using ServiceProvider = WitchyBND.Services.ServiceProvider;

namespace WitchyBND.CliModes;

public static class WatcherMode
{
    private static readonly IErrorService errorService;

    private const int ProcessDelay = 200; // Guard against IOExceptions when other programs aren't done editing (FLVER Editor)
    private const int EventRepeatThreshold = 200; // Guard against double change event bug in FileSystemWatcher

    private static readonly IOutputService output;

    static WatcherMode()
    {
        output = ServiceProvider.GetService<IOutputService>();
        errorService = ServiceProvider.GetService<IErrorService>();
    }
    private class WatchedFile
    {
        public WatchedFile(string path, FileSystemWatcher watcher, WFileParser parser)
        {
            Path = path;
            Watcher = watcher;
            Parser = parser;
        }

        public string Path { get; }
        public FileSystemWatcher Watcher { get; }
        public WFileParser Parser { get; }

        public DateTime LastChange { get; set; } = DateTime.Now;
    }

    private class WatchedFileFolder : WatchedFile
    {
        public WatchedFileFolder(string path, FileSystemWatcher watcher, WFileParser parser, List<string> fileList) :
            base(path, watcher, parser)
        {
            FileList = fileList;
        }

        public List<string> FileList { get; }
    }

    private class WatchedFileUnpack : WatchedFile
    {
        public WatchedFileUnpack(string path, FileSystemWatcher watcher, WFileParser parser, ISoulsFile file) : base(
            path, watcher, parser)
        {
            File = file;
        }

        public ISoulsFile File { get; }
    }

    private static Dictionary<string, WatchedFile> _watchedFiles = new();

    internal static void CliWatcherMode(CliOptions opt)
    {
        var paths = opt.Paths.ToList();
        paths = OSPath.ProcessPathGlobs(paths);
        WatchFiles(paths);
    }

    public static void WatchFiles(List<string> paths)
    {
        Dictionary<string, WFileParser> repack = new();
        Dictionary<string, (WFileParser, ISoulsFile)> unpack = new();
        paths.ForEach(p => {
            foreach (WFileParser parser in ParseMode.GetParsers(true))
            {
                if (parser.Exists(p) && parser.Is(p, null, out ISoulsFile? file))
                {
                    unpack.TryAdd(p, (parser, file));
                    return;
                }

                if (parser.ExistsUnpacked(p) && parser.IsUnpacked(p))
                {
                    repack.TryAdd(p, parser);
                    return;
                }
            }

            output.WriteError($"No valid unpacking parser found for {OSPath.GetFileName(p)}."
                .PromptPlusEscape());
        });

        var foldersToRepack = repack.Where(p => p.Value is WFolderParser).ToList();
        var filesToRepack = repack.Where(p => p.Value is WSingleFileParser).ToList();
        var filesToUnpack = unpack.Where(p => p.Value.Item1 is WSingleFileParser).ToList();

        var count = foldersToRepack.Count + filesToRepack.Count + filesToUnpack.Count;

        if (count == 0)
        {
            output.WriteLine("Could not find valid parsers for any selected files. Aborting.");
            return;
        }

        foreach ((string path, var (parser, file)) in filesToUnpack)
        {
            bool parsed = errorService.Catch(() => {
                    ParseMode.Unpack(path, file, file.Compression, parser, false);
                    return true;
                },
                out bool error, path);
            ParseMode.PrintParseSuccess(path, parsed, error, false);

            var watcher = new FileSystemWatcher();
            watcher.Path = OSPath.GetDirectoryName(OSPath.GetFullPath(path))!;
            watcher.Filter = OSPath.GetFileName(path);
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Changed += WatchUnpack;
            watcher.Deleted += WatchRemoval;
            watcher.Renamed += WatchRemoval;

            string fullPath = OSPath.Combine(watcher.Path, watcher.Filter);
            output.WriteLine($"Watching path \"{fullPath}\" for unpacking...".PromptPlusEscape());
            _watchedFiles.TryAdd(fullPath, new WatchedFileUnpack(fullPath, watcher, parser, file));

            watcher.EnableRaisingEvents = true;
        }

        foreach ((string path, WFileParser parser) in filesToRepack)
        {
            bool parsed = errorService.Catch(() => {
                    ParseMode.Repack(path, parser, false);
                    return true;
                },
                out bool error, path);
            ParseMode.PrintParseSuccess(path, parsed, error, false);

            var watcher = new FileSystemWatcher();
            watcher.Path = OSPath.GetDirectoryName(OSPath.GetFullPath(path))!;
            watcher.Filter = OSPath.GetFileName(path);
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Changed += WatchRepack;
            watcher.Deleted += WatchRemoval;
            watcher.Renamed += WatchRemoval;

            string fullPath = OSPath.Combine(watcher.Path, watcher.Filter);
            output.WriteLine($"Watching path \"{fullPath}\" for repacking...".PromptPlusEscape());
            _watchedFiles.TryAdd(fullPath, new WatchedFile(fullPath, watcher, parser));

            watcher.EnableRaisingEvents = true;
        }

        foreach ((string path, WFileParser parser) in foldersToRepack)
        {
            var folderParser = parser as WFolderParser;
            XElement xml = WFileParser.LoadXml(folderParser!.GetFolderXmlPath(path));
            XElement? filesEl = xml.Element("files");
            if (filesEl == null) continue;
            var files = WFolderParser.GetFolderFilePaths(filesEl, path).Select(p => OSPath.GetFullPath(p)).ToList();
            var watcher = new FileSystemWatcher();
            watcher.Path = OSPath.GetFullPath(path);
            watcher.Filter = "*.*";
            watcher.IncludeSubdirectories = true;
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Changed += WatchFolder;
            watcher.Deleted += WatchRemoval;
            watcher.Renamed += WatchRemoval;

            string fullPath = watcher.Path;
            output.WriteLine($"Watching path \"{fullPath}\" for repacking...".PromptPlusEscape());
            _watchedFiles.TryAdd(fullPath, new WatchedFileFolder(fullPath, watcher, parser, files));

            watcher.EnableRaisingEvents = true;
        }

        output.WriteLine(
            $"Watching {count} path(s) for changes.");

        output.KeyPress(@"Press any key to stop watching files...
").Run();
    }

    private static void WatchRemoval(object sender, FileSystemEventArgs e)
    {
        output.WriteLine(
            $"Path {OSPath.GetFileName(e.Name)} was renamed or deleted, aborting watch.".PromptPlusEscape());
        _watchedFiles[e.FullPath].Watcher.EnableRaisingEvents = false;
        _watchedFiles.Remove(e.FullPath);
    }

    private static void WatchUnpack(object sender, FileSystemEventArgs e)
    {
        var file = _watchedFiles[e.FullPath] as WatchedFileUnpack;
        if (DateTime.Now.Subtract(file.LastChange).TotalMilliseconds < EventRepeatThreshold)
            return;
        file.LastChange = DateTime.Now; // In case of exceptions
        Thread.Sleep(ProcessDelay);
        output.WriteLine($"[{DateTime.Now:T}] Detected change in {e.Name}.".PromptPlusEscape());
        bool parsed = errorService.Catch(() => {
                ParseMode.Unpack(e.FullPath, file!.File, file.File.Compression, file.Parser, false);
                return true;
            },
            out bool error, e.FullPath);
        ParseMode.PrintParseSuccess(file.Path, parsed, error, false);
        file.LastChange = DateTime.Now; // Due to long-lasting operations
    }

    private static void WatchRepack(object sender, FileSystemEventArgs e)
    {
        var file = _watchedFiles[e.FullPath];
        if (DateTime.Now.Subtract(file.LastChange).TotalMilliseconds < EventRepeatThreshold)
            return;
        file.LastChange = DateTime.Now; // In case of exceptions
        Thread.Sleep(ProcessDelay);
        output.WriteLine($"[{DateTime.Now:T}] Detected change in {e.Name}.".PromptPlusEscape());
        bool parsed = errorService.Catch(() => {
            ParseMode.Repack(e.FullPath, file.Parser, false);
            return true;
        }, out bool error, e.FullPath);
        ParseMode.PrintParseSuccess(file.Path, parsed, error, false);
        file.LastChange = DateTime.Now; // Due to long-lasting operations
    }

    private static void WatchFolder(object sender, FileSystemEventArgs e)
    {
        if (e.FullPath.ToLower().EndsWith(".tmp") || e.FullPath.ToLower().EndsWith(".bak")) return;
        var file = _watchedFiles
            .FirstOrDefault(p => p.Value.Parser is WFolderParser && e.FullPath.Contains(p.Value.Path))
            .Value as WatchedFileFolder;
        // if (!file!.FileList.Contains(e.FullPath)) return;
        if (DateTime.Now.Subtract(file.LastChange).TotalMilliseconds < EventRepeatThreshold)
            return;
        file.LastChange = DateTime.Now; // In case of exceptions
        Thread.Sleep(ProcessDelay);
        output.WriteLine($"[{DateTime.Now:T}] Detected change in {e.FullPath.Replace(OSPath.GetDirectoryName(file.Path) + Path.DirectorySeparatorChar, "")}."
            .PromptPlusEscape());
        bool parsed = errorService.Catch(() => {
            ParseMode.Repack(file.Path, file.Parser, false);
            return true;
        }, out bool error, e.FullPath);
        ParseMode.PrintParseSuccess(file.Path, parsed, error, false);
        file.LastChange = DateTime.Now; // Due to long-lasting operations
    }
}