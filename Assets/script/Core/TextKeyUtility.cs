using System;
using System.Linq;

public static class TextKeyUtility
{
    public static string NormalizeLoose(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : new string(value.Where(ch => !char.IsWhiteSpace(ch)).ToArray()).ToLowerInvariant();
    }

    public static string NormalizeKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value
            .Trim()
            .ToLowerInvariant()
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Replace("_", string.Empty);
    }

    public static string NormalizeResourceId(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : new string(value.Where(ch => !char.IsWhiteSpace(ch)).ToArray()).ToLowerInvariant();
    }
}
