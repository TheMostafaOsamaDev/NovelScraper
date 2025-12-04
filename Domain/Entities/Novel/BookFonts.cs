namespace NovelScraper.Domain.Entities.Novel;

public static class BookFonts
{
    private static string FontsBasePath { get; set; }
    private static readonly Dictionary<string, Font> FontsDict = new();
    private static Font ChosenFont { get; set; }

    static BookFonts()
    {
        AssignFontsBasePath();
        LoadAllFonts();
        SelectDefaultFont();
    }

    private static void AssignFontsBasePath()
    {
        string baseDir = AppContext.BaseDirectory;

        FontsBasePath =
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "Static", "Fonts"));
    }

    private static void LoadAllFonts()
    {
        var fontsDirectories = Directory.GetDirectories(FontsBasePath);

        foreach (var dir in fontsDirectories)
        {
            var folderName = Path.GetFileName(dir);
            var fontPath = Path.Combine(dir, "Index.ttf");
            var font = new Font(folderName, fontPath);

            FontsDict[folderName] = font;
        }
    }


    private static void SelectDefaultFont()
    {
        if (FontsDict.Count == 0)
            throw new Exception("No fonts found in FontsBasePath.");

        ChosenFont = FontsDict.Values.First();
        Console.WriteLine($"Default font selected: {ChosenFont.ClassName}");
    }


    public static Font GetFont()
    {
        return ChosenFont;
    }

    public class Font
    {
        public string ClassName { get; }
        public string FontPath { get; }
        public string ResourceName { get; set; }

        public FileStream FontStream { get; set; }
        public string StyleContent { get; set; }

        public Font(string className, string fontPath)
        {
            ClassName = className;
            FontPath = fontPath; // already full path
        }
    }
}