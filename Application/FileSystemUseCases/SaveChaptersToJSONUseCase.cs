using System.Text.Encodings.Web;
using System.Text.Json;
using NovelScraper.Domain.Entities;
using NovelScraper.Domain.Entities.Novel;
using NovelScraper.Helpers;

namespace NovelScraper.Application.FileSystemUseCases;

public abstract class SaveChaptersToJsonUseCase
{
    public static void Execute(string jsonCachePath, Chapter chapter)
    {
        if (!Directory.Exists(jsonCachePath))
            throw new DirectoryNotFoundException("Could found the cache path please create it first");

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };


        var chapterTitle = $"{chapter.ChapterId} - {chapter.Title}.json";
        var sanitizedChapterTitle = PathHelper.SanitizeFileName(chapterTitle);
        var filePath = Path.Combine(jsonCachePath, sanitizedChapterTitle);

        var isFileExists = File.Exists(filePath);
        if (isFileExists)
        {
            Console.WriteLine($"File {sanitizedChapterTitle} already exists. Skipping...");
        }
        else
        {
            string json = JsonSerializer.Serialize(chapter, options);

            File.WriteAllText(filePath, json);
            Console.WriteLine($"Chapter {chapter.ChapterId} saved to JSON file successfully.");
        }
    }
}