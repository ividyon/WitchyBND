using System.Numerics;

namespace WitchyLib;

public static class VecHelpers
{
    public static string Vector3ToString(this Vector3 vector)
    {
        return $"X:{vector.X} Y:{vector.Y} Z:{vector.Z}";
    }

    public static Vector3 ToVector3(this string str)
    {
        int xStartIndex = str.IndexOf("X:") + 2;
        int yStartIndex = str.IndexOf("Y:") + 2;
        int zStartIndex = str.IndexOf("Z:") + 2;

        string xStr = str.Substring(xStartIndex, yStartIndex - xStartIndex - 3);
        string yStr = str.Substring(yStartIndex, zStartIndex - yStartIndex - 3);
        string zStr = str.Substring(zStartIndex, str.Length - zStartIndex);

        float x = float.Parse(xStr);
        float y = float.Parse(yStr);
        float z = float.Parse(zStr);

        return new Vector3(x, y, z);
    }
}