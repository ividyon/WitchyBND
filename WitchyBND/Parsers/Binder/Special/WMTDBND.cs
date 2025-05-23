using SoulsFormats;

namespace WitchyBND.Parsers;

public class WMTDBND : WBND4Unsorted
{
    public override string Name => "MTDBND";
    public override string Extension => "mtdbnd";
    public override UnsortedFileFormat[] PackedFormats => new[]
    {
        new UnsortedFileFormat("*.mtd", Binder.FileFlags.Flag1, new DCX.NoCompressionData())
    };
}