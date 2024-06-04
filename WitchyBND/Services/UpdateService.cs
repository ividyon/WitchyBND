using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.Enumeration;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PPlus;
using WitchyBND.CliModes;
using WitchyLib;

namespace WitchyBND.Services;

public interface IUpdateService
{
    bool CheckForUpdates(string[] args);
    void PostUpdateActions();
}

public class UpdateService : IUpdateService
{
    private readonly IErrorService errorService;
    private readonly IOutputService output;
    private readonly HttpClient client;

    private const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36";

    private enum UpdateOptions
    {
        [Display(Name = "Update now (will restart WitchyBND)")]
        Update,

        [Display(Name = "Continue (no prompt for 24 hours)")]
        Continue,

        [Display(Name = "View release notes")] ReleaseNotes,

        [Display(Name = "Skip this version")] SkipVersion
    }

    public UpdateService(IErrorService error, IOutputService outputService)
    {
        errorService = error;
        output = outputService;
        client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
    }

    internal const int UpdateInterval = 24;

    internal const string UpdateManifestUrl =
        "https://api.github.com/repos/ividyon/WitchyBND/tags";

    internal const string ReleaseNotesUrl =
        "https://www.github.com/ividyon/WitchyBND/releases";

    public bool CheckForUpdates(string[] args)
    {
        if (Configuration.Active.Offline) return false;
        if ((Configuration.Stored.LastUpdateCheck - DateTime.UtcNow).Duration() <
            TimeSpan.FromHours(UpdateInterval).Duration()) return false;
        try
        {
            var jsonString = client.GetStringAsync(UpdateManifestUrl);
            jsonString.Wait();
            JArray doc = (JArray)JsonConvert.DeserializeObject(jsonString.Result)!;
            var onlineVersion = Version.Parse(String.Concat(doc[0].Value<string>("name")!.Skip(1)));

            var version = Assembly.GetExecutingAssembly().GetName().Version!;

            // if (true)
            if (onlineVersion > Configuration.Stored.SkipUpdateVersion && onlineVersion > version)
            {
                string updateType;
                if (onlineVersion.Major > version.Major)
                    updateType = "This is a major overhaul update. Please update at your earliest convenience.";
                else if (onlineVersion.Minor > version.Minor)
                    updateType =
                        "This is a major improvement update which adds significant new features.";
                else if (onlineVersion.Build > version.Build)
                    updateType =
                        "This is a minor improvement update which adds some new features or improvements.";
                else
                    updateType = "This is a bugfix update. It may resolve pressing issues.";

                errorService.RegisterNotice($@"There is a new version of WitchyBND available: v{onlineVersion}
{updateType}");

                bool loop = true;
                while (loop)
                {
                    var select = output.Select<UpdateOptions>("Select an option")
                        .Config(c => c.EnabledAbortKey(false))
                        .Run();

                    switch (select.Value)
                    {
                        case UpdateOptions.Update:
                            try
                            {
                                UpdateSelf(args);
                            }
                            catch (Exception e)
                            {
                                errorService.CriticalError(e.ToString());
                            }

                            loop = false;
                            break;
                        case UpdateOptions.ReleaseNotes:
                            Process.Start(
                                new ProcessStartInfo { FileName = ReleaseNotesUrl, UseShellExecute = true });
                            break;
                        case UpdateOptions.Continue:
                            // Update last update time
                            Configuration.Stored.LastUpdateCheck = DateTime.UtcNow;
                            Configuration.SaveConfiguration();
                            output.WriteLine("You will not be prompted to update in the next 24 hours.");
                            output.KeyPress("Press any key to continue...").Run();
                            loop = false;
                            break;
                        case UpdateOptions.SkipVersion:
                            // Update last update time
                            var confirm = output.Confirm(
                                "Are you sure? You will not receive any more update prompts for this version, and be unable to use the auto-updater for it.").Run();
                            if (confirm.Value.IsYesResponseKey())
                            {
                                Configuration.Stored.LastUpdateCheck = DateTime.UtcNow;
                                Configuration.Stored.SkipUpdateVersion = onlineVersion;
                                Configuration.SaveConfiguration();
                                output.WriteLine(
                                    $"You will not be prompted to update to WitchyBND v{onlineVersion} anymore.");
                                output.KeyPress("Press any key to continue...").Run();
                                loop = false;
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                return true;
            }
        }
        catch (Exception e)
        {
            errorService.RegisterError($"Failed to check for version update: {e.Message}");
            return false;
        }

        return false;
    }

    public void PostUpdateActions()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version!;

        // First launch ever.
        if (Configuration.Stored.LastLaunchedVersion.Major == 0)
        {
            // v2.7.1.0 introduced this system, so do the v2.7.1.0 upgrade here
            var lastUpdateFile = WBUtil.GetExeLocation("last-update.txt");
            if (File.Exists(lastUpdateFile))
                File.Delete(lastUpdateFile);

            Configuration.Stored.LastLaunchedVersion = version;
            Configuration.SaveConfiguration();
            return;
        }

        // Further upgrades go here

        // 2.9.0.0: Move settings
        if (Configuration.Stored.LastLaunchedVersion < new Version(2, 9, 0, 0))
        {
            var userConfig = WBUtil.GetExeLocation("appsettings.user.json");
            var newConfigPath = Path.Combine(Configuration.AppDataDirectory, "appsettings.user.json");
            if (File.Exists(userConfig))
                File.Move(userConfig, newConfigPath, true);
            Configuration.LoadConfiguration();
        }

        if (Configuration.Stored.LastLaunchedVersion < version)
        {
            var exePath = WBUtil.GetExecutablePath();
            var tempPath = exePath.Replace(".exe", ".exe.tmp");

            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }

        Configuration.Stored.LastLaunchedVersion = version;
        Configuration.SaveConfiguration();
    }

    public void UpdateSelf(string[] args, bool dry = false)
    {
        var exePath = WBUtil.GetExecutablePath();
        var tempPath = exePath.Replace(".exe", ".exe.tmp");
        var hasContextMenu = !dry && Shell.ComplexContextMenuIsRegistered(WBUtil.GetExeLocation());

        (Version, string) getOnlineVersion()
        {
            const string latestManifestUrl =
                "https://api.github.com/repos/ividyon/WitchyBND/releases/latest";

            var jsonString = client.GetStringAsync(latestManifestUrl);
            jsonString.Wait();
            JObject doc = (JObject)JsonConvert.DeserializeObject(jsonString.Result)!;

            var version = Version.Parse(String.Concat(doc.Value<string>("tag_name")!.Skip(1)));
            var url = doc.Value<JArray>("assets")!.First(a => a.Value<string>("browser_download_url")!.EndsWith(".zip"))
                .Value<string>("browser_download_url")!;

            return (version, url);
        }

        Version getLocalVersion()
        {
            var exePath = WBUtil.GetExeLocation("WitchyBND.exe");
            var exe = FileVersionInfo.GetVersionInfo(exePath);
            return Version.Parse(exe.FileVersion!);
        }

        void unregisterShell()
        {
            output.WriteLine("Unregistering WitchyBND context menu (this may take a moment)...");
            if (dry) return;
            Shell.UnregisterComplexContextMenu();
            Shell.RestartExplorer();
        }

        void closeProcesses()
        {
            if (dry) return;

            var currId = Process.GetCurrentProcess().Id;
            var processes = Process.GetProcessesByName("WitchyBND").Where(p => p.Id != currId).ToList();
            if (processes.Any())
            {
                output.WriteLine("Closing open WitchyBND processes...");
                Parallel.ForEach(processes, process => {
                    process.Kill();
                    process.WaitForExit();
                });
            }
        }

        void wipeFolder()
        {
            output.WriteLine("Removing existing WitchyBND files...");

            if (File.Exists(tempPath) && !dry)
                File.Delete(tempPath);

            // Gross: Load some assemblies that are about to be required, before renaming the executable, else they can't be found.
            Assembly.Load("System.IO.Compression");
            Assembly.Load("System.Text.Encodings.Web");

            if (!dry)
                File.Move(exePath, tempPath);

            string[] wipeList =
            {
                "appsettings.json",
                "WitchyBND.Shell.dll",
                "SharpShell.dll",
                "README.md",
                "Assets",
            };

            var di = new DirectoryInfo(WBUtil.GetExeLocation());

            foreach (FileInfo file in di.GetFiles())
            {
                if (!wipeList.Any(f => FileSystemName.MatchesSimpleExpression(f, file.Name))) continue;
                if (!dry)
                    file.Delete();
            }

            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                if (!wipeList.Any(f => FileSystemName.MatchesSimpleExpression(f, dir.Name))) continue;
                if (!dry)
                    dir.Delete(true);
            }
        }

        void registerShell()
        {
            output.WriteLine("Registering WitchyBND context menu...");
            if (dry) return;
            Shell.RegisterComplexContextMenu();
        }


        async Task downloadLatest(Version onlineVersion, string downloadUrl)
        {
            output.WriteLine($"Downloading WitchyBND v{onlineVersion}...");
            using HttpResponseMessage response = await client.GetAsync(downloadUrl);
            await using Stream? stream = await response.Content.ReadAsStreamAsync();
            output.WriteLine("Unpacking release archive...");
            using var zip = new ZipArchive(stream);
            foreach (ZipArchiveEntry e in zip.Entries)
            {
                // Check if directory
                var lowerByte = (byte)(e.ExternalAttributes & 0x00FF);
                var attributes = (FileAttributes)lowerByte;
                if ((attributes & FileAttributes.Directory) != 0)
                {
                    continue;
                }

                await using Stream? data = e.Open();
                var filePath = Path.Combine(WBUtil.GetExeLocation(),
                    e.FullName.Replace('/', Path.DirectorySeparatorChar));
                var fileDir = Path.GetDirectoryName(filePath)!;

                if (File.Exists(filePath))
                    throw new Exception($"File {filePath} already exists and was not removed properly.");

                if (!Directory.Exists(fileDir))
                    Directory.CreateDirectory(fileDir);

                output.WriteLine($"Extracting file: {filePath}");
                Stream resultStream = dry ? new MemoryStream() : new FileStream(filePath, FileMode.Create);
                await data.CopyToAsync(resultStream);
                resultStream.Close();
            }
        }

        (Version onlineVersion, string downloadUrl) = getOnlineVersion();
        // Version version = getLocalVersion();

        // output.WriteLine($"Current version is: v{version}");
        // output.WriteLine($"Online version is: v{onlineVersion}");
        //
        // if (version >= onlineVersion)
        // {
        //     output.WriteLine("You are on the latest version of WitchyBND.");
        //     return;
        // }

        output.WriteLine(
            $"WitchyBND will now download the latest version: v{onlineVersion}.");
        if (hasContextMenu)
            output.WriteLine(
                @"Explorer needs to be restarted to complete the process. Your taskbar will briefly disappear for a few seconds.
Witchy will try to restore any open Explorer windows.");

        if (!Configuration.Active.Passive)
            output.KeyPress("Press any key to continue...").Run();

        if (hasContextMenu)
            unregisterShell();

        closeProcesses();

        try
        {
            wipeFolder();

            downloadLatest(onlineVersion, downloadUrl).Wait();

            if (hasContextMenu)
                registerShell();
        }
        finally
        {
            if (!File.Exists(exePath) && File.Exists(tempPath) && !dry)
                File.Move(tempPath, exePath);
        }

        output.WriteLine($"Successfully updated WitchyBND to v{onlineVersion}.\nThe application will now restart.");

        if (!hasContextMenu)
        {
            var contextMenuQuery = output.Confirm(
                @"Would you like to enable Windows context menu integration?
You'll be able to perform common WitchyBND operations by right-clicking on files and folders.")
                .Config(c => c.EnabledAbortKey(false))
                .Run();
            if (contextMenuQuery.Value.IsYesResponseKey())
            {
                IntegrationMode.RegisterContext();
                output.WriteLine("Windows integration enabled.");
            }
            else
            {
                output.WriteLine("You can find the Windows integration setup in the WitchyBND settings if you wish to enable it later.");
            }
        }

        if (!Configuration.Active.Passive)
            output.KeyPress("Press any key to continue...").Run();

        if (dry) return;

        var processInfo = new ProcessStartInfo(exePath, args);
        processInfo.UseShellExecute = true;
        Process.Start(processInfo);
        Environment.Exit(0);
    }
}