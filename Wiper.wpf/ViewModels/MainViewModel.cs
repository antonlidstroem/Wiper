using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wiper.wpf.Models;
using Wiper.wpf.Services;

namespace Wiper.wpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly FileService _fileService = new();
    private readonly VisualStudioService _vsService = new();

    [ObservableProperty] private string _solutionPath = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _status = "Redo";
    [ObservableProperty] private bool _isDryRun = true;
    [ObservableProperty] private string _totalSizeDisplay = "0 B";

    public ObservableCollection<FolderOption> FolderTypeOptions { get; } = new()
    {
        new FolderOption("bin", true),
        new FolderOption("obj", true),
        new (".vs", false),
        new ("TestResults", false)
    };

    public ObservableCollection<ProjectFolder> Folders { get; } = [];
    public ObservableCollection<string> Logs { get; } = [];

    [RelayCommand]
    private async Task ScanAsync()
    {
        if (!File.Exists(SolutionPath)) return;
        IsBusy = true;
        Folders.Clear();

        var targets = FolderTypeOptions.Where(o => o.IsChecked).Select(o => o.Name.ToLower()).ToList();
        var result = await _fileService.ScanFoldersAsync(SolutionPath, targets);

        foreach (var f in result)
        {
            f.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ProjectFolder.IsSelected)) UpdateTotalSize();
            };
            Folders.Add(f);
        }

        UpdateTotalSize();
        Status = $"Hittade {Folders.Count} mappar.";
        IsBusy = false;
    }

    private void UpdateTotalSize()
    {
        long total = Folders.Where(f => f.IsSelected).Sum(f => f.SizeInBytes);
        TotalSizeDisplay = FormatSize(total);
    }

    private string FormatSize(long bytes)
    {
        string[] Suffix = { "B", "KB", "MB", "GB", "TB" };
        int i;
        double dblSByte = bytes;
        for (i = 0; i < Suffix.Length && bytes >= 1024; i++, bytes /= 1024) dblSByte = bytes / 1024.0;
        return $"{dblSByte:0.##} {Suffix[i]}";
    }

    [RelayCommand]
    private async Task CleanAsync()
    {
        var selected = Folders.Where(f => f.IsSelected).ToList();
        if (selected.Count == 0) return;

        if (!IsDryRun)
        {
            var result = MessageBox.Show("Vill du rensa på riktigt? Visual Studio kommer att stängas.",
                                       "Bekräfta rensning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
        }

        IsBusy = true;

        try
        {
            if (!IsDryRun)
            {
                // VIKTIGT: Vi väntar på bekräftelse att VS faktiskt stängdes
                bool isReadyToDelete = await _vsService.CleanAndCloseAsync(SolutionPath, Log);

                if (!isReadyToDelete)
                {
                    Log("AVBRUTET: Kunde inte verifiera att VS stängdes. Radering ej utförd.");
                    Status = "Rensning misslyckades.";
                    return; // Gå ur metoden här!
                }
            }
            else
            {
                Log("--- STARTAR SIMULERING ---");
            }

            // Kör radering först när vi vet att VS är borta
            await _fileService.DeleteFoldersAsync(selected, Log, IsDryRun);

            if (!IsDryRun)
            {
                Log("Rensning klar. Startar om Visual Studio...");
                _vsService.Restart(SolutionPath);
                Status = "Klart!";
            }
            else
            {
                Log("--- SIMULERING KLAR ---");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Log(string msg) =>
        App.Current.Dispatcher.Invoke(() => Logs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {msg}"));
}

// Flyttad hit ner, men fortfarande i samma namespace
public partial class FolderOption(string name, bool isChecked) : ObservableObject
{
    public string Name { get; } = name;
    [ObservableProperty] private bool _isChecked = isChecked;
}