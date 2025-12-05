using System.Text.Json;
using NovelScraper.Domain.Entities;
using NovelScraper.Helpers;

namespace NovelScraper.Application.FileSystemUseCases;

public static class SingleFileUseCase
{
    private const int MaxChapterFileLength = 100;
    private static string SavePath { set; get; } = "";

    private static string GetSavePath(string volumeCachePath, Chapter chapter)
    {
        var chapterTitle = $"{chapter.ChapterId} - {chapter.Title}.json";
        var sanitizedChapterTitle = PathHelper.SanitizeAndTrim(chapterTitle, MaxChapterFileLength, $"Chapter-{chapter.ChapterId}");
        SavePath = Path.Combine(volumeCachePath, sanitizedChapterTitle);

        return SavePath;
    }
    
    public static bool IsChapterExists(string volumeCachePath, Chapter chapter)
    {
        var filePath = GetSavePath(volumeCachePath, chapter);

        Console.WriteLine($"Searched of it in: {filePath}");

        return File.Exists(filePath);
    }

    public static Chapter? GetChapter(string volumeCachePath, Chapter chapter)
    {
        try
        {
            var filePath = GetSavePath(volumeCachePath, chapter);

            var json = File.ReadAllText(filePath);
            var chapterData = JsonSerializer.Deserialize<Chapter>(json);

            SavePath = "";

            return chapterData;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}