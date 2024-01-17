using System;
using System.IO;
using System.Linq;
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
            Sekiro
        }

        private static Dictionary<Game, string> SearchPaths = new()
        {
            { Game.EldenRing, @"\steamapps\common\ELDEN RING\Game" },
            { Game.ArmoredCore6, @"\steamapps\common\ARMORED CORE VI FIRES OF RUBICON\Game" },
            { Game.Sekiro, @"\steamapps\common\Sekiro" }
        };

        private static Dictionary<Game, string> InstallPaths = new();
        static (string, string)[] _pathValueTuple =
        {
            (@"HKEY_CURRENT_USER\SOFTWARE\Valve\Steam", "SteamPath"),
            (@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Valve\Steam", "InstallPath"),
            (@"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "InstallPath"),
            (@"HKEY_CURRENT_USER\SOFTWARE\Wow6432Node\Valve\Steam", "SteamPath"),
        };

        public static string GetSteamInstallPath()
        {
            string installPath;

            foreach ((string Path, string Value) pathValueTuple in _pathValueTuple)
            {
                string registryKey = pathValueTuple.Path;
                installPath = (string)Registry.GetValue(registryKey, pathValueTuple.Value, null);

                if (installPath != null)
                    return installPath;
            }

            return null;
        }

        public static string? TryGetGameInstallLocation(string gamePath)
        {
            if (!gamePath.StartsWith("\\") && !gamePath.StartsWith("/"))
                return null;

            string steamPath = GetSteamInstallPath();

            if (string.IsNullOrWhiteSpace(steamPath) || !File.Exists($@"{steamPath}\SteamApps\libraryfolders.vdf"))
                return null;

            string[] libraryFolders = File.ReadAllLines($@"{steamPath}\SteamApps\libraryfolders.vdf");

            var pathStrings = libraryFolders.Where(str => str.Contains("\"path\""));
            var paths = pathStrings.Select(str =>
            {
                var split = str.Split('"').Where((s, i) => i % 2 == 1).ToList();
                if (split.Count == 2)
                    return split[1];

                return null;
            }).ToList();

            foreach (string path in paths)
            {
                string libraryPath = path.Replace(@"\\", @"\") + gamePath;
                if (Directory.Exists(libraryPath))
                    return libraryPath;
            }

            return null;
        }

        public static string? GetGamePath(IEnumerable<Game> inputGames, Action<string> writeLineFunction, bool useFolderPicker)
        {
            IEnumerable<Game> games = inputGames as Game[] ?? inputGames.ToArray();
            if (InstallPaths.Keys.Intersect(games).Any())
                return InstallPaths[InstallPaths.Keys.Intersect(games).First()];

            string? path = null;

            foreach (Game game in games)
            {
                path = TryGetGameInstallLocation(SearchPaths[game]);
                if (path != null)
                {
                    InstallPaths[game] = path;
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
                    InstallPaths[games.First()] = result.Path;
                    path = result.Path;
                }
            }

            return path;
        }
    }
}