using System.Text.Json;
using NovelScraper.Domain.Entities.Settings;

namespace NovelScraper.Application.UserSettingsUseCases;

public class FindOrCreateUserSettingsUseCase
{
    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true
    };

    public UserSettings Execute(string settingsPath)
    {
        // Ensure directory exists
        string? dir = Path.GetDirectoryName(settingsPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        // If file doesn't exist => create default settings file
        if (!File.Exists(settingsPath))
        {
            var defaultSettings = new UserSettings();

            string json = JsonSerializer.Serialize(defaultSettings, _options);
            File.WriteAllText(settingsPath, json);

            return defaultSettings;
        }

        // Load existing file
        string content = File.ReadAllText(settingsPath);

        // Deserialize (never deserialize into interface!)
        var settings = JsonSerializer.Deserialize<UserSettings>(content);

        return settings ?? new UserSettings(); // fallback
    }
}