namespace NovelScraper.Helpers;

using System.Text.RegularExpressions;

public static class PathHelper
{
    public static string SanitizeFileName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var invalidChars = Path.GetInvalidFileNameChars();
        var pattern = $"[{Regex.Escape(new string(invalidChars))}]";
        return Regex.Replace(input, pattern, "");
    }

    public static string SanitizeAndTrim(string input, int maxLength, string fallback)
    {
        var sanitized = SanitizeFileName(input).Trim();
        if (string.IsNullOrWhiteSpace(sanitized))
            sanitized = fallback;

        if (sanitized.Length > maxLength)
            sanitized = sanitized[..maxLength].TrimEnd('.', ' ');

        if (string.IsNullOrWhiteSpace(sanitized))
            sanitized = fallback;

        return sanitized;
    }
}