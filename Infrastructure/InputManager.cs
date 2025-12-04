namespace NovelScraper.Infrastructure;

public static class InputManager
{
    public static bool IsItYes(string? question = null)
    {
        if (!string.IsNullOrWhiteSpace(question))
            Console.Write($"{question} (y/n): ");
        else
            Console.Write("Confirm (y/n): ");

        while (true)
        {
            string? input = Console.ReadLine()?.Trim().ToLower();

            switch (input)
            {
                case "y":
                case "yes":
                    return true;

                case "n":
                case "no":
                    return false;

                default:
                    Console.Write("Invalid input. Please enter 'y' or 'n': ");
                    break;
            }
        }
    }
}