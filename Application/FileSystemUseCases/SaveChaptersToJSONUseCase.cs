using System.Text.Encodings.Web;
using System.Text.Json;
using NovelScraper.Domain.Entities;
using NovelScraper.Helpers;

namespace NovelScraper.Application.FileSystem;

public abstract class SaveChaptersToJsonUseCase
{
    public static void Execute(string volumePath, Chapter chapter)
    {
        if (!Directory.Exists(volumePath))
            Directory.CreateDirectory(volumePath);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };


        var chapterTitle = $"{chapter.ChapterId} - {chapter.Title}.json";
        var sanitizedChapterTitle = PathHelper.SanitizeFileName(chapterTitle);
        var filePath = Path.Combine(volumePath, sanitizedChapterTitle);

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