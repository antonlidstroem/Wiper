using System.Windows;
using Wiper.Core.Interfaces;

namespace Wiper.WPF.Services;

public class WpfDialogService : IDialogService
{
    public bool Confirm(string message, string title) =>
        MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning)
        == MessageBoxResult.Yes;
}
