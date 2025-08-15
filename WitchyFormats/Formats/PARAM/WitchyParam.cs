using System.Collections.Generic;
using System.Linq;
using SoulsFormats;

namespace WitchyFormats;

public static class WitchyParam
{
    public static IEnumerable<PARAMDEF.Field> FilterByGameVersion(this IEnumerable<PARAMDEF.Field> fields, ulong version)
    {
        return fields.Where(field => field.FitsGameVersion(version));
    }
}