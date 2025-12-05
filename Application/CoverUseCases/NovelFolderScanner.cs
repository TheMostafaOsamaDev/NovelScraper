using System;
using System.Collections.Generic;
using System.IO;
using NovelScraper.Domain.Entities.Settings;

namespace NovelScraper.Application.CoverUseCases;

public class NovelFolderScanner
{
    public IEnumerable<string> FindNovelDirectories(UserSettings settings)
    {
        var novelsRoot = settings.NovelsPath;
        if (string.IsNullOrWhiteSpace(novelsRoot) || !Directory.Exists(novelsRoot))
        {
            Console.WriteLine("Novels directory is invalid or does not exist.");
            return Array.Empty<string>();
        }

        return Directory.GetDirectories(novelsRoot);
    }
}
