using System;
using System.IO;
using System.Runtime.InteropServices;

namespace WitchyBND;

public static class PlatformInfo
{
    public static OSPlatform Detect(Func<OSPlatform, bool>? isPlatform = null)
    {
        isPlatform ??= RuntimeInformation.IsOSPlatform;

        if (isPlatform(OSPlatform.Windows))
            return OSPlatform.Windows;
        if (isPlatform(OSPlatform.OSX))
            return OSPlatform.OSX;
        if (isPlatform(OSPlatform.Linux))
            return OSPlatform.Linux;

        throw new PlatformNotSupportedException("WitchyBND supports Windows, Linux, and macOS.");
    }

    public static string ResolveAppDataDirectory(
        OSPlatform platform,
        string? applicationData = null,
        string? userProfile = null)
    {
        applicationData ??= Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        userProfile ??= Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        string root;
        if (platform == OSPlatform.OSX)
        {
            root = Path.Combine(RequireUserProfile(userProfile), "Library", "Application Support");
        }
        else if (!string.IsNullOrWhiteSpace(applicationData))
        {
            root = applicationData;
        }
        else if (platform == OSPlatform.Linux)
        {
            root = Path.Combine(RequireUserProfile(userProfile), ".config");
        }
        else
        {
            root = Path.Combine(RequireUserProfile(userProfile), "AppData", "Roaming");
        }

        return Path.GetFullPath(Path.Combine(root, "WitchyBND"));
    }

    private static string RequireUserProfile(string? userProfile)
    {
        if (string.IsNullOrWhiteSpace(userProfile))
            throw new InvalidOperationException("Could not determine the current user's home directory.");
        return userProfile;
    }
}
