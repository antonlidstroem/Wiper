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

    public ObservableCollection<ProjectFolder> Folders { get; } = [];
    public ObservableCollection<string> Logs { get; } = [];

    [RelayCommand]
    private async Task ScanAsync()
    {
        if (!File.Exists(SolutionPath)) return;
        IsBusy = true;
        Folders.Clear();
        var result = await _fileService.ScanFoldersAsync(SolutionPath);
        foreach (var f in result) Folders.Add(f);
        Status = $"Hittade {Folders.Count} mappar.";
        IsBusy = false;
    }

    [RelayCommand]
    private async Task CleanAsync()
    {
        var selected = Folders.Where(f => f.IsSelected).ToList();
        if (!selected.Any()) return;

        if (MessageBox.Show("Vill du rensa och starta om VS?", "Wiper", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;

        IsBusy = true;
        await _vsService.CleanAndCloseAsync(SolutionPath, Log);
        await _fileService.DeleteFoldersAsync(selected, Log);
        _vsService.Restart(SolutionPath);
        IsBusy = false;
        Log("Klar!");
    }

    private void Log(string msg) =>
        App.Current.Dispatcher.Invoke(() => Logs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {msg}"));
}