using System.Globalization;
using PPlus;
using WitchyBND;

namespace WitchyTests;

[Ignore("Base class")]
public class TestBase
{
    [OneTimeSetUp]
    public void StartUp()
    {
        Configuration.Args.Passive = true;
        Environment.SetEnvironmentVariable("PromptPlusOverUnitTest", "true");
        PromptPlus.Setup();
        PromptPlus.Reset();
        PromptPlus.Clear();
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        PromptPlus.Config.DefaultCulture = new CultureInfo("en-us");
    }

    [TearDown]
    public void Cleanup()
    {
        if (Directory.Exists("./Results"))
            Directory.Delete("./Results", true);
        Thread.Sleep(1000);
    }
}