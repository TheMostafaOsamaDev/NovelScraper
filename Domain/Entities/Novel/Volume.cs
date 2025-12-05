namespace NovelScraper.Domain.Entities;

public class Volume(int volumeId, string bookTitle, string volumePath)
{
    private readonly List<Chapter> _chapters = new();

    public int VolumeId { get; } = volumeId;
    public string BookTitle { get; } = bookTitle;
    public string VolumePath { get; } = volumePath;
    public string VolumeCachedPath { set; get; }
    public List<Chapter> Chapters => _chapters;
    
    public void AddChapter(Chapter chapter)
    {
        _chapters.Add(chapter);
    }
    
    public void InsertLines(Chapter chapter)
    {
        var existingChapter = _chapters.FirstOrDefault(c => c.ChapterId == chapter.ChapterId);
        if (existingChapter != null)
        {
            existingChapter.Lines.AddRange(chapter.Lines);
        }
    }
}