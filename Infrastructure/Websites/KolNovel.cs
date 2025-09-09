using Microsoft.Playwright;
using NovelScraper.Application.FileSystem;
using NovelScraper.Domain.Entities;
using NovelScraper.Domain.Enums;
using NovelScraper.Domain.Enums.Entities.Website;
using NovelScraper.Domain.Interfaces;
using NovelScraper.Helpers;
using NovelScraper.Infrastructure.Interfaces;

namespace NovelScraper.Infrastructure.Websites;

public class KolNovel : Website
{
    public override string Name { get; } = "KolNovel";
    public override string BaseUrl { get; } = "https://kolnovel.site";

    public string StartUrl { get; }
    private IBrowserInfrastructure _browserService;

    public KolNovel(string startUrl, IBrowserInfrastructure browserService)
    {
        StartUrl = startUrl;
        _browserService = browserService;
    }

    public override async Task<Volume[]> StartScrapingAsync(Configuration configuration)
    {
        string savingDirectory = configuration.SavingDirectory;
        int? startVolume = configuration.StartVolume;
        int? endVolume = configuration.EndVolume;
        
        // 1. Initialize the browser service
        await _browserService.InitializeAsync(headless: true);


        // 2. Get the page and navigate to StartUrl
        var page = await _browserService.GetPageAsync();
        await page.GotoAsync(StartUrl);

        // 3. Wait for the selector and query elements
        await page.WaitForSelectorAsync(".ts-chl-collapsible");
        var elements =
            (await page.QuerySelectorAllAsync(".ts-chl-collapsible")).Reverse().ToList();

        var volumes = new List<Volume>();
        var currentChapterId = 1;

        for (int i = 0; i < elements.Count; i++)
        {
            

            var volumeElement = elements[i];
            var volumeId = i + 1;
            var volumeTitle = (await volumeElement.TextContentAsync()) ?? $"Volume {volumeId}";
            
            if (i < startVolume)
            {
                Console.WriteLine($"skipping volume {volumeTitle}");
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
                    await chapterTitleElement?.TextContentAsync()! ?? $"{currentChapterId++} - No Title";


                var chapter = new Chapter(currentChapterId++, chapterTitle, url);

                volume.AddChapter(chapter);
            }


            volumes.Add(volume);

            if (i == endVolume)
            {
                Console.WriteLine($"Breaking {i} == {endVolume}");
                break;
            }
        }


        await page.CloseAsync();

        // 4. Start downloading chapters
        await Parallel.
            ForEachAsync
            (volumes,
            new ParallelOptions { MaxDegreeOfParallelism = 4 }, async (vol, ct) =>
            {
                Logger.LogVolumeStarted(vol.VolumeId, vol.BookTitle);

                for (int i = 0; i < vol.Chapters.Count; i++)
                {
                    var chapter = vol.Chapters[i];

                    Logger.LogChapterStarted(chapter.ChapterId, chapter.Title);

                    var isChapterExists = 
                        SingleFileUseCase.IsChapterExists(vol.VolumePath, chapter);
                    if (isChapterExists)
                    {
                        Logger.LogChapterSkipped(chapter.ChapterId, chapter.Title);
                        var chapterData = SingleFileUseCase.GetChapter(vol.VolumePath, chapter);

                        if (chapterData?.Lines != null) chapter.Lines?.AddRange(chapterData?.Lines);

                        continue;
                    }

                    IPage? page = null;
                    try
                    {
                        // Get a new page for this chapter
                        page = await _browserService.GetPageAsync();
                        await page.GotoAsync(chapter.Url);

                        await page.WaitForFunctionAsync(@"
                        () => {
                            const paragraphs = document.querySelectorAll('.entry-content p');
                            return Array.from(paragraphs).some(p => p.textContent.trim().length > 0);
                        }
                    ");

                        var allParagraphs = await page.QuerySelectorAllAsync(".entry-content p");

                        foreach (var para in allParagraphs)
                        {
                            bool isHidden = await IsHiddenByStyle(para);

                            if (isHidden) continue;

                            var innerText = await para.InnerTextAsync();
                            if (string.IsNullOrWhiteSpace(innerText)) continue;

                            var isUrl = UrlHelper.IsUrl(innerText);
                            var lineType = isUrl ? LineType.Image : LineType.Text;
                            var line = new Line(lineType, innerText);

                            chapter.Lines.Add(line);
                        }

                        SaveChaptersToJsonUseCase.Execute(vol.VolumePath, chapter);
                        Logger.LogChapterCompleted(chapter.ChapterId, chapter.Title);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Error processing chapter {chapter.ChapterId}: {ex.Message}");
                    }
                    finally
                    {
                        if (page != null)
                        {
                            await page.CloseAsync();
                        }
                    }
                }

                Logger.LogVolumeCompleted(vol.VolumeId, vol.BookTitle);
            });


        return volumes.ToArray();
    }

    private async Task<bool> IsHiddenByStyle(IElementHandle element)
    {
        return await element.EvaluateAsync<bool>(@"(p) => {
        const style = window.getComputedStyle(p);
        console.log(p);
        console.log('Style:', style);

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

        Object.entries(checks).forEach(([key, value]) => {
            console.log(key, value);
        });

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
}