using System.Collections.ObjectModel;
using System.IO; // <--- VIKTIGT: Löser CS0103 'File'
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Wiper.wpf.Services;
using Wiper.wpf.Models; // <--- VIKTIGT: Löser CS0234/CS0246

namespace Wiper.wpf.ViewModels;

public partial class MainViewModel : ObservableObject, IRecipient<FolderSelectionChangedMessage>
{
    private readonly FileService _fileService = new();
    private readonly VisualStudioService _vsService = new();

    public LogViewModel LogVM { get; } = new();

    [ObservableProperty] private string _solutionPath = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _status = "Redo";
    [ObservableProperty] private bool _isDryRun = true;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(TotalSizeDisplay))]
    private long _totalSizeInBytes;

    public string TotalSizeDisplay => ByteSizeFormatter.FormatSize(TotalSizeInBytes);
    public ObservableCollection<ProjectFolder> Folders { get; } = [];
    public ObservableCollection<FolderOption> FolderTypeOptions { get; } = [
        new ("bin", true), new ("obj", true), new (".vs", false), new ("TestResults", false)
    ];

    public MainViewModel()
    {
        WeakReferenceMessenger.Default.Register(this);
    }

    public void Receive(FolderSelectionChangedMessage message) => UpdateTotalSize();

    [RelayCommand]
    private async Task ScanAsync()
    {
        if (!File.Exists(SolutionPath)) return;
        IsBusy = true;
        Folders.Clear();

        // Fixat för att undvika konverteringsfel till List<string>
        var targets = FolderTypeOptions
            .Where(o => o.IsChecked)
            .Select(o => o.Name.ToLower())
            .ToList();

        var result = await _fileService.ScanFoldersAsync(SolutionPath, targets);

        foreach (var f in result) Folders.Add(f);

        UpdateTotalSize();
        Status = $"Hittade {Folders.Count} mappar.";
        IsBusy = false;
    }

    [RelayCommand]
    private async Task CleanAsync()
    {
        var selected = Folders.Where(f => f.IsSelected).ToList();
        if (selected.Count == 0 || (!IsDryRun && !ConfirmAction())) return;

        IsBusy = true;
        try
        {
            if (!IsDryRun)
            {
                if (!await _vsService.SaveCleanAndCloseAsync(SolutionPath, LogVM.Log)) return;
            }

            await _fileService.DeleteFoldersAsync(selected, LogVM.Log, IsDryRun);

            if (!IsDryRun)
            {
                await _vsService.RestartAndRebuildAsync(SolutionPath, LogVM.Log);
                Status = "Klar!";
            }
        }
        finally { IsBusy = false; }
    }

    private void UpdateTotalSize() =>
        TotalSizeInBytes = Folders.Where(f => f.IsSelected).Sum(f => f.SizeInBytes);

    private bool ConfirmAction() =>
        MessageBox.Show("Kör rensningscykel?", "Bekräfta", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
}