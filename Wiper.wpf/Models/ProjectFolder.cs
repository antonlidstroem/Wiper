using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

public partial class ProjectFolder : ObservableObject
{
    public string Name { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public string FullPath { get; init; } = string.Empty;
    public long SizeInBytes { get; set; }
    public string SizeDisplay { get; set; } = string.Empty;

    [ObservableProperty] private bool _isSelected = true;

  
    partial void OnIsSelectedChanged(bool value)
    {
        WeakReferenceMessenger.Default.Send(new FolderSelectionChangedMessage());
    }
}


public record FolderSelectionChangedMessage();