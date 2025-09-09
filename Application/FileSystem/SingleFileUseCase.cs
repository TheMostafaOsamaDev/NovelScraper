using System.Text.Json;
using NovelScraper.Domain.Entities;
using NovelScraper.Helpers;

namespace NovelScraper.Application.FileSystem;

public static class SingleFileUseCase
{
    private static string _savePath { set; get; } = "";

    private static string GetSavePath(string volumePath, Chapter chapter)
    {
        var chapterTitle = $"{chapter.ChapterId} - {chapter.Title}.json";
        var sanitizedChapterTitle = PathHelper.SanitizeFileName(chapterTitle);
        _savePath = Path.Combine(volumePath, sanitizedChapterTitle);

        return _savePath;
    }
    
    public static bool IsChapterExists(string volumePath, Chapter chapter)
    {
        var filePath = GetSavePath(volumePath, chapter);

        Console.WriteLine($"Searched of it in: {filePath}");

        return File.Exists(filePath);
    }

    public static Chapter? GetChapter(string volumePath, Chapter chapter)
    {
        try
        {
            var filePath = GetSavePath(volumePath, chapter);

            var json = File.ReadAllText(filePath);
            var chapterData = JsonSerializer.Deserialize<Chapter>(json);

            _savePath = "";

            return chapterData;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}