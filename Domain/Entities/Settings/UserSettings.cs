using System.IO;
using NovelScraper.Domain.Interfaces;

namespace NovelScraper.Domain.Entities.Settings;

public class UserSettings : ISettings
{
    public static readonly string SettingsPath =
        Path.Combine(AppContext.BaseDirectory, "static", "settings.json");

    private static readonly string DefaultNovelsPath =
        Path.Combine(AppContext.BaseDirectory, "Static", "Novels");

    // Needed for JSON deserialization
    public UserSettings() { }

    // Optional: manual creation with custom path
    public UserSettings(string? path)
    {
        NovelsPath = string.IsNullOrWhiteSpace(path)
            ? DefaultNovelsPath
            : path;
    }

    public string NovelsPath { get; set; } = DefaultNovelsPath;

    public string ChangeDefaultNovelDirectory()
    {
        while (true)
        {
            Console.WriteLine("Do you want to use the default directory or choose a new one?");
            Console.WriteLine("1. Use Default");
            Console.WriteLine($"   Default: {DefaultNovelsPath}");
            Console.WriteLine("2. Change Directory");
            Console.Write("Enter choice (1 or 2): ");

            string? input = Console.ReadLine();

            if (input == "1")
            {
                NovelsPath = DefaultNovelsPath;
                return DefaultNovelsPath;
            }
            
            if (input == "2")
            {
                Console.Write("Enter new directory path: ");
                string? newPath = Console.ReadLine();

                Console.WriteLine($"Chosen Path {newPath}");

                if (string.IsNullOrWhiteSpace(newPath))
                {
                    Console.WriteLine("❌ Path cannot be empty.");
                    continue;
                }

                if (!Directory.Exists(newPath))
                {
                    Console.WriteLine("❌ Directory does not exist. Create it? (y/n): ");
                    string? create = Console.ReadLine();

                    if (create?.ToLower() == "y")
                    {
                        try
                        {
                            Directory.CreateDirectory(newPath);
                            Console.WriteLine("✔ Directory created.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Could not create directory: {ex.Message}");
                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }

                NovelsPath = newPath;
                return newPath;
            }
        }
    }
}
