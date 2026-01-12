using System.Runtime.InteropServices;
using Microsoft.Win32;
using NativeFileDialogSharp;

namespace SoulsOodleLib
{
    /// <summary>
    /// A class existing by the grace of Nordgaren and his Yabber+ code. Thanks Nordgaren!
    /// </summary>
    public static class AssetLocator
    {
        public enum Game
        {
            EldenRing,
            ArmoredCore6,
            Sekiro,
            Nightreign
        }

        private static readonly Dictionary<Game, string> steamSearchPaths = new()
        {
            { Game.EldenRing, @$"steamapps{Path.DirectorySeparatorChar}common{Path.DirectorySeparatorChar}ELDEN RING{Path.DirectorySeparatorChar}Game" },
            { Game.ArmoredCore6, @$"steamapps{Path.DirectorySeparatorChar}common{Path.DirectorySeparatorChar}ARMORED CORE VI FIRES OF RUBICON{Path.DirectorySeparatorChar}Game" },
            { Game.Sekiro, @$"steamapps{Path.DirectorySeparatorChar}common{Path.DirectorySeparatorChar}Sekiro" },
            { Game.Nightreign, @$"steamapps{Path.DirectorySeparatorChar}common{Path.DirectorySeparatorChar}ELDEN RING NIGHTREIGN{Path.DirectorySeparatorChar}Game" }
        };

        private static readonly Dictionary<Game, string> installPaths = new();

        private static readonly (string, string)[] winRegistryPathValueTuple =
        {
            (@"HKEY_CURRENT_USER\SOFTWARE\Wow6432Node\Valve\Steam", "InstallPath"),
            (@"HKEY_CURRENT_USER\SOFTWARE\Valve\Steam", "InstallPath"),
            (@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Valve\Steam", "InstallPath"),
            (@"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "InstallPath"),
        };

            public static string? GetSteamPath(params string[] path)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    foreach ((string Path, string Value) pathValueTuple in winRegistryPathValueTuple)
                    {
                        string registryKey = pathValueTuple.Path;
                        var regPath = (string?)Registry.GetValue(registryKey, pathValueTuple.Value, null);
                        if (regPath != null)
                        {
                            var fullPath = Path.Combine(((string[]) [regPath]).Union(path).ToArray());
                            if (Directory.Exists(fullPath) || File.Exists(fullPath))
                            {
                                return fullPath;
                            }
                        }
                    }
                }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var snapDir = Environment.GetEnvironmentVariable("SNAP_USER_DATA");
                if (snapDir == null)
                    snapDir = Path.Combine(homeDir, "snap");
                HashSet<string> possiblePaths =
                [
                    // snap
                    Path.Combine(homeDir, ".var/app/com.valvesoftware.Steam/.local/share/Steam"),
                    Path.Combine(homeDir, ".var/app/com.valvesoftware.Steam/.steam/steam"),
                    Path.Combine(homeDir, ".var/app/com.valvesoftware.Steam/.steam/root"),
                    // regular
                    Path.Combine(homeDir, ".local/share/Steam"),
                    Path.Combine(homeDir, ".steam/steam"),
                    Path.Combine(homeDir, ".steam/root"),
                    Path.Combine(homeDir, ".steam/debian-installation"),
                    // snap
                    Path.Combine(snapDir, "steam/common/.local/share/Steam"),
                    Path.Combine(snapDir, "steam/common/.steam/steam"),
                    Path.Combine(snapDir, "steam/common/.steam/root")
                ];
                foreach (string possiblePath in possiblePaths)
                {
                    string myPath = Path.Combine([possiblePath, .. path]);
                    if (Directory.Exists(myPath) || File.Exists(myPath))
                    {
                        return myPath;
                    }
                }
            }

            return null;
        }

        public static List<string> GetSteamLibraryPaths()
        {

            string? librariesPath = GetSteamPath("steamapps", "libraryfolders.vdf");
            if (librariesPath == null || !File.Exists(librariesPath))
            {
                return new();
            }

            string[] libraryFolders = File.ReadAllLines(librariesPath);

            var pathStrings = libraryFolders.Where(str => str.Contains("\"path\""));
            var paths = pathStrings.Select(str =>
            {
                var split = str.Split('"').Where((s, i) => i % 2 == 1).ToList();
                if (split.Count == 2)
                    return split[1].Replace('\\', Path.DirectorySeparatorChar);

                return null;
            }).Where(p => p != null && Directory.Exists(p)).Cast<string>().ToList();

            return paths;
        }

        public static string? TryGetGameInstallLocation(string gamePath)
        {
            var libraryPaths = GetSteamLibraryPaths();
            foreach (string path in libraryPaths)
            {
                string libraryPath = Path.Combine(path, gamePath);
                if (Directory.Exists(libraryPath))
                    return libraryPath;
            }

            return null;
        }

        public static string? GetGamePath(IEnumerable<Game> inputGames, Action<string> writeLineFunction, bool useFolderPicker)
        {
            IEnumerable<Game> games = inputGames as Game[] ?? inputGames.ToArray();
            if (installPaths.Keys.Intersect(games).Any())
                return installPaths[installPaths.Keys.Intersect(games).First()];

            string? path = null;

            foreach (Game game in games)
            {
                path = TryGetGameInstallLocation(steamSearchPaths[game]);
                if (path != null)
                {
                    installPaths[game] = path;
                    break;
                }
            }

            if (path == null && useFolderPicker)
            {
                writeLineFunction(
                    @"Could not locate your game folder. Please select it manually to proceed.");
                Console.ReadKey();
                var result = Dialog.FolderPicker();
                if (result.IsOk)
                {
                    installPaths[games.First()] = result.Path;
                    path = result.Path;
                }
            }

            return path;
        }
    }
}