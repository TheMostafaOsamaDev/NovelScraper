using System.Text.Json;
using NovelScraper.Domain.Entities.Settings;

namespace NovelScraper.Application.UserSettingsUseCases;

public class SaveUserSettingsUseCase
{
    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true
    };

    public void Execute(string settingsPath, UserSettings newUserSettings)
    {

        if (string.IsNullOrWhiteSpace(settingsPath))
            throw new ArgumentException("Settings path cannot be null or empty.", nameof(settingsPath));

        Console.WriteLine($"~~~~~ After know {newUserSettings.NovelsPath}");

        try
        {
            // Serialize object
            string json = JsonSerializer.Serialize(newUserSettings, _options);

            // Write to file
            File.WriteAllText(settingsPath, json);

            Console.WriteLine("Saved Successfully.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}