using System.Text.Encodings.Web;
using System.Text.Json;
using NovelScraper.Domain.Entities;
using NovelScraper.Domain.Entities.Novel;
using NovelScraper.Domain.Entities.Settings;
using NovelScraper.Helpers;

namespace NovelScraper.Application.FileSystemUseCases;

public class GetJSONCachedFolderUseCase
{
    public static string Execute(string novelTitle, string volumeTitle)
    {
        var jsonCachePath = Path.Combine(Configuration.JsonCacheDirectory, PathHelper.SanitizeFileName(novelTitle),
            PathHelper.SanitizeFileName(volumeTitle));

        if (!Directory.Exists(jsonCachePath))
            Directory.CreateDirectory(jsonCachePath);

        return jsonCachePath;
    }
}