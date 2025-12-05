using System.Text.Encodings.Web;
using System.Text.Json;
using NovelScraper.Domain.Entities;
using NovelScraper.Domain.Entities.Novel;
using NovelScraper.Domain.Entities.Settings;
using NovelScraper.Helpers;

namespace NovelScraper.Application.FileSystemUseCases;

public class GetJSONCachedFolderUseCase
{
    private const int MaxSegmentLength = 60;

    public static string Execute(string novelTitle, string volumeTitle)
    {
        var novelSegment = PathHelper.SanitizeAndTrim(novelTitle, MaxSegmentLength, "Novel");
        var volumeSegment = PathHelper.SanitizeAndTrim(volumeTitle, MaxSegmentLength, "Volume");
        var jsonCachePath = Path.Combine(Configuration.JsonCacheDirectory, novelSegment, volumeSegment);

        if (!Directory.Exists(jsonCachePath))
            Directory.CreateDirectory(jsonCachePath);

        return jsonCachePath;
    }
}