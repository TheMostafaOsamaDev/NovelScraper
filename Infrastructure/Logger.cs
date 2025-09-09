namespace NovelScraper.Infrastructure;

public static class Logger
{
    public static void LogVolumeStarted(int volumeId, string volumeTitle)
    {
        SetConsoleColor(ConsoleColor.Cyan);

        LogSeparator();

        Console.WriteLine($"Started Volume {volumeId}: {volumeTitle}");

        ResetConsoleColor();
    }

    public static void LogVolumeCompleted(int volumeId, string volumeTitle)
    {
        SetConsoleColor(ConsoleColor.Green);

        Console.WriteLine($"Completed Volume {volumeId}: {volumeTitle}");

        LogSeparator();

        ResetConsoleColor();
    }

    public static void LogChapterStarted(int chapterId, string chapterTitle)
    {
        SetConsoleColor(ConsoleColor.Yellow);

        Console.WriteLine($"  Started Chapter {chapterId}: {chapterTitle}");

        ResetConsoleColor();
    }

    public static void LogChapterCompleted(int chapterId, string chapterTitle)
    {
        SetConsoleColor(ConsoleColor.Green);

        Console.WriteLine($"  Completed Chapter {chapterId}: {chapterTitle}");

        ResetConsoleColor();
    }

    public static void AllCompleted()
    {
        SetConsoleColor(ConsoleColor.Magenta);

        LogSeparator();

        Console.WriteLine("All volumes and chapters have been processed.");

        LogSeparator();

        ResetConsoleColor();
    }

    private static void LogSeparator()
    {
        Console.WriteLine("------------------------------");
    }

    private static void SetConsoleColor(ConsoleColor color)
    {
        Console.ForegroundColor = color;
    }

    private static void ResetConsoleColor()
    {
        Console.ResetColor();
    }

    public static void LogChapterSkipped(int chapterChapterId, string chapterTitle)
    {
        SetConsoleColor(ConsoleColor.DarkGray);

        Console.WriteLine($"  Skipped Chapter {chapterChapterId}: {chapterTitle} (already exists)");

        ResetConsoleColor();
    }

    public static void LogError(string s)
    {
        SetConsoleColor(ConsoleColor.Red);

        Console.WriteLine(s);

        ResetConsoleColor();
    }
}