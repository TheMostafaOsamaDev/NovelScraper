using Microsoft.Extensions.DependencyInjection;
using NovelScraper.Application.CoverUseCases;
using NovelScraper.Application.FileSystem;
using NovelScraper.Application.FileSystemUseCases;
using NovelScraper.Application.Services;
using NovelScraper.Application.UserSettingsUseCases;
using NovelScraper.Domain.Entities;
using NovelScraper.Domain.Entities.Novel;
using NovelScraper.Domain.Enums.Entities.Website;
using NovelScraper.Domain.Interfaces;
using NovelScraper.Helpers;
using NovelScraper.Infrastructure;
using NovelScraper.Infrastructure.BrowserService;
using NovelScraper.Infrastructure.Generators;
using NovelScraper.Infrastructure.Interfaces;
using NovelScraper.Infrastructure.Services;
using NovelScraper.Infrastructure.Websites;

var servicesCollection = new ServiceCollection();

// Configuration Services
servicesCollection.AddSingleton<FindOrCreateUserSettingsUseCase>();
servicesCollection.AddSingleton<DeleteUserSettingsUseCase>();
servicesCollection.AddSingleton<SaveUserSettingsUseCase>();
servicesCollection.AddSingleton<UserSettingsManager>();

// Cover Update Services
servicesCollection.AddSingleton<IEpubCoverService, EpubCoverService>();
servicesCollection.AddSingleton<NovelFolderScanner>();
servicesCollection.AddSingleton<NovelCoversUpdater>();
servicesCollection.AddSingleton<NovelsCoverUpdateOrchestrator>();

// Browser
servicesCollection.AddSingleton<IBrowserInfrastructure, PlaywrightBrowserService>();

// Websites
servicesCollection.AddTransient<KolNovel>();


var provider = servicesCollection.BuildServiceProvider();

var userSettingsManager = provider.GetRequiredService<UserSettingsManager>();

var settings = userSettingsManager.LoadSettings();

// Start the program
while (true)
{
    Console.WriteLine("\n=== NovelScraper Menu ===");
    Console.WriteLine("1. Download Novels");
    Console.WriteLine("2. Update Downloaded Novels Covers");
    Console.WriteLine("3. User Settings");
    Console.WriteLine("4. Exit");
    Console.Write("\nSelect an option: ");
    
    var menuChoice = Console.ReadLine();
    
    if (menuChoice == "4")
    {
        Console.WriteLine("Goodbye!");
        break;
    }
    
    if (menuChoice == "3")
    {
        Logger.LogSeparator();
        Console.WriteLine("User Settings:");
        Console.WriteLine("1. Change Novels Saving Path");
        Console.WriteLine("2. Back to Main Menu");
        Console.Write("Select: ");
        
        var settingsChoice = Console.ReadLine();
        if (settingsChoice == "1")
        {
            userSettingsManager.ChangeNovelsPath();
        }
        continue;
    }
    
    if (menuChoice == "2")
    {
        // Update Downloaded Novels Covers
        Console.WriteLine($"\n=== Update Downloaded Novels Covers ===");
        Console.WriteLine($"Novels Directory: {settings.NovelsPath}");
        
        if (!InputManager.IsItYes("Is this the correct folder?"))
        {
            Console.WriteLine("Please update the novels path in User Settings first.");
            continue;
        }
        
        Console.WriteLine("\n⚠️  IMPORTANT: Folder Structure Required ⚠️");
        Console.WriteLine("Each novel must follow this structure:");
        Console.WriteLine("  Novel Title/");
        Console.WriteLine("    ├── Volume1.epub");
        Console.WriteLine("    ├── Volume2.epub");
        Console.WriteLine("    └── covers/");
        Console.WriteLine("        ├── 1.png (or .jpg, .jpeg, .gif, .bmp, .webp)");
        Console.WriteLine("        └── 2.png (or .jpg, .jpeg, .gif, .bmp, .webp)");
        Console.WriteLine("\nSupported image formats: PNG, JPG, JPEG, GIF, BMP, WEBP");
        Console.WriteLine();
        
        if (!InputManager.IsItYes("Does your folder follow this structure?"))
        {
            Console.WriteLine("Please organize your folders according to this structure first.");
            continue;
        }
        
        var orchestrator = provider.GetRequiredService<NovelsCoverUpdateOrchestrator>();
        var reports = orchestrator.Execute(settings);
        
        Console.WriteLine("\n=== Summary ===");
        var reportsList = reports.ToList();
        var totalUpdated = reportsList.Sum(r => r.UpdatedVolumes.Count);
        var totalMissing = reportsList.Sum(r => r.MissingCoverVolumes.Count);
        
        Console.WriteLine($"✓ Total volumes updated: {totalUpdated}");
        Console.WriteLine($"✗ Total volumes missing covers: {totalMissing}");
        
        foreach (var report in reportsList)
        {
            Console.WriteLine($"\n=== {report.NovelName} ===");
            
            if (report.CoversFolderMissing)
            {
                Console.WriteLine("⚠️  'covers' folder is missing");
                continue;
            }
            
            if (!report.HasVolumes)
            {
                Console.WriteLine("⚠️  No EPUB volumes detected");
                continue;
            }
            
            if (report.UpdatedVolumes.Count > 0)
            {
                Console.WriteLine("Updated:");
                foreach (var vol in report.UpdatedVolumes)
                    Console.WriteLine($"  ✓ {vol}");
            }
            
            if (report.MissingCoverVolumes.Count > 0)
            {
                Console.WriteLine("Missing Covers:");
                foreach (var vol in report.MissingCoverVolumes)
                    Console.WriteLine($"  ✗ {vol}");
            }
        }
        
        continue;
    }
    
    if (menuChoice != "1")
    {
        Console.WriteLine("Invalid option. Please try again.");
        continue;
    }
    
    // Download Novels (Original workflow)
    // Configuration settings
    if (InputManager.IsItYes("Do you want to change anything in the settings?"))
    {
        Logger.LogSeparator();
    
        Console.WriteLine("You can change: ");
        Console.WriteLine("1. Novels Saving Path.");
        Console.WriteLine("2. None");
    
        Console.Write("What do you want to change exactly? ");
        var isParsedSuccessfully = int.TryParse(Console.ReadLine(), out int option);
    
        if (isParsedSuccessfully)
        {
            switch (option)
            {
                case 1:
                    userSettingsManager.ChangeNovelsPath();
                    break;
                default:
                    Console.WriteLine("Okay restarting again.");
                    break;
            }
        }
        else
        {
            Console.WriteLine("\nSorry wrong input, please try again.");
            continue;
        }
    }

    var outputDirectory = settings.NovelsPath;

    // Novel inputs
    Console.Write("Please enter the novel URL: ");
    var novelUrl = Console.ReadLine();
    
    if (string.IsNullOrWhiteSpace(novelUrl) || !UrlHelper.IsUrl(novelUrl))
    {
        Logger.LogError($"Sorry novel url can't be null or empty and must be a valid url.");
        continue;
    }

    Console.Write("Please enter the novel title: ");
    var novelTitle = Console.ReadLine();

    Console.Write("Please give the novel author name: ");
    var authorName = Console.ReadLine();
    
    // Ensure authorName is never null or empty - provide default value
    if (string.IsNullOrWhiteSpace(authorName))
    {
        authorName = "Unknown";
    }

    Console.Write("Starting volume: ");
    int? startingVolume = null;
    var canBeParsed = int.TryParse(Console.ReadLine(), out int startVol);
    if (canBeParsed) startingVolume = startVol;

    Console.Write("End volume: ");
    int? endVolume = null;
    canBeParsed = int.TryParse(Console.ReadLine(), out int endVol);
    if (canBeParsed) endVolume = endVol;

    Console.Write("Do you want separated volumes [yes/no] (no is default): ");
    bool isSeparated = Console.ReadLine()?.Trim().ToLower() == "yes";

    if (string.IsNullOrWhiteSpace(novelTitle))
    {
        Console.WriteLine("Please provide all the required inputs.");
        continue;
    }

    // Font selection
    var availableFonts = BookFonts.GetAvailableFonts();
    var defaultFont = BookFonts.GetDefaultFont();
    BookFonts.Font selectedFont = defaultFont;

    while (true)
    {
        Console.WriteLine("\nFont options:");
        Console.WriteLine($"Press Enter to use default: {defaultFont.ClassName}");
        for (int i = 0; i < availableFonts.Count; i++)
            Console.WriteLine($"{i + 1}. {availableFonts[i].ClassName}");
        

        Console.Write($"Choose font number [1-{availableFonts.Count}] or Enter for {defaultFont.ClassName}: ");
        var fontChoice = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(fontChoice))
        {
            Console.WriteLine($"Using default font: {defaultFont.ClassName}\n");
            break;
        }

        if (int.TryParse(fontChoice, out int fontIndex) && fontIndex >= 1 && fontIndex <= availableFonts.Count)
        {
            selectedFont = availableFonts[fontIndex - 1];
            Console.WriteLine($"Using selected font: {selectedFont.ClassName}\n");
            break;
        }

        Console.WriteLine("Invalid choice, please try again.\n");
    }

    // Prepare website scraper
    Website? website = null;

    if (novelUrl.StartsWith(KolNovel.BaseUrl, StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("It's a Kol Novel URL, using Kol Novel scraper.");

        var browser = provider.GetRequiredService<IBrowserInfrastructure>();
        website = new KolNovel(novelUrl, browser);
    }
    else
    {
        Logger.LogError("No scraper available for this website.");
        continue;
    }

    // Configure scraping
    var savingPath = CreateNovelDirectoryUseCase.Execute(outputDirectory, novelTitle);
    var configuration = new Configuration(novelTitle, savingPath, startingVolume, endVolume, selectedFont);

    // Start scraping
    var volumes = await website.StartScrapingAsync(configuration);

    // Generate EPUB
    var epubGenerator = new QuickEpubGenerator(savingPath, selectedFont);
    epubGenerator.InsertVolumes(volumes.ToList());

    if (isSeparated)
        epubGenerator.GenerateSeparatedEpub(authorName);
    else
        epubGenerator.GenerateEpub(novelTitle, authorName);

    break;
}

return;

//
// Console.Write("Please enter the novel URL: ");
// var novelUrl = Console.ReadLine();
//
// Console.Write("Please enter the output directory: ");
// var outputDirectory = Console.ReadLine();
//
// Console.Write("Please enter the novel title: ");
// var novelTitle = Console.ReadLine();
//
// Console.Write("Please give the novel author name: ");
// var authorName = Console.ReadLine();
//
// Console.Write("Starting volume: ");
// int startingVolume;
// var canBeParsed = int.TryParse(Console.ReadLine(), out startingVolume);
//
// Console.Write("end volume: ");
// int endVolume;
//
// canBeParsed = int.TryParse(Console.ReadLine(), out endVolume);
//
// Console.Write("Do you want separated volumes [yes/no] (no is default): ");
// bool isSeparated = Console.ReadLine() == "yes";
//
//
// if (novelUrl == null || outputDirectory == null || novelTitle == null || authorName == null)
// {
//     Console.WriteLine("Please provide all the required inputs.");
//     return;
// }
//
// var font = BookFonts.GetFont();
//
//
// var services = new ServiceCollection();
//
// services.AddSingleton<IBrowserInfrastructure, PlaywrightBrowserService>();
//
// services.AddTransient<KolNovel>(serviceProvider =>
// {
//     var browserService = serviceProvider.GetRequiredService<IBrowserInfrastructure>();
//     return new KolNovel(novelUrl!, browserService);
// });
//
//
// var serviceProvider = services.BuildServiceProvider();
// var kolNovel = serviceProvider.GetRequiredService<KolNovel>();
//
// var savingPath = CreateNovelDirectoryUseCase.Execute(outputDirectory, novelTitle);
//
// var configuration = new Configuration(savingPath, startingVolume, endVolume, font);
//
// var volumes = await kolNovel.StartScrapingAsync(configuration);
//
//
// var epubGenerator =
//     new QuickEpubGenerator(savingPath, font);
//
// epubGenerator.InsertVolumes(volumes.ToList());
//
// if (isSeparated)
// {
//     epubGenerator.GenerateSeparatedEpub(authorName);
// }
// else
//     epubGenerator.GenerateEpub(novelTitle, authorName);