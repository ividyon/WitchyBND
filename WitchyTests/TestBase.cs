using System.Globalization;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PromptPlusLibrary;
using WitchyBND;
using WitchyBND.Services;
using WitchyLib;
using WitchyTests.Services;
using ServiceProvider = WitchyBND.Services.ServiceProvider;

namespace WitchyTests;

[Ignore("Base class")]
public class TestBase
{
    protected static IGameService _gameService;
    static IServiceProvider CreateProvider()
    {
        var output = new TestOutputService();
        var error = new ErrorService(output);
        var game = new GameService(error, output);
        var update = new UpdateService(error, output);

        var collection = new ServiceCollection()
            .AddSingleton<IOutputService>(output)
            .AddSingleton<IErrorService>(error)
            .AddSingleton<IGameService>(game)
            .AddSingleton<IUpdateService>(update);

        return collection.BuildServiceProvider();
    }

    public bool Location;
    public bool Parallel;
    public TestBase(bool location, bool parallel)
    {
        Location = location;
        Parallel = parallel;
    }

    static TestBase()
    {
        ServiceProvider.ChangeProvider(CreateProvider());
        _gameService = ServiceProvider.GetService<IGameService>();
    }

    [OneTimeSetUp]
    public void StartUp()
    {
        Configuration.SwapOutConfig(new ConfigurationBuilder()
            .AddJsonFile(OSPath.Combine(TestContext.CurrentContext.TestDirectory, "appsettings.json"))
            .Build());
        Configuration.Active.Passive = true;
        Configuration.IsTest = true;
        Configuration.Active.Parallel = Parallel;
        WBUtil.ExeLocation = TestContext.CurrentContext.TestDirectory;
        Environment.SetEnvironmentVariable("PromptPlusOverUnitTest", "true");
        // PromptPlus.Reset();
        // PromptPlus.Clear();
        Console.OutputEncoding = Encoding.UTF8;
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
    }

    [SetUp]
    public void Init()
    {
        Configuration.Active.Location = null;
        if (Directory.Exists(OSPath.Combine(TestContext.CurrentContext.TestDirectory, "Results")))
            Directory.Delete(OSPath.Combine(TestContext.CurrentContext.TestDirectory, "Results"), true);
    }

    protected void SetLocation(string path)
    {
        if (Location)
        {
            Configuration.Active.Location = OSPath.Combine(Path.GetDirectoryName(path)!, "Target");
            Directory.CreateDirectory(Configuration.Active.Location);
            return;
        }

        Configuration.Active.Location = null;
    }


    protected static IEnumerable<string> GetSamples(string sampleDir, string pattern = "*")
    {
        return Directory.GetFiles(OSPath.Combine(TestContext.CurrentContext.TestDirectory, "Samples", sampleDir), pattern,
            SearchOption.AllDirectories);
    }
    protected static IEnumerable<string> GetAllSamples(string pattern = "*")
    {
        return Directory.GetFiles(OSPath.Combine(TestContext.CurrentContext.TestDirectory, "Samples"), pattern,
            SearchOption.AllDirectories);
    }

    protected static string GetCopiedPath(string path)
    {
        var newPath = path.Replace("Samples", "Results");
        Directory.CreateDirectory(OSPath.GetDirectoryName(newPath)!);
        File.Copy(path, newPath, true);
        return newPath;
    }

    protected static string GetTestPath()
    {
        return TestContext.CurrentContext.TestDirectory;
    }
}