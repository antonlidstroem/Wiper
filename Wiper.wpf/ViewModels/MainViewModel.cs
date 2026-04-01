using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wiper.Core.Interfaces;
using Wiper.Core.Models;
using Wiper.Core.Services;
using Wiper.WPF.Services;

namespace Wiper.WPF.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly FileService        _fileService;
    private readonly VisualStudioService _vsService;
    private readonly IDialogService     _dialogService;
    private readonly ISettingsService   _settingsService;

    private CancellationTokenSource? _cts;

    // ── Child ViewModels ──────────────────────────────────────────────────────
    public ScanSettingsViewModel Settings  { get; }
    public FolderListViewModel   FolderList { get; } = new();
    public LogViewModel          LogVM      { get; } = new();

    // ── Observerbart tillstånd ────────────────────────────────────────────────
    [ObservableProperty] private bool   _isBusy;
    [ObservableProperty] private string _status    = "Redo";
    [ObservableProperty] private bool   _isDryRun  = true;
    [ObservableProperty] private bool   _isDarkMode;
    [ObservableProperty] private bool   _isDragOver;

    [ObservableProperty,
     NotifyPropertyChangedFor(nameof(IsScanning)),
     NotifyPropertyChangedFor(nameof(IsLockChecking)),
     NotifyPropertyChangedFor(nameof(IsCleaning)),
     NotifyPropertyChangedFor(nameof(IsRestarting))]
    private PipelineState _pipelineState = PipelineState.Idle;

    // Bekvämlighetsegenskaper för pipeline-indikatorn i XAML
    public bool IsScanning     => PipelineState == PipelineState.Scanning;
    public bool IsLockChecking => PipelineState == PipelineState.LockCheck;
    public bool IsCleaning     => PipelineState == PipelineState.Cleaning;
    public bool IsRestarting   => PipelineState == PipelineState.Restarting;

    public MainViewModel()
    {
        _settingsService = App.SettingsService;
        _fileService     = new FileService();
        _vsService       = new VisualStudioService(_settingsService);
        _dialogService   = new WpfDialogService();

        Settings    = new ScanSettingsViewModel(_settingsService);
        IsDryRun    = _settingsService.Settings.DefaultDryRun;
        IsDarkMode  = _settingsService.Settings.DarkMode;
    }

    // ── Kommandon ─────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ScanAsync()
    {
        if (!File.Exists(Settings.SolutionPath))
        {
            Status = "Fel: Ogiltig sökväg till .sln-fil.";
            return;
        }

        IsBusy        = true;
        PipelineState = PipelineState.Scanning;
        _cts          = new CancellationTokenSource();

        try
        {
            LogVM.Log("[SCAN] Skannar lösning...");
            var filters = Settings.GetSelectedFilters();
            var result  = await _fileService.ScanFoldersAsync(
                Settings.SolutionPath, filters, _cts.Token);

            FolderList.Refresh(result);
            Status        = $"Hittade {FolderList.Folders.Count} mappar · {FolderList.TotalSizeDisplay}";
            PipelineState = PipelineState.Idle;
            LogVM.Log($"[SCAN] Klar — {result.Count} mappar hittade.");

            // Spara senaste sökväg
            _settingsService.Settings.LastSolutionPath = Settings.SolutionPath;
            await _settingsService.SaveAsync();
        }
        catch (OperationCanceledException)
        {
            PipelineState = PipelineState.Cancelled;
            Status        = "Skanning avbruten.";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task CleanAsync()
    {
        var selected = FolderList.Folders.Where(f => f.IsSelected).ToList();
        if (selected.Count == 0)
        {
            Status = "Inga mappar valda.";
            return;
        }

        if (!IsDryRun && !_dialogService.Confirm(
            $"Radera {selected.Count} mappar ({FolderList.TotalSizeDisplay})?\n\nDetta kan inte ångras.",
            "Bekräfta rensning"))
            return;

        IsBusy = true;
        _cts   = new CancellationTokenSource();

        try
        {
            bool vsWasOpen = !IsDryRun && _vsService.IsSolutionOpen(Settings.SolutionPath);

            // 2. LOCK CHECK ───────────────────────────────────────────────────
            PipelineState = PipelineState.LockCheck;
            LogVM.Log("[LOCK CHECK] Kontrollerar fillås...");
            await Task.Delay(300, _cts.Token); // Platshållare tills LockService är implementerad

            // 3. CLEAN ────────────────────────────────────────────────────────
            PipelineState = PipelineState.Cleaning;

            if (!IsDryRun && vsWasOpen)
            {
                LogVM.Log("[CLEAN] Sparar och stänger Visual Studio...");
                if (!await _vsService.SaveCleanAndCloseAsync(
                    Settings.SolutionPath, LogVM.Log, _cts.Token))
                {
                    Status        = "Fel: Kunde inte stänga Visual Studio.";
                    PipelineState = PipelineState.Error;
                    return;
                }
            }
            else if (!IsDryRun && !vsWasOpen)
            {
                LogVM.Log("[CLEAN] Visual Studio var inte öppen — hoppar över Save & Close.");
            }

            await _fileService.DeleteFoldersAsync(selected, LogVM.Log, IsDryRun, _cts.Token);

            // 4. RESTART ──────────────────────────────────────────────────────
            if (!IsDryRun && vsWasOpen)
            {
                PipelineState = PipelineState.Restarting;
                LogVM.Log("[RESTART] Startar om Visual Studio...");
                await _vsService.RestartAndRebuildAsync(Settings.SolutionPath, LogVM.Log, _cts.Token);
            }

            PipelineState = PipelineState.Done;
            Status        = IsDryRun ? "Simulering klar." : "Rensning klar!";
            LogVM.Log($"[DONE] {(IsDryRun ? "Simulering" : "Rensning")} slutförd.");
        }
        catch (OperationCanceledException)
        {
            PipelineState = PipelineState.Cancelled;
            Status        = "Avbruten.";
            LogVM.Log("[AVBRUTEN] Körningen avbröts av användaren.");
        }
        catch (Exception ex)
        {
            PipelineState = PipelineState.Error;
            Status        = $"Fel: {ex.Message}";
            LogVM.Log($"ERROR: {ex.Message}");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void CancelOperation()
    {
        _cts?.Cancel();
        LogVM.Log("Avbryter pågående operation...");
    }

    [RelayCommand]
    private async Task ToggleThemeAsync()
    {
        IsDarkMode = !IsDarkMode;
        App.ApplyTheme(IsDarkMode);
        _settingsService.Settings.DarkMode = IsDarkMode;
        await _settingsService.SaveAsync();
    }
}
