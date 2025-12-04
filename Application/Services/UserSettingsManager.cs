using NovelScraper.Application.UserSettingsUseCases;
using NovelScraper.Domain.Entities.Settings;

namespace NovelScraper.Application.Services;

public class UserSettingsManager
{
    private readonly string _settingsPath = UserSettings.SettingsPath;

    private readonly FindOrCreateUserSettingsUseCase _findOrCreate;

    // TODO: If you will not use _delete remove it later 
    private readonly DeleteUserSettingsUseCase _delete;
    private readonly SaveUserSettingsUseCase _save;
    private UserSettings _settings { set; get; }

    public UserSettingsManager(
        FindOrCreateUserSettingsUseCase findOrCreate,
        DeleteUserSettingsUseCase delete,
        SaveUserSettingsUseCase save
        )
    {
        _findOrCreate = findOrCreate;
        _delete = delete;
        _save = save;
    }


    public UserSettings LoadSettings()
    {
        try
        {
            var settings = _findOrCreate.Execute(_settingsPath);

            var properties = settings.GetType().GetProperties();

            Console.WriteLine("Default Data Loaded \nCurrent Defaults: ");
            foreach (var property in properties)
            {
                string key = property.Name;
                object? value = property.GetValue(settings);

                // Set color for the key
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"{key}: ");

                // Set color for the value
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(value);

                // Reset color for next line
                Console.ResetColor();
            }

            _settings = settings;

            return settings;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }


    public string ChangeNovelsPath()
    {
        var novelsPath = _settings.ChangeDefaultNovelDirectory();

        _settings.NovelsPath = novelsPath;

        Console.WriteLine(_settings.NovelsPath);

        _save.Execute(_settingsPath, _settings);
        return novelsPath;
    }
}