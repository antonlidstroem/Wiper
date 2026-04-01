using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wiper.Core.Interfaces;
using Wiper.Core.Models;

namespace Wiper.WPF.ViewModels;

public partial class ScanSettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;

    [ObservableProperty] private string _solutionPath = string.Empty;
    [ObservableProperty] private string _newFolderName = string.Empty;
    [ObservableProperty] private string _newGhostProcess = string.Empty;

    public ObservableCollection<FolderOption> FolderTypeOptions { get; } = [];
    public ObservableCollection<string> GhostProcesses { get; } = [];

    public ScanSettingsViewModel(ISettingsService settings)
    {
        _settings = settings;
        SolutionPath = settings.Settings.LastSolutionPath;
        RebuildFolderOptions();
        RebuildGhostProcesses();
    }

    private void RebuildFolderOptions()
    {
        FolderTypeOptions.Clear();
        foreach (var f in _settings.Settings.TargetFolders)
            FolderTypeOptions.Add(new FolderOption(f, true));
    }

    private void RebuildGhostProcesses()
    {
        GhostProcesses.Clear();
        foreach (var g in _settings.Settings.GhostProcesses)
            GhostProcesses.Add(g);
    }

    public List<string> GetSelectedFilters() =>
        FolderTypeOptions.Where(o => o.IsChecked).Select(o => o.Name.ToLower()).ToList();

    [RelayCommand]
    private async Task AddFolderAsync()
    {
        var name = NewFolderName.Trim();
        if (string.IsNullOrEmpty(name)) return;
        _settings.AddTargetFolder(name);
        FolderTypeOptions.Add(new FolderOption(name, true));
        NewFolderName = string.Empty;
        await _settings.SaveAsync();
    }

    [RelayCommand]
    private async Task RemoveFolderAsync(FolderOption option)
    {
        _settings.RemoveTargetFolder(option.Name);
        FolderTypeOptions.Remove(option);
        await _settings.SaveAsync();
    }

    [RelayCommand]
    private async Task AddGhostProcessAsync()
    {
        var name = NewGhostProcess.Trim();
        if (string.IsNullOrEmpty(name)) return;
        _settings.AddGhostProcess(name);
        GhostProcesses.Add(name);
        NewGhostProcess = string.Empty;
        await _settings.SaveAsync();
    }

    [RelayCommand]
    private async Task RemoveGhostProcessAsync(string processName)
    {
        _settings.RemoveGhostProcess(processName);
        GhostProcesses.Remove(processName);
        await _settings.SaveAsync();
    }
}
