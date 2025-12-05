using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NovelScraper.Domain.Entities.Covers;
using NovelScraper.Domain.Interfaces;

namespace NovelScraper.Application.CoverUseCases;

public class NovelCoversUpdater
{
    private readonly IEpubCoverService _epubCoverService;
    private static readonly string[] SupportedImageExtensions = { "*.png", "*.jpg", "*.jpeg", "*.gif", "*.bmp", "*.webp" };

    public NovelCoversUpdater(IEpubCoverService epubCoverService)
    {
        _epubCoverService = epubCoverService;
    }

    public NovelCoverUpdateReport UpdateCoversForNovel(string novelDirectory)
    {
        var novelName = Path.GetFileName(novelDirectory);
        var coversFolder = Path.Combine(novelDirectory, "covers");
        var epubFiles = Directory.GetFiles(novelDirectory, "*.epub").OrderBy(f => f).ToList();

        if (!epubFiles.Any())
        {
            return new NovelCoverUpdateReport(
                novelName,
                Array.Empty<string>(),
                new[] { "No EPUB files detected" },
                coversFolderMissing: !Directory.Exists(coversFolder),
                hasVolumes: false);
        }

        if (!Directory.Exists(coversFolder))
        {
            return new NovelCoverUpdateReport(
                novelName,
                Array.Empty<string>(),
                epubFiles.Select(Path.GetFileName).Where(n => n != null).Cast<string>().ToList(),
                coversFolderMissing: true,
                hasVolumes: true);
        }

        var coverFiles = Directory
            .GetFiles(coversFolder)
            .Where(f => SupportedImageExtensions.Any(ext => 
                f.EndsWith(ext.TrimStart('*'), StringComparison.OrdinalIgnoreCase)))
            .OrderBy(f => f)
            .ToList();

        if (!coverFiles.Any())
        {
            return new NovelCoverUpdateReport(
                novelName,
                Array.Empty<string>(),
                epubFiles.Select(Path.GetFileName).Where(n => n != null).Cast<string>().ToList(),
                coversFolderMissing: false,
                hasVolumes: true);
        }

        var updatedVolumes = new List<string>();
        var missingCovers = new List<string>();

        for (int i = 0; i < epubFiles.Count; i++)
        {
            var epubFile = epubFiles[i];
            var coverFile = i < coverFiles.Count ? coverFiles[i] : null;

            if (coverFile == null)
            {
                missingCovers.Add(Path.GetFileName(epubFile)!);
                continue;
            }

            _epubCoverService.ApplyCover(epubFile, coverFile);
            updatedVolumes.Add(Path.GetFileName(epubFile)!);
        }

        return new NovelCoverUpdateReport(
            novelName,
            updatedVolumes,
            missingCovers,
            coversFolderMissing: false,
            hasVolumes: true);
    }
}
