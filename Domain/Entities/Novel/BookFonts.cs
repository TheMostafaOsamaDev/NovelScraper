namespace NovelScraper.Domain.Entities.Novel;

public static class BookFonts
{
    private static string FontsBasePath { get; set; }
    private static readonly Dictionary<string, Font> FontsDict = new();
    private static readonly List<Font> Fonts = new();

    static BookFonts()
    {
        AssignFontsBasePath();
        LoadAllFonts();
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
            
            // If Index.ttf doesn't exist, find the first .ttf file in the directory
            if (!File.Exists(fontPath))
            {
                var ttfFiles = Directory.GetFiles(dir, "*.ttf", SearchOption.TopDirectoryOnly);
                if (ttfFiles.Length == 0)
                {
                    Console.WriteLine($"Warning: No .ttf files found in {folderName}, skipping...");
                    continue;
                }
                fontPath = ttfFiles[0];
            }
            
            var font = new Font(folderName, fontPath);

            FontsDict[folderName] = font;
            Fonts.Add(font);
        }

        Fonts.Sort((a, b) => string.Compare(a.ClassName, b.ClassName, StringComparison.Ordinal));
    }

    public static IReadOnlyList<Font> GetAvailableFonts()
    {
        if (Fonts.Count == 0)
            throw new Exception("No fonts found in FontsBasePath.");

        return Fonts;
    }

    public static Font GetDefaultFont()
    {
        return GetAvailableFonts()[0];
    }

    public static Font? GetFontByName(string name)
    {
        return FontsDict.TryGetValue(name, out var font) ? font : null;
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
            
            // Initialize ResourceName from the font file name
            ResourceName = Path.GetFileName(fontPath);
            
            // Initialize FontStream - keep it open for reuse
            FontStream = new FileStream(fontPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            
            // Initialize StyleContent with RTL CSS
            StyleContent = $@"
<style>
    @font-face {{
        font-family: '{className}';
        src: url('{ResourceName}');
    }}
    .rtl-content {{
        direction: rtl;
        text-align: right;
        font-family: '{className}', serif;
    }}
    body {{
        font-family: '{className}', serif;
    }}
</style>";
        }
    }
}