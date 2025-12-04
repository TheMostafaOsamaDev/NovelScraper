using System.Text.Json;
using NovelScraper.Domain.Interfaces;

namespace NovelScraper.Application.UserSettingsUseCases;

public class ReadUserSettingsUseCase
{

    public void Execute(string settingsPath)
    {
        var settings = JsonSerializer.Deserialize<ISettings>(settingsPath);
    }
}