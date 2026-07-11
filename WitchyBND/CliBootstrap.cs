using System.Collections.Generic;
using System.IO;
using System.Linq;
using WitchyBND.Services;
using WitchyLib;

namespace WitchyBND;

public static class CliBootstrap
{
    public static bool IsSilentRequested(IEnumerable<string> args)
    {
        return args.Any(arg => arg is "--silent" or "-s");
    }

    public static bool IsPlainOutputRequested(IEnumerable<string> args)
    {
        return args.Any(arg => arg is
            "--help" or "-h" or "--version" or "-v" or "--doctor" or "--passive" or "-p");
    }

    public static void ApplyGameOverride(CliOptions options, IGameService gameService)
    {
        if (options.Game == null) return;

        foreach (string path in options.Paths)
        {
            string fullPath = OSPath.GetFullPath(path);
            string root = File.Exists(fullPath) ? OSPath.GetDirectoryName(fullPath)! : fullPath;
            gameService.KnownGamePaths[root] = options.Game.Value;
        }
    }
}
