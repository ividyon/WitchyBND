using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PPlus;
using SoulsFormats.KF4;
using WitchyBND.Services;
using WitchyLib;

namespace WitchyBND;

public interface IStartupService
{
    bool CheckForUpdates();
    void UpgradeActions();
}

public class StartupService : IStartupService
{
    private readonly IErrorService errorService;
    private readonly IOutputService output;

    private enum UpdateOptions
    {
        [Display(Name = "Continue")] Continue,

        [Display(Name = "View release page (closes WitchyBND)")]
        UpdateNotes,

        [Display(Name = "Skip this version")] SkipVersion
    }

    public StartupService(IErrorService error, IOutputService outputService)
    {
        errorService = error;
        output = outputService;
    }

    internal const int UpdateInterval = 24;

    internal const string UpdateManifestUrl =
        "https://api.github.com/repos/ividyon/WitchyBND/tags";

    internal const string UpdateNotesUrl =
        "https://www.github.com/ividyon/WitchyBND/releases";

    internal const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36";

    public bool CheckForUpdates()
    {
        if (Configuration.Offline) return false;
        if ((Configuration.LastUpdateCheck - DateTime.UtcNow).Duration() <
            TimeSpan.FromHours(UpdateInterval).Duration()) return false;
        try
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            var jsonString = client.GetStringAsync(UpdateManifestUrl);
            jsonString.Wait();
            JArray doc = (JArray)JsonConvert.DeserializeObject(jsonString.Result)!;
            var onlineVersion = Version.Parse(String.Concat(doc[0].Value<string>("name").Skip(1)));

            var version = Assembly.GetExecutingAssembly().GetName().Version!;

            if (onlineVersion > Configuration.SkipUpdateVersion && onlineVersion > version)
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

                if (!Configuration.Args.Passive)
                {
                    var select = output.Select<UpdateOptions>("Select an option")
                        .Config(c => c.EnabledAbortKey(false))
                        .Run();

                    switch (select.Value)
                    {
                        case UpdateOptions.Continue:
                            // Update last update time
                            Configuration.LastUpdateCheck = DateTime.UtcNow;
                            Configuration.UpdateConfiguration();
                            output.WriteLine("You will not be prompted to update in the next 24 hours.");
                            output.KeyPress("Press any key to continue...").Run();
                            break;
                        case UpdateOptions.UpdateNotes:
                            Process.Start(new ProcessStartInfo { FileName = UpdateNotesUrl, UseShellExecute = true });
                            Environment.Exit(0);
                            break;
                        case UpdateOptions.SkipVersion:
                            // Update last update time
                            Configuration.LastUpdateCheck = DateTime.UtcNow;
                            Configuration.SkipUpdateVersion = onlineVersion;
                            Configuration.UpdateConfiguration();
                            output.WriteLine(
                                $"You will not be prompted to update to WitchyBND v{onlineVersion} anymore.");
                            output.KeyPress("Press any key to continue...").Run();
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

    public void UpgradeActions()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version!;

        // First launch ever.
        if (Configuration.LastLaunchedVersion.Major == 0)
        {
            // v2.7.1.0 introduced this system, so do the v2.7.1.0 upgrade here
            var lastUpdateFile = WBUtil.GetExeLocation("last-update.txt");
            if (File.Exists(lastUpdateFile))
                File.Delete(lastUpdateFile);

            Configuration.LastLaunchedVersion = version;
            Configuration.UpdateConfiguration();
            return;
        }

        // Further upgrades go here

        Configuration.LastLaunchedVersion = version;
        Configuration.UpdateConfiguration();
    }
}