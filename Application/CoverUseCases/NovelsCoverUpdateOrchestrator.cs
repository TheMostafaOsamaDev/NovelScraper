using System;
using System.Collections.Generic;
using System.Linq;
using NovelScraper.Domain.Entities.Covers;
using NovelScraper.Domain.Entities.Settings;
using NovelScraper.Infrastructure;

namespace NovelScraper.Application.CoverUseCases;

public class NovelsCoverUpdateOrchestrator
{
    private readonly NovelFolderScanner _folderScanner;
    private readonly NovelCoversUpdater _coversUpdater;

    public NovelsCoverUpdateOrchestrator(
        NovelFolderScanner folderScanner,
        NovelCoversUpdater coversUpdater)
    {
        _folderScanner = folderScanner;
        _coversUpdater = coversUpdater;
    }

    public IEnumerable<NovelCoverUpdateReport> Execute(UserSettings settings)
    {
        var novelFolders = _folderScanner.FindNovelDirectories(settings);
        if (!novelFolders.Any())
        {
            Logger.LogError("No novels found to update. Please verify your novels directory.");
            return Array.Empty<NovelCoverUpdateReport>();
        }

        var reports = new List<NovelCoverUpdateReport>();
        foreach (var novelFolder in novelFolders)
        {
            var report = _coversUpdater.UpdateCoversForNovel(novelFolder);
            reports.Add(report);
        }

        return reports;
    }
}
