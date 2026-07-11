using WitchyBND.Services;
using WitchyBND.Parsers;
using WitchyTests.Infrastructure;

namespace WitchyTests;

[TestFixture]
public class InfrastructureTests
{
    [Test]
    public void FixtureLocatorReturnsNullWhenNotConfigured()
    {
        Assert.That(FixtureLocator.TryGetRoot(_ => null), Is.Null);
    }

    [Test]
    public void FixtureLocatorNormalizesConfiguredRoot()
    {
        Assert.That(Path.IsPathFullyQualified(FixtureLocator.TryGetRoot(_ => ".")!), Is.True);
    }

    [Test]
    public void DirectoryManifestIsStableAndContentSensitive()
    {
        string root = CreateTemporaryDirectory();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "nested"));
            File.WriteAllText(Path.Combine(root, "b.txt"), "second");
            File.WriteAllText(Path.Combine(root, "nested", "a.txt"), "first");

            IReadOnlyList<ManifestEntry> first = DirectoryManifest.Create(root);
            IReadOnlyList<ManifestEntry> second = DirectoryManifest.Create(root);

            Assert.That(second, Is.EqualTo(first));
            Assert.That(first.Select(entry => entry.RelativePath),
                Is.EqualTo(new[] { "b.txt", "nested/a.txt" }));
            Assert.That(first.All(entry => entry.Sha256.Length == 64), Is.True);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public async Task ProcessRunnerCapturesOutputAndExitCode()
    {
        var runner = new ProcessRunner();
        ProcessResult result = await runner.RunAsync(ShellRequest("printf output; printf error >&2; exit 7"));

        Assert.That(result.ExitCode, Is.EqualTo(7));
        Assert.That(result.StandardOutput, Does.Contain("output"));
        Assert.That(result.StandardError, Does.Contain("error"));
    }

    [Test]
    public void ProcessRunnerTimesOutAndTerminatesProcess()
    {
        var runner = new ProcessRunner();
        ProcessRequest request = ShellRequest("sleep 5") with { Timeout = TimeSpan.FromMilliseconds(100) };

        Assert.ThrowsAsync<TimeoutException>(async () => await runner.RunAsync(request));
    }

    [Test]
    public void ProcessRunnerHonorsCancellation()
    {
        var runner = new ProcessRunner();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        Assert.CatchAsync<OperationCanceledException>(async () =>
            await runner.RunAsync(ShellRequest("sleep 5"), cancellation.Token));
    }

    [Test]
    public void SafeWritePreservesDestinationWhenWriterFails()
    {
        string root = CreateTemporaryDirectory();
        string destination = Path.Combine(root, "archive.dcx");
        File.WriteAllText(destination, "original");
        try
        {
            Assert.Throws<InvalidOperationException>(() => WFileParser.WriteSafely(destination, temporary =>
            {
                File.WriteAllText(temporary, "partial");
                throw new InvalidOperationException("failed");
            }));

            Assert.That(File.ReadAllText(destination), Is.EqualTo("original"));
            Assert.That(Directory.GetFiles(root, ".witchy-*"), Is.Empty);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void SafeWriteAtomicallyReplacesDestinationAfterSuccess()
    {
        string root = CreateTemporaryDirectory();
        string destination = Path.Combine(root, "archive.dcx");
        File.WriteAllText(destination, "original");
        try
        {
            WFileParser.WriteSafely(destination, temporary => File.WriteAllText(temporary, "replacement"));

            Assert.That(File.ReadAllText(destination), Is.EqualTo("replacement"));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static ProcessRequest ShellRequest(string command)
    {
        return OperatingSystem.IsWindows()
            ? new ProcessRequest("cmd.exe", ["/d", "/s", "/c", command])
            : new ProcessRequest("/bin/sh", ["-c", command]);
    }

    private static string CreateTemporaryDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"witchy-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
