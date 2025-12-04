using NovelScraper.Helpers;

namespace NovelScraper.Application.FileSystem;

public class CreateNovelDirectoryUseCase
{
    public static string Execute(string basePath, string novelTitle)
    {
        var sanitizedTitle = PathHelper.SanitizeFileName(novelTitle);
        var novelDirectoryPath = Path.Combine(basePath, sanitizedTitle);

        if (!Directory.Exists(novelDirectoryPath))
        {
            Directory.CreateDirectory(novelDirectoryPath);
        }

        return novelDirectoryPath;
    }
}