using CommunityToolkit.Mvvm.ComponentModel;

namespace Wiper.wpf.Models;

public partial class FolderOption(string name, bool isChecked) : ObservableObject
{
    public string Name { get; } = name;
    [ObservableProperty] private bool _isChecked = isChecked;
}

// Meddelandet för Messenger
public record FolderSelectionChangedMessage();