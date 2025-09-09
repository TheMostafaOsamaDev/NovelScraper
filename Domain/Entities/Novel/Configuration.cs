namespace NovelScraper.Domain.Entities;

public class Configuration
{
    // string savingDirectory, int? startVolume, int? endVolume
    public Configuration(string savingDirectory, int? startVolume, int? endVolume, BookFonts.Font chosenFont)
    {
        SavingDirectory = savingDirectory;
        StartVolume = startVolume;
        EndVolume = endVolume;
        ChosenFont = chosenFont;
    }

    public string SavingDirectory { set; get; }
    public int? StartVolume { set; get; }
    public int? EndVolume { set; get; }
    public BookFonts.Font ChosenFont { set; get; }
    
    
}