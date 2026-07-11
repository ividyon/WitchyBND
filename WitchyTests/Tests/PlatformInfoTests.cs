using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using WitchyBND;
using WitchyBND.Services;
using ServiceProvider = WitchyBND.Services.ServiceProvider;

namespace WitchyTests;

[TestFixture]
public class PlatformInfoTests
{
    [TestCase("windows")]
    [TestCase("linux")]
    [TestCase("macos")]
    public void DetectReturnsExpectedPlatform(string expected)
    {
        OSPlatform platform = PlatformInfo.Detect(candidate => expected switch
        {
            "windows" => candidate == OSPlatform.Windows,
            "linux" => candidate == OSPlatform.Linux,
            "macos" => candidate == OSPlatform.OSX,
            _ => false
        });

        OSPlatform expectedPlatform = expected switch
        {
            "windows" => OSPlatform.Windows,
            "linux" => OSPlatform.Linux,
            _ => OSPlatform.OSX
        };
        Assert.That(platform, Is.EqualTo(expectedPlatform));
    }

    [Test]
    public void DetectRejectsUnsupportedPlatform()
    {
        Assert.Throws<PlatformNotSupportedException>(() => PlatformInfo.Detect(_ => false));
    }

    [Test]
    public void ResolveAppDataUsesMacApplicationSupport()
    {
        string path = PlatformInfo.ResolveAppDataDirectory(OSPlatform.OSX, "", "/Users/tester");

        Assert.That(path, Is.EqualTo(Path.GetFullPath("/Users/tester/Library/Application Support/WitchyBND")));
    }

    [Test]
    public void ResolveAppDataUsesLinuxFallbackWhenApplicationDataIsEmpty()
    {
        string path = PlatformInfo.ResolveAppDataDirectory(OSPlatform.Linux, "", "/home/tester");

        Assert.That(path, Is.EqualTo(Path.GetFullPath("/home/tester/.config/WitchyBND")));
    }

    [Test]
    public void ResolveAppDataNeverReturnsRelativeExecutablePath()
    {
        string path = PlatformInfo.ResolveAppDataDirectory(OSPlatform.Windows, "", "/home/tester");

        Assert.That(Path.IsPathFullyQualified(path), Is.True);
        Assert.That(path, Does.EndWith(Path.Combine("AppData", "Roaming", "WitchyBND")));
    }

    [TestCase("--silent")]
    [TestCase("-s")]
    public void SilentBootstrapRecognizesSilentFlags(string flag)
    {
        Assert.That(CliBootstrap.IsSilentRequested([flag]), Is.True);
    }

    [Test]
    public void SilentBootstrapDoesNotMatchUnrelatedArguments()
    {
        Assert.That(CliBootstrap.IsSilentRequested(["--passive", "/tmp/file"]), Is.False);
    }

    [TestCase("--help")]
    [TestCase("-h")]
    [TestCase("--version")]
    [TestCase("-v")]
    public void PlainOutputBootstrapRecognizesInformationalCommands(string flag)
    {
        Assert.That(CliBootstrap.IsPlainOutputRequested([flag]), Is.True);
    }

    [Test]
    public void ServiceProviderUsesPlainOutputWithoutPromptPlus()
    {
        bool previousSilent = Configuration.Active.Silent;
        try
        {
            Configuration.Active.Silent = false;
            IServiceProvider provider = ServiceProvider.CreateProvider(true);

            Assert.That(provider.GetRequiredService<IOutputService>(), Is.TypeOf<PlainOutputService>());
        }
        finally
        {
            Configuration.Active.Silent = previousSilent;
        }
    }
}
