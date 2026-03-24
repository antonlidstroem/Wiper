using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wiper.Core.Interfaces;
using Wiper.Core.Services;
using Wiper.wpf.Services;
using Wiper.Core.Models; 

namespace Wiper.wpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly FileService _fileService = new();
    private readonly VisualStudioService _vsService = new();
    private readonly IDialogService _dialogService = new WpfDialogService();

    // Child ViewModels
    public ScanSettingsViewModel Settings { get; } = new();
    public FolderListViewModel FolderList { get; } = new();
    public LogViewModel LogVM { get; } = new();

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _status = "Redo";
    [ObservableProperty] private bool _isDryRun = true;

    [RelayCommand]
    private async Task ScanAsync()
    {
        if (!File.Exists(Settings.SolutionPath))
        {
            Status = "Fel: Ogiltig sökväg.";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _fileService.ScanFoldersAsync(Settings.SolutionPath, Settings.GetSelectedFilters());
            FolderList.Refresh(result);
            Status = $"Hittade {FolderList.Folders.Count} mappar.";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task CleanAsync()
    {
        var selected = FolderList.Folders.Where(f => f.IsSelected).ToList();
        if (selected.Count == 0) return;

        if (!IsDryRun && !_dialogService.Confirm("Kör rensningscykel?", "Bekräfta"))
            return;

        IsBusy = true;
        try
        {
            if (!IsDryRun)
            {
                if (!await _vsService.SaveCleanAndCloseAsync(Settings.SolutionPath, LogVM.Log)) return;
            }

            await _fileService.DeleteFoldersAsync(selected, LogVM.Log, IsDryRun);

            if (!IsDryRun)
            {
                await _vsService.RestartAndRebuildAsync(Settings.SolutionPath, LogVM.Log);
                Status = "Klar!";
            }
        }
        finally { IsBusy = false; }
    }
}