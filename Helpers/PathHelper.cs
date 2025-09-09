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
}