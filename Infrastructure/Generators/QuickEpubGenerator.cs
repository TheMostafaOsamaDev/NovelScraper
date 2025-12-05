using System.Net;
using System.Text;
using NovelScraper.Application.FileSystem;
using NovelScraper.Domain.Entities;
using NovelScraper.Domain.Entities.Novel;
using NovelScraper.Domain.Enums;
using NovelScraper.Helpers;
using QuickEPUB;

namespace NovelScraper.Infrastructure.Generators;

public class QuickEpubGenerator
{
    private string _novelPath { set; get; }
    private string _epubPath { set; get; }
    private List<Volume> _volumes { set; get; }
    private BookFonts.Font _font { set; get; }

    public QuickEpubGenerator(string novelPath, BookFonts.Font font)
    {
        _novelPath = novelPath;
        _volumes = new List<Volume>();
        _font = font;
    }
    
    public void InsertVolumes(List<Volume> volumes)
    {
        _volumes.AddRange(volumes);
    }

    public void GenerateEpub(string novelTitle, string authorName)
    {
        _epubPath = Path.Combine(_novelPath, novelTitle + ".epub");

        var doc = new Epub(novelTitle, authorName ?? "Unknown")
        {
            Language = "ar"
        };

        var cssContent = _font.StyleContent;
        var fontResourceName = _font.ResourceName;
        var fontStream = _font.FontStream;

        // Add font as resource to EPUB
        try
        {
            // Reset stream position to beginning
            if (fontStream.CanSeek)
            {
                fontStream.Position = 0;
            }
            
            doc.AddResource(fontResourceName, EpubResourceType.TTF, fontStream);

            Console.WriteLine($"Font added successfully: {fontResourceName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding font: {ex.Message}");
        }

        foreach (var volume in _volumes)
        {
            // Volume title section with CSS class
            string volumeContent = cssContent + $"<h1 class=\"rtl-content\">{volume.BookTitle}</h1>";
            doc.AddSection(volume.BookTitle, volumeContent);

            foreach (var chapter in volume.Chapters)
            {
                string chapterSection = $"{chapter.ChapterId} - {chapter.Title}";
                var sb = new StringBuilder();
                sb.AppendLine(cssContent); // Add CSS to each chapter
                sb.AppendLine("<div class=\"rtl-content\">");
                sb.AppendLine($"<h2>{chapter.Title}</h2>");

                foreach (var line in chapter.Lines)
                {
                    if (line.LineType == Domain.Enums.LineType.Text)
                    {
                        sb.AppendLine($"<p>{WebUtility.HtmlDecode(line.Content)}</p>");
                    }
                    else if (line.LineType == Domain.Enums.LineType.Image)
                    {
                        sb.AppendLine($"<img src=\"{line.Content}\" alt=\"Image\" />");
                    }
                }

                sb.AppendLine("</div>");
                string chapterContent = sb.ToString();
                doc.AddSection(chapterSection, chapterContent);
            }
        }

        var fs = new FileStream(_epubPath, FileMode.Create);
        doc.Export(fs);
        fs.Close();

        Console.WriteLine($"EPUB generated: {_epubPath}");
    }

    public void GenerateSeparatedEpub(string authorName)
    {
        var styleContent = _font.StyleContent;
        var resourceFont = _font.ResourceName;
        var fontStream = _font.FontStream;
        
        foreach (var volume in _volumes)
        {
            _epubPath = Path.Combine(_novelPath, volume.BookTitle + ".epub");

            var doc = new Epub(volume.BookTitle, authorName ?? "Unknown")
            {
                Language = "ar"
            };


            // Add font as resource to EPUB
            try
            {
                // Reset stream position to beginning before each use
                if (fontStream.CanSeek)
                {
                    fontStream.Position = 0;
                }
                
                doc.AddResource(resourceFont, EpubResourceType.TTF, fontStream);

                Console.WriteLine($"Font added successfully to: {_epubPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding font: {ex.Message}");
            }


            // Volume title section with CSS class
            string volumeContent = styleContent + $"<h1 class=\"rtl-content\">{volume.BookTitle}</h1>";
            doc.AddSection(volume.BookTitle, volumeContent);

            foreach (var chapter in volume.Chapters)
            {
                string chapterSection = $"{chapter.ChapterId} - {chapter.Title}";
                var sb = new StringBuilder();
                sb.AppendLine(styleContent); // Add CSS to each chapter
                sb.AppendLine("<div class=\"rtl-content\">");
                sb.AppendLine($"<h2>{chapter.Title}</h2>");
                

                foreach (var line in chapter.Lines)
                {
                    if (line.LineType == LineType.Text)
                    {
                        sb.AppendLine($"<p>{WebUtility.HtmlDecode(line.Content)}</p>");
                    }
                    else if (line.LineType == LineType.Image)
                    {
                        sb.AppendLine($"<img src=\"{line.Content}\" alt=\"Image\" />");
                    }
                }

                sb.AppendLine("</div>");
                string chapterContent = sb.ToString();
                doc.AddSection(chapterSection, chapterContent);
            }

            var fs = new FileStream(_epubPath, FileMode.Create);
            doc.Export(fs);
            fs.Close();

            Console.WriteLine($"EPUB Separated generated: {_epubPath}");
        }
    }
}