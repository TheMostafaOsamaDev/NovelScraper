using NovelScraper.Domain.Enums;

namespace NovelScraper.Domain.Entities;

public class Line
{
    private LineType _lineType = LineType.Text;
    private string _content = string.Empty;
    
    public LineType LineType => _lineType;
    public string Content => _content;
    
    public Line(LineType lineType, string content)
    {
        _lineType = lineType;
        _content = content;
    }
}