using CommunityToolkit.Mvvm.ComponentModel;

namespace Wiper.wpf.Models;

public partial class ProjectFolder : ObservableObject
{
    public string Name { get; init; } = string.Empty;
    public string FullPath { get; init; } = string.Empty;

    // Vi använder [ObservableProperty] så att vi kan trigga omräkning i ViewModel
    [ObservableProperty] private bool _isSelected = true;

    public long SizeInBytes { get; set; }
    public string SizeDisplay { get; set; } = "0 B";
}