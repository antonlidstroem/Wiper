using Wiper.Core.Interfaces;
using Wiper.Core.Services;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("=== WIPER CLI ===");

var settingsService = new SettingsService();
await settingsService.LoadAsync();

// ── Hantera argument ─────────────────────────────────────────────────────────

if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
{
    PrintHelp();
    return;
}

// config-subkommando
if (args[0] == "config")
{
    await HandleConfigCommand(args[1..], settingsService);
    return;
}

// ── Wipe-körning ─────────────────────────────────────────────────────────────

string slnPath = args[0];
bool isDryRun  = !args.Contains("--force");
var  filters   = GetFiltersFromArgs(args, settingsService);

if (!File.Exists(slnPath))
{
    WriteError($"Hittar inte filen: '{slnPath}'");
    return;
}

var fileService = new FileService();
var vsService   = new VisualStudioService(settingsService);
var cts         = new CancellationTokenSource();

// Avbryt med Ctrl+C
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    WriteWarning("Avbryter...");
};

Action<string> logger = msg =>
{
    if      (msg.StartsWith("ERROR"))    Console.ForegroundColor = ConsoleColor.Red;
    else if (msg.StartsWith("DONE"))     Console.ForegroundColor = ConsoleColor.Green;
    else if (msg.Contains("[DRY RUN]")) Console.ForegroundColor = ConsoleColor.Yellow;
    else if (msg.StartsWith("["))        Console.ForegroundColor = ConsoleColor.Cyan;

    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {msg}");
    Console.ResetColor();
};

try
{
    // 1. SCAN ─────────────────────────────────────────────────────────────────
    logger("[SCAN] Skannar lösning...");
    var folders = await fileService.ScanFoldersAsync(slnPath, filters, cts.Token);

    if (!folders.Any())
    {
        logger("Inga mappar matchade filtren.");
        return;
    }

    long totalSize = folders.Sum(f => f.SizeInBytes);
    logger($"[SCAN] Hittade {folders.Count} mappar. Total storlek: {ByteSizeFormatter.FormatSize(totalSize)}");

    if (isDryRun)
    {
        logger("[DRY RUN] Simulering — använd --force för att radera på riktigt.");
        await fileService.DeleteFoldersAsync(folders, logger, isDryRun: true, cts.Token);
        logger("Simulering klar.");
        return;
    }

    // 2. LOCK CHECK ────────────────────────────────────────────────────────────
    logger("[LOCK CHECK] Kontrollerar fillås...");
    // LockService är förberedd — ghost-processer hanteras i VS-steget

    // Bekräftelse
    Console.Write("VARNING: Filer raderas permanent. Stäng VS och radera? (j/n): ");
    if (Console.ReadKey().Key != ConsoleKey.J)
    {
        Console.WriteLine();
        logger("Avbrutet av användaren.");
        return;
    }
    Console.WriteLine();

    // 3. CLEAN ─────────────────────────────────────────────────────────────────
    bool vsWasOpen = vsService.IsSolutionOpen(slnPath);

    if (vsWasOpen)
    {
        logger("[CLEAN] Visual Studio är öppen — sparar och stänger...");
        if (!await vsService.SaveCleanAndCloseAsync(slnPath, logger, cts.Token))
        {
            logger("ERROR: Kunde inte stänga Visual Studio. Avbryter.");
            return;
        }
    }
    else
    {
        logger("[CLEAN] Visual Studio var inte öppen — hoppar över Save & Close.");
    }

    await fileService.DeleteFoldersAsync(folders, logger, isDryRun: false, cts.Token);

    // 4. RESTART ───────────────────────────────────────────────────────────────
    if (vsWasOpen)
    {
        logger("[RESTART] Startar om Visual Studio...");
        await vsService.RestartAndRebuildAsync(slnPath, logger, cts.Token);
    }

    // Spara senast använd sökväg
    settingsService.Settings.LastSolutionPath = slnPath;
    await settingsService.SaveAsync();

    logger("Allt klart!");
}
catch (OperationCanceledException)
{
    WriteWarning("Körningen avbröts.");
}
catch (Exception ex)
{
    WriteError($"Oväntat fel: {ex.Message}");
}

// ── Hjälpmetoder ─────────────────────────────────────────────────────────────

static void PrintHelp()
{
    Console.WriteLine("""

Användning:
  wiper <sökväg.sln> [alternativ]
  wiper config <kommando>

Alternativ för wipe-körning:
  --force              Utför raderingen (annars körs simulering)
  --all                Inkluderar även .vs och TestResults
  --filter <namn>      Lägg till extra mapp att radera (repeterbart)
  -h, --help           Visa denna hjälp

Config-kommandon:
  wiper config --list
  wiper config --add-ghost <processnamn>
  wiper config --remove-ghost <processnamn>
  wiper config --add-folder <mappnamn>
  wiper config --remove-folder <mappnamn>
  wiper config --set-rebuild true|false
  wiper config --set-dark true|false

Exempel:
  wiper MyApp.sln --force
  wiper MyApp.sln --force --all
  wiper config --add-ghost node.exe
  wiper config --add-folder node_modules
""");
}

static List<string> GetFiltersFromArgs(string[] args, ISettingsService settings)
{
    // Starta från sparade inställningar
    var filters = new List<string>(settings.Settings.TargetFolders);

    if (args.Contains("--all"))
        foreach (var extra in new[] { ".vs", "testresults", "packages" })
            if (!filters.Contains(extra, StringComparer.OrdinalIgnoreCase))
                filters.Add(extra);

    // --filter <namn>
    for (int i = 0; i < args.Length - 1; i++)
        if (args[i] == "--filter")
            filters.Add(args[i + 1]);

    return filters;
}

static async Task HandleConfigCommand(string[] args, ISettingsService settings)
{
    if (args.Length == 0 || args.Contains("--list"))
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== Wiper Konfiguration ===");
        Console.ResetColor();
        Console.WriteLine($"  Ghost-processer : {string.Join(", ", settings.Settings.GhostProcesses)}");
        Console.WriteLine($"  Målmappar       : {string.Join(", ", settings.Settings.TargetFolders)}");
        Console.WriteLine($"  Auto-rebuild    : {settings.Settings.AutoRebuildAfterClean}");
        Console.WriteLine($"  Dark mode       : {settings.Settings.DarkMode}");
        Console.WriteLine($"  Dry-run default : {settings.Settings.DefaultDryRun}");
        return;
    }

    bool changed = false;

    for (int i = 0; i < args.Length - 1; i++)
    {
        switch (args[i])
        {
            case "--add-ghost":
                settings.AddGhostProcess(args[i + 1]);
                WriteSuccess($"Ghost-process '{args[i + 1]}' tillagd.");
                changed = true; break;

            case "--remove-ghost":
                settings.RemoveGhostProcess(args[i + 1]);
                WriteSuccess($"Ghost-process '{args[i + 1]}' borttagen.");
                changed = true; break;

            case "--add-folder":
                settings.AddTargetFolder(args[i + 1]);
                WriteSuccess($"Målmapp '{args[i + 1]}' tillagd.");
                changed = true; break;

            case "--remove-folder":
                settings.RemoveTargetFolder(args[i + 1]);
                WriteSuccess($"Målmapp '{args[i + 1]}' borttagen.");
                changed = true; break;

            case "--set-rebuild":
                settings.Settings.AutoRebuildAfterClean = bool.Parse(args[i + 1]);
                WriteSuccess($"AutoRebuild satt till {settings.Settings.AutoRebuildAfterClean}.");
                changed = true; break;

            case "--set-dark":
                settings.Settings.DarkMode = bool.Parse(args[i + 1]);
                WriteSuccess($"DarkMode satt till {settings.Settings.DarkMode}.");
                changed = true; break;
        }
    }

    if (changed) await settings.SaveAsync();
}

static void WriteError(string msg)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ERROR: {msg}");
    Console.ResetColor();
}

static void WriteWarning(string msg)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {msg}");
    Console.ResetColor();
}

static void WriteSuccess(string msg)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {msg}");
    Console.ResetColor();
}
