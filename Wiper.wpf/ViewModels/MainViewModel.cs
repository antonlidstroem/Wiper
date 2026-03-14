using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wiper.wpf.ViewModels
{
   

    public partial class MainViewModel : ObservableObject
    {
        private readonly VisualStudioService _vsService = new();

        [ObservableProperty] private string _solutionPath = string.Empty;
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _statusMessage = "Redo";

        public ObservableCollection<ProjectFolder> Folders { get; } = [];
        public ObservableCollection<string> Logs { get; } = [];

        [RelayCommand]
        private async Task ScanAsync()
        {
            if (!File.Exists(SolutionPath) || !SolutionPath.EndsWith(".sln"))
            {
                MessageBox.Show("Vänligen ange en giltig sökväg till en .sln-fil.");
                return;
            }

            IsBusy = true;
            Folders.Clear();
            Log("Startar skanning...");

            await Task.Run(() =>
            {
                var root = Path.GetDirectoryName(SolutionPath);
                if (root == null) return;

                var targets = new[] { "bin", "obj" };
                var found = Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories)
                    .Where(d => targets.Contains(Path.GetFileName(d).ToLower()))
                    .Select(d => new ProjectFolder { Name = Path.GetFileName(d), FullPath = d });

                App.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var folder in found) Folders.Add(folder);
                });
            });

            Log($"Hittade {Folders.Count} mappar.");
            IsBusy = false;
        }

        [RelayCommand]
        private async Task CleanAsync()
        {
            var selectedFolders = Folders.Where(f => f.IsSelected).ToList();
            if (!selectedFolders.Any()) return;

            var result = MessageBox.Show($"Är du säker på att du vill radera {selectedFolders.Count} mappar?",
                "Bekräfta radering", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            IsBusy = true;

            // 1. VS Automation
            await _vsService.CleanAndCloseSolutionAsync(SolutionPath, Log);

            // 2. Fysisk radering
            await Task.Run(() =>
            {
                foreach (var folder in selectedFolders)
                {
                    try
                    {
                        if (Directory.Exists(folder.FullPath))
                        {
                            Log($"Raderar: {folder.FullPath}");
                            Directory.Delete(folder.FullPath, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"Kunde inte radera {folder.Name}: {ex.Message}");
                    }
                }
            });

            // 3. Starta om
            Log("Startar om Visual Studio...");
            _vsService.RestartSolution(SolutionPath);

            IsBusy = false;
            Log("Klar!");
        }

        private void Log(string message)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                Logs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
                StatusMessage = message;
            });
        }
    }
}
