namespace NoteTool.Extensions;

public static class StringExtensions {
    public static string SubstringSafe(this string text, int start, int length)
    {
        if (string.IsNullOrEmpty(text)) 
            return string.Empty;      

        return text.Length <= start ? ""
            : text.Length - start <= length ? text.Substring(start)
            : text.Substring(start, length);
    }
}