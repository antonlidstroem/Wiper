using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using Wiper.wpf.Models;
using Wiper.wpf.Services;

namespace Wiper.wpf.ViewModels;

public partial class FolderListViewModel : ObservableObject, IRecipient<FolderSelectionChangedMessage>
{
    public ObservableCollection<ProjectFolder> Folders { get; } = [];

    [ObservableProperty, NotifyPropertyChangedFor(nameof(TotalSizeDisplay))]
    private long _totalSizeInBytes;

    public string TotalSizeDisplay => ByteSizeFormatter.FormatSize(TotalSizeInBytes);

    public FolderListViewModel()
    {
        WeakReferenceMessenger.Default.Register(this);
    }

    public void Receive(FolderSelectionChangedMessage message) => UpdateTotalSize();

    public void UpdateTotalSize() =>
        TotalSizeInBytes = Folders.Where(f => f.IsSelected).Sum(f => f.SizeInBytes);

    public void Refresh(IEnumerable<ProjectFolder> newFolders)
    {
        Folders.Clear();
        foreach (var f in newFolders) Folders.Add(f);
        UpdateTotalSize();
    }
}