using System.Globalization;
using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Pose;
using PPlus;
using WitchyBND;

namespace WitchyTests;

[Ignore("Base class")]
public class TestBase
{
    [OneTimeSetUp]
    public void StartUp()
    {
        Shim shim = Shim.Replace(() => Configuration.GetRoot()).With(() => {
            Configuration.WitchyConfigValues values = new Configuration.WitchyConfigValues()
            {
                Bnd = false,
                Dcx = false,
                Recursive = false,
                ParamDefaultValues = true,
            };
            var json = JsonConvert.SerializeObject(values);
            return new ConfigurationBuilder()
                .AddJsonStream(new MemoryStream(Encoding.ASCII.GetBytes(json)))
                .Build();
        });
        Configuration.Args.Passive = true;
        Environment.SetEnvironmentVariable("PromptPlusOverUnitTest", "true");
        PromptPlus.Setup();
        PromptPlus.Reset();
        PromptPlus.Clear();
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        PromptPlus.Config.DefaultCulture = new CultureInfo("en-us");
    }

    [SetUp]
    public void Init()
    {
        Configuration.Args.Location = null;
    }

    [TearDown]
    public void Cleanup()
    {
        if (Directory.Exists("./Results"))
            Directory.Delete("./Results", true);
        Thread.Sleep(1000);
    }

    protected static IEnumerable<string> GetSamples(string sampleDir)
    {
        return Directory.GetFiles($"./Samples/{sampleDir}", "*", SearchOption.AllDirectories);
    }

    protected static string GetCopiedPath(string path)
    {
        var newPath = path.Replace(@"/Samples/", @"/Results/");
        Directory.CreateDirectory(Path.GetDirectoryName(newPath)!);
        File.Copy(path, newPath);
        return newPath;
    }
}