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
            var result = MessageBox.Show("Sekvens: Save -> Clean -> Close -> Delete -> Restart -> Rebuild.\nVill du köra?",
                                       "Bekräfta total rensning", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result != MessageBoxResult.Yes) return;
        }

        IsBusy = true;
        try
        {
            if (!IsDryRun)
            {
                // STEG 1, 2 & 3: Save, Clean, Close
                bool readyForDelete = await _vsService.SaveCleanAndCloseAsync(SolutionPath, Log);
                if (!readyForDelete)
                {
                    Log("AVBRUTET: VS kunde inte stängas säkert.");
                    return;
                }
            }

            // STEG 4: Delete (Fysisk radering av mappar)
            // Denna körs nu när VS är helt borta och inga filer är låsta
            await _fileService.DeleteFoldersAsync(selected, Log, IsDryRun);

            if (!IsDryRun)
            {
                // STEG 5 & 6: Restart & Rebuild
                await _vsService.RestartAndRebuildAsync(SolutionPath, Log);
                Status = "Fullständig cykel genomförd!";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CopyLogs()
    {
        if (Logs.Count == 0) return;

        // Eftersom du gör Logs.Insert(0, ...) är loggen "bakvänd" (nyast först).
        // Vi vänder på den här så att den kopierade texten blir i kronologisk ordning.
        var fullLog = string.Join(Environment.NewLine, Logs.Reverse());

        Clipboard.SetText(fullLog);
        Log("System: Loggen har kopierats till urklipp.");
    }

    [RelayCommand]
    private void ClearLogs()
    {
        Logs.Clear();
        Status = "Loggen rensad.";
        Log("System: Loggen har nollställts.");
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