using System.Security.Cryptography;

namespace WitchyTests.Infrastructure;

public sealed record ManifestEntry(string RelativePath, long Length, string Sha256);

public static class DirectoryManifest
{
    public static IReadOnlyList<ManifestEntry> Create(string root)
    {
        string fullRoot = Path.GetFullPath(root);
        return Directory.EnumerateFiles(fullRoot, "*", SearchOption.AllDirectories)
            .Select(path => new ManifestEntry(
                Path.GetRelativePath(fullRoot, path).Replace(Path.DirectorySeparatorChar, '/'),
                new FileInfo(path).Length,
                Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant()))
            .OrderBy(entry => entry.RelativePath, StringComparer.Ordinal)
            .ToArray();
    }
}
