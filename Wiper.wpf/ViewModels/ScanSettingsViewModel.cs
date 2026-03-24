using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using Wiper.Core.Models;

namespace Wiper.wpf.ViewModels;

public partial class ScanSettingsViewModel : ObservableObject
{
    [ObservableProperty] private string _solutionPath = string.Empty;

    public ObservableCollection<FolderOption> FolderTypeOptions { get; } = [
        new ("bin", true), new ("obj", true), new (".vs", false), new ("TestResults", false)
    ];

    public List<string> GetSelectedFilters() =>
        FolderTypeOptions.Where(o => o.IsChecked).Select(o => o.Name.ToLower()).ToList();
}