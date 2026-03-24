using System.Diagnostics;
using Wiper.Core.Models;
using Wiper.Core.Services;

Console.WriteLine("=== WIPER CLI - Visual Studio Housekeeper ===");

// 1. Hantera argument
if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
{
    PrintHelp();
    return;
}

string slnPath = args[0];
bool isDryRun = !args.Contains("--force"); // Kräver --force för att faktiskt radera
var filters = GetFiltersFromArgs(args);

if (!File.Exists(slnPath))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Fel: Hittade inte filen '{slnPath}'");
    Console.ResetColor();
    return;
}

// 2. Initiera tjänster
var fileService = new FileService();
var vsService = new VisualStudioService();

// Funktion för snygg loggning i konsolen
Action<string> logger = (msg) =>
{
    if (msg.StartsWith("FEL")) Console.ForegroundColor = ConsoleColor.Red;
    else if (msg.StartsWith("KLART")) Console.ForegroundColor = ConsoleColor.Green;
    else if (msg.Contains("[DRY RUN]")) Console.ForegroundColor = ConsoleColor.Yellow;

    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {msg}");
    Console.ResetColor();
};

// 3. Kör processen
logger($"Skannar lösning: {slnPath}");
var folders = await fileService.ScanFoldersAsync(slnPath, filters);

if (!folders.Any())
{
    logger("Inga mappar hittades som matchar filtren.");
    return;
}

long totalSize = folders.Sum(f => f.SizeInBytes);
logger($"Hittade {folders.Count} mappar. Total storlek: {ByteSizeFormatter.FormatSize(totalSize)}");

if (isDryRun)
{
    logger("--- KÖR SIMULERING (Dry Run) ---");
    logger("Använd flaggan --force för att utföra raderingen på riktigt.");
}
else
{
    Console.Write("VARNING: Detta kommer stänga VS och radera filer. Fortsätta? (j/n): ");
    if (Console.ReadKey().Key != ConsoleKey.J) return;
    Console.WriteLine();

    // Stäng VS
    if (!await vsService.SaveCleanAndCloseAsync(slnPath, logger))
    {
        logger("Kunde inte stänga Visual Studio ordentligt. Avbryter.");
        return;
    }
}

// Radera mappar
await fileService.DeleteFoldersAsync(folders, logger, isDryRun);

if (!isDryRun)
{
    // Starta om VS
    await vsService.RestartAndRebuildAsync(slnPath, logger);
}

logger("Allt klart!");

// --- Hjälpmetoder ---

static void PrintHelp()
{
    Console.WriteLine("\nAnvändning:");
    Console.WriteLine("  wiper <sökväg till .sln> [alternativ]");
    Console.WriteLine("\nAlternativ:");
    Console.WriteLine("  --force        Utför raderingen (utan denna körs bara simulering)");
    Console.WriteLine("  --all          Inkluderar även .vs och TestResults");
    Console.WriteLine("  -h, --help     Visa denna hjälp");
}

static List<string> GetFiltersFromArgs(string[] args)
{
    var filters = new List<string> { "bin", "obj" }; // Standard
    if (args.Contains("--all"))
    {
        filters.AddRange(new[] { ".vs", "testresults" });
    }
    return filters;
}