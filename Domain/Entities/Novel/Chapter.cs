using System.Text;
namespace NovelScraper.Domain.Entities;

public class Chapter
{
    public int ChapterId { get; set; }
    public string Title { get; set; }
    public string Url { get; set; }
    public List<Line>? Lines { get; set; }

    public Chapter()
    {
        Lines = new List<Line>();
    }

    public Chapter(int chapterId, string title, string url)
    {
        ChapterId = chapterId;
        Title = title;
        Url = url;
        Lines = new List<Line>();
    }
}