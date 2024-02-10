using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PPlus;
using WitchyBND.Services;

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

    public bool CheckForUpdates()
    {
        if (Configuration.Offline) return false;
        DateTime? updateTime = Configuration.LastUpdateCheckTime;
        if (updateTime != null && updateTime - DateTime.UtcNow > TimeSpan.FromHours(UpdateInterval)) return false;
        try
        {
            // Update last update time
            Configuration.LastUpdateCheckTime = DateTime.UtcNow;
            Configuration.UpdateConfiguration();

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