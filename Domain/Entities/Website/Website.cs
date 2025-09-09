using NovelScraper.Domain.Entities;

namespace NovelScraper.Domain.Enums.Entities.Website;

public abstract class Website
{
    public abstract string Name { get; }
    public abstract string BaseUrl { get; }

    public abstract Task<Volume[]> StartScrapingAsync(Configuration configuration);
}