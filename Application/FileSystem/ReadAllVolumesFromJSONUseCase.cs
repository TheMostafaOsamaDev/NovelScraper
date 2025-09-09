using System.Text.Json;
using NovelScraper.Domain.Entities;

namespace NovelScraper.Application.FileSystem;

public static class ReadAllVolumesFromJsonUseCase
{
    public static List<Volume> Execute(string novelPath)
    {
        var allFolders = Directory.GetDirectories(novelPath).ToList();
        var volumes = new List<Volume>();

        for (int i = 0; i < allFolders.Count; i++)
        {
            var folder = allFolders[i];

            Console.WriteLine();

            var folderName = Path.GetFileName(folder);

            var volume = new Volume(i + 1, folderName, folder);


            var allFiles = Directory.GetFiles(folder, "*.json").ToList();


            for (int j = 0; j < allFiles.Count; j++)
            {
                var file = allFiles[j];

                var jsonString = File.ReadAllText(file);

                var chapter = JsonSerializer.Deserialize<Chapter>(jsonString);

                if (chapter != null)
                    volume.AddChapter(chapter);
            }

            volumes.Add(volume);
        }

        return volumes;
    }
}