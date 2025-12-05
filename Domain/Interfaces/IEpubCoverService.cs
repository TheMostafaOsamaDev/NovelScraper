namespace NovelScraper.Domain.Interfaces;

public interface IEpubCoverService
{
    void ApplyCover(string epubFilePath, string coverImagePath);
}

