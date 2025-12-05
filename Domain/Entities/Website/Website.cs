using NovelScraper.Domain.Entities;
using NovelScraper.Domain.Entities.Novel;

namespace NovelScraper.Domain.Enums.Entities.Website;

public abstract class Website
{
    public abstract string Name { get; }
    public static string BaseUrl { get; }

    public abstract Task<Volume[]> StartScrapingAsync(Configuration configuration);

    // public static bool IsMatchingUrl(string? url = null)
    // {
    //     Console.WriteLine(BaseUrl);
    //     return !string.IsNullOrWhiteSpace(url) && url.StartsWith(BaseUrl);
    // }
}