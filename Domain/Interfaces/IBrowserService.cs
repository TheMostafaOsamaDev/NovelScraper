namespace NovelScraper.Domain.Interfaces;

public interface IBrowserService
{
    Task InitializeAsync(bool headless = false);
    Task CreateNewPageAsync(string url);
}