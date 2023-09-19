namespace WitchyLib;

public static class StringExtensions
{
    public static string PromptPlusEscape(this string str)
    {
        return str.Replace("[", "[[").Replace("]", "]]");
    }
}