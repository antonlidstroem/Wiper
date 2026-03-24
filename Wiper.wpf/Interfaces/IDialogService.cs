using System.Windows;

namespace Wiper.wpf.Interfaces;

public interface IDialogService
{
    bool Confirm(string message, string title);
}

public class WpfDialogService : IDialogService
{
    public bool Confirm(string message, string title) =>
        MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
}