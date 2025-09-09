using Microsoft.Playwright;
using NovelScraper.Domain.Interfaces;

namespace NovelScraper.Infrastructure.Interfaces;

public interface IBrowserInfrastructure : IBrowserService
{
    public Task<IPage> GetPageAsync();
}