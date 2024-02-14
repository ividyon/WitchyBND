using System;
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

public interface IUpdateService
{
    bool CheckForUpdates();
}

public class UpdateService : IUpdateService
{
    private readonly IErrorService errorService;
    private readonly IOutputService output;

    public UpdateService(IErrorService error, IOutputService outputService)
    {
        errorService = error;
        output = outputService;
    }

    internal const int UpdateInterval = 6;
    internal const string UpdateManifestUrl =
        "https://api.github.com/repos/ividyon/WitchyBND/tags";

    internal const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36";

    public DateTime ReadUpdateFile()
    {
        DateTime? time = null;
        var path = WBUtil.GetExeLocation("last-update.txt");
        if (File.Exists(path))
        {
            var parsed = DateTime.TryParse(File.ReadAllText(path), out var parsedTime);
            if (parsed)
            {
                time = parsedTime;
            }
        }
        return time ?? new DateTime(0);
    }

    public bool WriteUpdateFile(DateTime time)
    {
        try
        {
            File.WriteAllText(WBUtil.GetExeLocation("last-update.txt"), time.ToString(CultureInfo.InvariantCulture));
        }
        catch (Exception)
        {
            output.WriteError("Could not write current time to file.");
            return false;
        }

        return true;
    }
    public bool CheckForUpdates()
    {
        if (Configuration.Offline) return false;
        DateTime updateTime = ReadUpdateFile();
        if (updateTime - DateTime.Now > TimeSpan.FromHours(UpdateInterval)) return false;
        try
        {
            // Update last update time
            WriteUpdateFile(DateTime.Now);

            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            var jsonString = client.GetStringAsync(UpdateManifestUrl);
            jsonString.Wait();
            JArray doc = (JArray)JsonConvert.DeserializeObject(jsonString.Result)!;
            var onlineVersion = Version.Parse(String.Concat(doc[0].Value<string>("name").Skip(1)));

            var version = Assembly.GetExecutingAssembly().GetName().Version;

            if (onlineVersion > version)
            {
                errorService.RegisterNotice($@"There is a new version of WitchyBND available: {onlineVersion}
Please update at your earliest convenience, as the new version may contain important fixes and new features.");

                if (!Configuration.Args.Passive)
                    output.KeyPress("Press any key to continue...").Run();
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
}