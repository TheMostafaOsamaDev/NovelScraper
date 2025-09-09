namespace NovelScraper.Domain.Entities;

public static class BookFonts
{
    private static string FontsBasePath { set; get; }
    private static Dictionary<string, Font> FontsDict = new();
    private static Font ChosenFont { set; get; }


    static BookFonts()
    {
        AssignFontsBasePath();
        LoadAllFonts();
        SelectFont();
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


            FontsDict.Add(folderName, font);
        }

        Console.WriteLine(FontsDict["Alexandria"].ClassName);
        Console.WriteLine(FontsDict["Alexandria"].FontPath);
    }

    public static void SelectFont()
    {
        Console.WriteLine("Please Select a font from the list:");
        var counter = 1;
        foreach (var key in FontsDict.Keys)
        {
            Console.WriteLine($"({counter++}) - {key}");
        }

        Font selectedFont = null;

        Console.Write("I choose: ");
        var option = int.Parse(Console.ReadLine());

        counter = 1;

        Font chosenFont = null;

        foreach (var key in FontsDict.Keys)
        {
            if (counter++ == option)
            {
                Console.WriteLine($"You choose {key}");
                chosenFont = FontsDict[key];
                break;
            }
        }

        if (chosenFont == null)
        {
            throw new Exception("No font selected");
        }

        ChosenFont = chosenFont;
    }

    public static Font GetFont()
    {
        ChosenFont.FontStream = new FileStream(ChosenFont.FontPath, FileMode.Open);
        ChosenFont.FinalizeFont();
        return ChosenFont;
    }

    public class Font
    {
        public string ClassName { get; }
        public string FontPath { get; }
        public string ResourceName { set; get; }

        public FileStream FontStream { set; get; }
        public string StyleContent { set; get; }

        public Font(string className, string fontPath)
        {
            ClassName = className;
            FontPath = Path.Combine(FontsBasePath, fontPath);
        }


        public void FinalizeFont()
        {
            if (string.IsNullOrEmpty(FontPath) || string.IsNullOrEmpty(ClassName))
            {
                throw new ArgumentNullException("All arguments must be given a value");
            }

            FontStream = new FileStream(FontPath, FileMode.Open);

            ResourceName = Path.GetFileName(FontPath);

            StyleContent = $@"
                       <style>
                            @font-face {{
                            font-family: '{ClassName}';
                            src: url('{ResourceName}') format('truetype');
                            font-weight: normal;
                            font-style: normal;
                            font-display: swap;
                        }}

                        body {{
                            font-family: {ClassName} !important;
                            direction: rtl;
                            text-align: right;
                        }}

                        * {{
                            font-family: {ClassName} !important;
                        }}

                        .rtl-content {{
                            direction: rtl;
                            text-align: right;
                            font-family: {ClassName};
                        }}

                        h1, h2 {{
                            text-align: center;
                        }}
                       </style>
            ";
        }
    }
}