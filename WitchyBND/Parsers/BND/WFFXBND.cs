using SoulsFormats;

namespace WitchyBND.Parsers;

public class WFFXBND : WBinderParser
{

    public override string Name => "FFXBND";
    public override bool Is(string path)
    {
        return !Program.Configuration.Bnd && (path.EndsWith(".ffxbnd") || path.EndsWith(".ffxbnd.dcx")) && BND4.Is(path);
    }

    public override bool IsUnpacked(string path)
    {
        throw new System.NotImplementedException();
    }

    public override void Unpack(string srcPath)
    {
        throw new System.NotImplementedException();
    }

    public override void Repack(string srcPath)
    {
        throw new System.NotImplementedException();
    }

    public override string GetUnpackDestPath(string srcPath)
    {
        throw new System.NotImplementedException();
    }

    public override string GetRepackDestPath(string srcDirPath, string destFileName)
    {
        throw new System.NotImplementedException();
    }
}