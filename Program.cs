using Microsoft.Extensions.DependencyInjection;
using NovelScraper.Application.FileSystem;
using NovelScraper.Domain.Entities;
using NovelScraper.Infrastructure.BrowserService;
using NovelScraper.Infrastructure.Generators;
using NovelScraper.Infrastructure.Interfaces;
using NovelScraper.Infrastructure.Websites;


Console.Write("Please enter the novel URL: ");
var novelUrl = Console.ReadLine();

Console.Write("Please enter the output directory: ");
var outputDirectory = Console.ReadLine();

Console.Write("Please enter the novel title: ");
var novelTitle = Console.ReadLine();


Console.Write("Please give the novel author name: ");
var authorName = Console.ReadLine();

Console.Write("Starting volume: ");
int startingVolume;
var canBeParsed = int.TryParse(Console.ReadLine(), out startingVolume);

Console.Write("end volume: ");
int endVolume;

canBeParsed = int.TryParse(Console.ReadLine(), out endVolume);

Console.Write("Do you want separated volumes [yes/no] (no is default): ");
bool isSeparated = Console.ReadLine() == "yes";


if (novelUrl == null || outputDirectory == null || novelTitle == null || authorName == null)
{
    Console.WriteLine("Please provide all the required inputs.");
    return;
}

var font = BookFonts.GetFont();


var services = new ServiceCollection();

services.AddSingleton<IBrowserInfrastructure, PlaywrightBrowserService>();

services.AddTransient<KolNovel>(serviceProvider =>
{
    var browserService = serviceProvider.GetRequiredService<IBrowserInfrastructure>();
    return new KolNovel(novelUrl!, browserService);
});


var serviceProvider = services.BuildServiceProvider();
var kolNovel = serviceProvider.GetRequiredService<KolNovel>();

var savingPath = CreateNovelDirectoryUseCase.Execute(outputDirectory, novelTitle);

var configuration = new Configuration(savingPath, startingVolume, endVolume, font);

var volumes = await kolNovel.StartScrapingAsync(configuration);


var epubGenerator =
    new QuickEpubGenerator(savingPath, font);

epubGenerator.InsertVolumes(volumes.ToList());

if (isSeparated)
{
    epubGenerator.GenerateSeparatedEpub(authorName);
}
else
    epubGenerator.GenerateEpub(novelTitle, authorName);