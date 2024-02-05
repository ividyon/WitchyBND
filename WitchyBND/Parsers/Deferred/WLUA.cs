using System.IO;
using System.Xml.Linq;
using PPlus;
using SoulsFormats;
using WitchyBND.CliModes;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WLUA : WSingleFileParser
{

    public override string Name => "LUA";
    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        file = null;
        var extension = Path.GetExtension(path).ToLower();
        var cond = extension is ".lua" or ".hks";
        if (!cond)
            throw new DeferToolPathException(DeferFormat.Lua);
        return true;
    }
    public override bool ExistsUnpacked(string path)
    {
        return false;
    }

    public override bool IsUnpacked(string path)
    {
        return false;
    }

    public override string GetUnpackDestPath(string srcPath)
    {
        throw new System.NotImplementedException();
    }

    public override void Unpack(string srcPath, ISoulsFile? file)
    {
        DeferredFormatHandling.Process(DeferFormat.Lua, srcPath);
    }

    public override void Repack(string srcPath)
    {
        throw new System.NotImplementedException();
    }

    public override string GetRepackDestPath(string srcPath, XElement xml)
    {
        throw new System.NotImplementedException();
    }
}