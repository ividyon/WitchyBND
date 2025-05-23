using SoulsFormats;

namespace WitchyBND.Parsers;

public class WMATBINBND : WBND4Unsorted
{
    public override string Name => "MATBINBND";
    public override string Extension => "matbinbnd";
    public override UnsortedFileFormat[] PackedFormats => new[]
    {
        new UnsortedFileFormat("*.matbin", Binder.FileFlags.Flag1, new DCX.NoCompressionData())
    };
}