using Microsoft.Playwright;
using NovelScraper.Application.FileSystem;
using NovelScraper.Application.FileSystemUseCases;
using NovelScraper.Domain.Entities;
using NovelScraper.Domain.Entities.Novel;
using NovelScraper.Domain.Enums;
using NovelScraper.Domain.Enums.Entities.Website;
using NovelScraper.Helpers;
using NovelScraper.Infrastructure.Interfaces;

namespace NovelScraper.Infrastructure.Websites;

public class KolNovel(string startUrl, IBrowserInfrastructure browserService) : Website
{
    public override string Name { get; } = "KolNovel";
    public new static string BaseUrl { get; } = "https://free.kolnovel.com";

    private string StartUrl { get; } = startUrl;
    private readonly SemaphoreSlim _browserSemaphore = new(8, 8);
    private Configuration _config { set; get; }

    // Limit concurrent browser operations to prevent resource exhaustion
    public override async Task<Volume[]> StartScrapingAsync(Configuration configuration)
    {
        _config = configuration;
        var savingDirectory = _config.SavingDirectory;
        var startVolume = _config.StartVolume;
        var endVolume = _config.EndVolume;
        
        
        // 1. Initialize the browser service
        await browserService.InitializeAsync(headless: true);

        // 2. Get the page and navigate to StartUrl
        var page = await browserService.GetPageAsync();
        await page.GotoAsync(StartUrl);

        // 3. Wait for the selector and query elements
        await page.WaitForSelectorAsync(".ts-chl-collapsible");
        var elements =
            (await page.QuerySelectorAllAsync(".ts-chl-collapsible")).Reverse().ToList();

        if (endVolume == null || endVolume == 0)
        {
            endVolume = elements.Count;
        }
        

        var volumes = new List<Volume>();
        var currentChapterId = 1;

        for (int i = 0; i < elements.Count; i++)
        {
            var volumeElement = elements[i];
            var volumeId = i + 1;
            var volumeTitle = (await volumeElement.TextContentAsync()) ?? $"Volume {volumeId}";
            
            if (startVolume.HasValue && volumeId < startVolume.Value)
            {
                Console.WriteLine($"Skipping volume {volumeTitle}");
                continue;
            }

            var volumePath =
                CreateVolumeDirectoryUseCase.Execute(volumeId, volumeTitle, savingDirectory);

            var volume = new Volume(volumeId, volumeTitle, volumePath);

            var anchorElements =
                (await volumeElement.QuerySelectorAllAsync("+ .ts-chl-collapsible-content ul li a"))
                .Reverse().ToList();

            for (int j = 0; j < anchorElements.Count; j++)
            {
                var anchorElement = anchorElements[j];
                var url = await anchorElement.GetAttributeAsync("href");

                var chapterTitleElement = await anchorElement.QuerySelectorAsync(".epl-title");
                var chapterTitle =
                    await chapterTitleElement?.TextContentAsync()! ?? $"{currentChapterId} - No Title";

                var chapter = new Chapter(currentChapterId++, chapterTitle, url);
                volume.AddChapter(chapter);
            }

            volumes.Add(volume);

            if (endVolume.HasValue && volumeId >= endVolume.Value)
            {
                Console.WriteLine($"Breaking at volume {volumeId} (endVolume: {endVolume})");
                break;
            }
        }

        await page.CloseAsync();

        // 4. Start downloading chapters with parallel processing for both volumes and chapters
        await ProcessVolumesInParallel(volumes);

        return volumes.ToArray();
    }

    private async Task ProcessVolumesInParallel(List<Volume> volumes)
    {
        // Process volumes in parallel
        await Parallel.ForEachAsync(
            volumes,
            new ParallelOptions { MaxDegreeOfParallelism = 2 }, // Limit volume parallelism
            async (volume, ct) =>
            {
                Logger.LogVolumeStarted(volume.VolumeId, volume.BookTitle);

                try
                {
                    // Process chapters within each volume in parallel
                    await ProcessChaptersInParallel(volume, ct);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error processing volume {volume.VolumeId}: {ex.Message}");
                }

                Logger.LogVolumeCompleted(volume.VolumeId, volume.BookTitle);
            });
    }

    private async Task ProcessChaptersInParallel(Volume volume, CancellationToken ct)
    {
        // Process chapters in parallel within the volume
        await Parallel.ForEachAsync(
            volume.Chapters,
            new ParallelOptions 
            { 
                MaxDegreeOfParallelism = 4, // Limit chapter parallelism
                CancellationToken = ct
            },
            async (chapter, innerCt) =>
            {
                Logger.LogChapterStarted(chapter.ChapterId, chapter.Title);

                try
                {
                    volume.VolumeCachedPath = GetJSONCachedFolderUseCase.Execute(_config.NovelTitle, volume.BookTitle);
                    
                    // Check if chapter already exists
                    var isChapterExists = SingleFileUseCase.IsChapterExists(volume.VolumeCachedPath, chapter);
                    if (isChapterExists)
                    {
                        Logger.LogChapterSkipped(chapter.ChapterId, chapter.Title);
                        var chapterData = SingleFileUseCase.GetChapter(volume.VolumeCachedPath, chapter);

                        if (chapterData?.Lines != null) 
                        {
                            // Use thread-safe operations for adding lines
                            lock (chapter.Lines)
                            {
                                chapter.Lines.AddRange(chapterData.Lines);
                            }
                        }
                        return;
                    }

                    await ProcessSingleChapter(volume, chapter, innerCt);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error processing chapter {chapter.ChapterId}: {ex.Message}");
                }
            });
    }

    private async Task ProcessSingleChapter(Volume volume, Chapter chapter, CancellationToken ct)
    {
        // Use semaphore to limit concurrent browser operations
        await _browserSemaphore.WaitAsync(ct);
        
        IPage? page = null;
        try
        {
            // Get a new page for this chapter
            page = await browserService.GetPageAsync();
            await page.GotoAsync(chapter.Url);

            await page.WaitForFunctionAsync(@"
                () => {
                    const paragraphs = document.querySelectorAll('.entry-content p');
                    return Array.from(paragraphs).some(p => p.textContent.trim().length > 0);
                }
            ");

            var allParagraphs = await page.QuerySelectorAllAsync(".entry-content p");
            var lines = new List<Line>(); // Use local list to avoid threading issues

            foreach (var para in allParagraphs)
            {
                bool isHidden = await IsHiddenByStyle(para);
                if (isHidden) continue;

                var innerText = await para.InnerTextAsync();
                if (string.IsNullOrWhiteSpace(innerText)) continue;

                var isUrl = UrlHelper.IsUrl(innerText);
                var lineType = isUrl ? LineType.Image : LineType.Text;
                var line = new Line(lineType, innerText);

                lines.Add(line);
            }

            // Thread-safe assignment of lines
            lock (chapter.Lines)
            {
                chapter.Lines.AddRange(lines);
            }

            SaveChaptersToJsonUseCase.Execute(volume.VolumeCachedPath, chapter);
            Logger.LogChapterCompleted(chapter.ChapterId, chapter.Title);
        }
        finally
        {
            if (page != null)
            {
                await page.CloseAsync();
            }
            _browserSemaphore.Release();
        }
    }

    private async Task<bool> IsHiddenByStyle(IElementHandle element)
    {
        return await element.EvaluateAsync<bool>(@"(p) => {
            const style = window.getComputedStyle(p);
            
            const checks = {
                height: style.height === '0.1px',
                overflow: style.overflow === 'hidden',
                position: style.position === 'fixed',
                opacity: style.opacity === '0',
                textIndent: style.textIndent === '-99999px',
                bottom: style.bottom === '-999px',
                offsetHeight: p.offsetHeight === 0,
                offsetWidth: p.offsetWidth === 0
            };

            return (
                (checks.height &&
                 checks.overflow &&
                 checks.position &&
                 checks.opacity &&
                 checks.textIndent &&
                 checks.bottom) ||
                checks.offsetHeight ||
                checks.offsetWidth
            );
        }");
    }

    private bool IsIgnoredLine(string line)
    {
        foreach (var pattern in _ignoredLines)
        {
            var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$";
            if (System.Text.RegularExpressions.Regex.IsMatch(line, regexPattern,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private List<string> _ignoredLines = new()
    {
        "*إقرأ* رواياتنا* فقط* على* مو*قع م*لوك الرو*ايات ko*lno*vel ko*lno*vel. com"
    };

    public void Dispose()
    {
        _browserSemaphore?.Dispose();
    }
}