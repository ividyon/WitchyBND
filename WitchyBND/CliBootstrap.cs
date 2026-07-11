using System.Collections.Generic;
using System.Linq;

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
}
