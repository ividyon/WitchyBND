using System.Collections.Generic;
using System.Linq;

namespace WitchyFormats;

public static class WitchyParam
{
    public static bool FitsGameVersion(this PARAMDEF.Field field, ulong version)
    {
        return version == 0 || field.FirstRegulationVersion <= version && (field.RemovedRegulationVersion == 0 ||
                                                           field.RemovedRegulationVersion > version);
    }
    public static List<PARAMDEF.Field> FilterByGameVersion(this List<PARAMDEF.Field> fields, ulong version)
    {
        return fields.Where(field => field.FitsGameVersion(version)).ToList();
    }
}