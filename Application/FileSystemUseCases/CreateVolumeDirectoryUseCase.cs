using NovelScraper.Helpers;

namespace NovelScraper.Application.FileSystem;

public static class CreateVolumeDirectoryUseCase
{
    public static string Execute(int volumeId, string bookTitle, string baseDirectory)
    {
        // Sanitize book title to create a valid directory name
        var sanitizedBookTitle = PathHelper.SanitizeFileName(bookTitle);
        var volumeDirectoryName = $"{volumeId} - {sanitizedBookTitle}";
        var volumeDirectoryPath = Path.Combine(baseDirectory, volumeDirectoryName);

        // Create the directory if it doesn't exist
        if (!Directory.Exists(volumeDirectoryPath))
        {
            Directory.CreateDirectory(volumeDirectoryPath);
        }

        return volumeDirectoryPath;
    }
}