using System.Numerics;

namespace WitchyLib;

public static class VecHelpers
{
    public static string Vector2ToString(this Vector2 vector)
    {
        return $"X:{vector.X} Y:{vector.Y}";
    }

    public static string Vector3ToString(this Vector3 vector)
    {
        return $"X:{vector.X} Y:{vector.Y} Z:{vector.Z}";
    }

    public static string Vector4ToString(this Vector4 vector)
    {
        return $"X:{vector.X} Y:{vector.Y} Z:{vector.Z} W:{vector.W}";
    }

    public static Vector2 ToVector2(this string str)
    {
        int xStartIndex = str.IndexOf("X:") + 2;
        int yStartIndex = str.IndexOf("Y:") + 2;

        string xStr = str.Substring(xStartIndex, yStartIndex - xStartIndex - 3);
        string yStr = str[yStartIndex..];

        float x = float.Parse(xStr);
        float y = float.Parse(yStr);

        return new Vector2(x, y);
    }

    public static Vector3 ToVector3(this string str)
    {
        int xStartIndex = str.IndexOf("X:") + 2;
        int yStartIndex = str.IndexOf("Y:") + 2;
        int zStartIndex = str.IndexOf("Z:") + 2;

        string xStr = str.Substring(xStartIndex, yStartIndex - xStartIndex - 3);
        string yStr = str.Substring(yStartIndex, zStartIndex - yStartIndex - 3);
        string zStr = str[zStartIndex..];

        float x = float.Parse(xStr);
        float y = float.Parse(yStr);
        float z = float.Parse(zStr);

        return new Vector3(x, y, z);
    }

    public static Vector4 ToVector4(this string str)
    {
        int xStartIndex = str.IndexOf("X:") + 2;
        int yStartIndex = str.IndexOf("Y:") + 2;
        int zStartIndex = str.IndexOf("Z:") + 2;
        int wStartIndex = str.IndexOf("W:") + 2;

        string xStr = str.Substring(xStartIndex, yStartIndex - xStartIndex - 3);
        string yStr = str.Substring(yStartIndex, zStartIndex - yStartIndex - 3);
        string zStr = str.Substring(zStartIndex, wStartIndex - zStartIndex - 3);
        string wStr = str[wStartIndex..];

        float x = float.Parse(xStr);
        float y = float.Parse(yStr);
        float z = float.Parse(zStr);
        float w = float.Parse(wStr);

        return new Vector4(x, y, z, w);
    }
}