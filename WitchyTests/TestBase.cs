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
        Configuration.ReplaceConfig(new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(TestContext.CurrentContext.TestDirectory, "appsettings.json"))
            .Build());
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
    }

    protected static IEnumerable<string> GetSamples(string sampleDir, string pattern = "*")
    {
        return Directory.GetFiles(Path.Combine(TestContext.CurrentContext.TestDirectory, "Samples", sampleDir), pattern,
            SearchOption.AllDirectories);
    }

    protected static string GetCopiedPath(string path)
    {
        var newPath = path.Replace("Samples", "Results");
        Directory.CreateDirectory(Path.GetDirectoryName(newPath)!);
        File.Copy(path, newPath);
        return newPath;
    }
}