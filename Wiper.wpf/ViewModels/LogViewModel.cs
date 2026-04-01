using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Wiper.WPF.ViewModels;

public partial class LogViewModel : ObservableObject
{
    public ObservableCollection<string> Logs { get; } = [];

    public void Log(string msg)
    {
        Application.Current.Dispatcher.Invoke(() =>
            Logs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {msg}"));
    }

    [RelayCommand]
    public void CopyLogs()
    {
        if (Logs.Count == 0) return;
        var full = string.Join(Environment.NewLine, Logs.Reverse());
        Clipboard.SetText(full);
        Log("System: Loggen kopierad till urklipp.");
    }

    [RelayCommand]
    public void ClearLogs() => Logs.Clear();
}
