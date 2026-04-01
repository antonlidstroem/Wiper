using CommunityToolkit.Mvvm.ComponentModel;

namespace Wiper.Core.Models;

public partial class FolderOption(string name, bool isChecked) : ObservableObject
{
    public string Name { get; } = name;
    [ObservableProperty] private bool _isChecked = isChecked;
}

public record FolderSelectionChangedMessage();
