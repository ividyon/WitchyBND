namespace WitchyTests.Infrastructure;

public static class FixtureLocator
{
    public const string EnvironmentVariable = "WITCHY_FIXTURES_ROOT";

    public static string? TryGetRoot(Func<string, string?>? getEnvironmentVariable = null)
    {
        getEnvironmentVariable ??= Environment.GetEnvironmentVariable;
        string? configured = getEnvironmentVariable(EnvironmentVariable);
        return string.IsNullOrWhiteSpace(configured) ? null : Path.GetFullPath(configured);
    }

    public static string RequireFile(string relativePath)
    {
        string? root = TryGetRoot();
        if (root == null)
            Assert.Ignore($"Set {EnvironmentVariable} to run local game fixture tests.");

        return RequireFile(root!, relativePath);
    }

    public static string RequireFile(string root, string relativePath)
    {
        root = Path.GetFullPath(root);
        string path = Path.GetFullPath(Path.Combine(root, relativePath));
        string rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        Assert.That(path.StartsWith(rootPrefix, StringComparison.Ordinal), Is.True,
            "Fixture paths must remain under the configured fixture root.");
        Assert.That(File.Exists(path), Is.True, $"Fixture does not exist: {relativePath}");
        return path;
    }
}
