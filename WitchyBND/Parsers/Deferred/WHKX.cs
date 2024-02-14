using System;
using System.IO;
using System.Xml.Linq;
using SoulsFormats;

namespace WitchyBND.Parsers;

public class WHKX : WSingleFileParser
{

    public override string Name => "HKX";
    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
        file = null;
        var extension = Path.GetExtension(path).ToLower();
        var cond = extension is ".hkx";
        if (cond && !Configuration.DeferTools.ContainsKey(DeferFormat.Hkx))
            throw new DeferToolPathException(DeferFormat.Hkx);
        return cond;
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
        throw new NotSupportedException();
    }

    public override void Unpack(string srcPath, ISoulsFile? file)
    {
        DeferredFormatHandling.Process(DeferFormat.Hkx, srcPath);
    }

    public override void Repack(string srcPath)
    {
        throw new NotSupportedException();
    }

    public override string GetRepackDestPath(string srcPath, XElement xml)
    {
        throw new NotSupportedException();
    }
}