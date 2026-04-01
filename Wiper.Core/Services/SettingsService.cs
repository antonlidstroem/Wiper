using System.Text.Json;
using Wiper.Core.Interfaces;
using Wiper.Core.Models;

namespace Wiper.Core.Services;

public class SettingsService : ISettingsService
{
    private readonly string _configPath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public WiperSettings Settings { get; private set; } = new();

    public SettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _configPath = Path.Combine(appData, "Wiper", "wiper.config.json");
    }

    /// <summary>Används i tester eller för att ange anpassad sökväg.</summary>
    public SettingsService(string configPath)
    {
        _configPath = configPath;
    }

    public async Task LoadAsync()
    {
        if (!File.Exists(_configPath))
        {
            Settings = new WiperSettings();
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_configPath);
            Settings = JsonSerializer.Deserialize<WiperSettings>(json, JsonOptions) ?? new WiperSettings();
        }
        catch
        {
            // Korrupt konfiguration — återgå till standardvärden
            Settings = new WiperSettings();
        }
    }

    public async Task SaveAsync()
    {
        var dir = Path.GetDirectoryName(_configPath)!;
        Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(Settings, JsonOptions);
        await File.WriteAllTextAsync(_configPath, json);
    }

    public void AddGhostProcess(string processName)
    {
        if (!Settings.GhostProcesses.Contains(processName, StringComparer.OrdinalIgnoreCase))
            Settings.GhostProcesses.Add(processName);
    }

    public void RemoveGhostProcess(string processName) =>
        Settings.GhostProcesses.RemoveAll(p => p.Equals(processName, StringComparison.OrdinalIgnoreCase));

    public void AddTargetFolder(string folderName)
    {
        if (!Settings.TargetFolders.Contains(folderName, StringComparer.OrdinalIgnoreCase))
            Settings.TargetFolders.Add(folderName);
    }

    public void RemoveTargetFolder(string folderName) =>
        Settings.TargetFolders.RemoveAll(f => f.Equals(folderName, StringComparison.OrdinalIgnoreCase));
}
