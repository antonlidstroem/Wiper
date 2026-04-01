using System.IO;
using System.Windows;
using Microsoft.Win32;
using Wiper.WPF.ViewModels;

namespace Wiper.WPF;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Drag & Drop
        AllowDrop = true;
        Drop     += OnFileDrop;
        DragOver += OnDragOver;
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnFileDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        var sln = files?.FirstOrDefault(f =>
            f.EndsWith(".sln",  StringComparison.OrdinalIgnoreCase) ||
            f.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase));

        if (sln is null) return;

        var vm = (MainViewModel)DataContext;
        vm.Settings.SolutionPath = sln;

        // Trigga skanning direkt
        if (vm.ScanCommand.CanExecute(null))
            await vm.ScanCommand.ExecuteAsync(null);
    }

    private void BrowseForSolution(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title  = "Välj lösningsfil",
            Filter = "Solution files|*.sln;*.slnx|All files|*.*",
        };

        if (dialog.ShowDialog() == true)
        {
            var vm = (MainViewModel)DataContext;
            vm.Settings.SolutionPath = dialog.FileName;
        }
    }
}
