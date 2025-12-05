namespace NovelScraper.Domain.Entities.Novel;

public class Configuration
{
    // string savingDirectory, int? startVolume, int? endVolume
    public Configuration(string novelTitle ,string savingDirectory, int? startVolume, int? endVolume, BookFonts.Font chosenFont)
    {
        NovelTitle = novelTitle;
        SavingDirectory = savingDirectory;
        StartVolume = startVolume;
        EndVolume = endVolume;
        ChosenFont = chosenFont;
    }

    public string NovelTitle { set; get; }
    public string SavingDirectory { set; get; }
    public int? StartVolume { set; get; }
    public int? EndVolume { set; get; }

    public static string JsonCacheDirectory { get; } =
        Path.Combine(AppContext.BaseDirectory, "static", "cache");

    public BookFonts.Font ChosenFont { set; get; }
}