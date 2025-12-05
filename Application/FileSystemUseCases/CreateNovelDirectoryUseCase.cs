using NovelScraper.Helpers;

namespace NovelScraper.Application.FileSystemUseCases;

public abstract class CreateNovelDirectoryUseCase
{
    private const int MaxSegmentLength = 80;

    public static string Execute(string basePath, string novelTitle)
    {
        var sanitizedTitle = PathHelper.SanitizeAndTrim(novelTitle, MaxSegmentLength, "Novel");
        var novelDirectoryPath = Path.Combine(basePath, sanitizedTitle);

        if (!Directory.Exists(novelDirectoryPath))
        {
            Directory.CreateDirectory(novelDirectoryPath);
        }

        return novelDirectoryPath;
    }
}