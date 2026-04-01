using System.Windows;
using Wiper.Core.Services;

namespace Wiper.WPF;

public partial class App : Application
{
    public static SettingsService SettingsService { get; } = new();

    private async void OnStartup(object sender, StartupEventArgs e)
    {
        await SettingsService.LoadAsync();
        ApplyTheme(SettingsService.Settings.DarkMode);
        new MainWindow().Show();
    }

    /// <summary>
    /// Swaps the active theme ResourceDictionary without touching other merged dictionaries.
    /// Replaces the first entry whose Source ends with "Theme.xaml"; appends if none found yet.
    /// </summary>
    public static void ApplyTheme(bool dark)
    {
        var uri = new Uri(
            dark ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml",
            UriKind.Relative);

        var dicts = Current.Resources.MergedDictionaries;

        for (int i = 0; i < dicts.Count; i++)
        {
            var src = dicts[i].Source?.OriginalString ?? string.Empty;
            if (src.EndsWith("Theme.xaml", StringComparison.OrdinalIgnoreCase))
            {
                dicts[i] = new ResourceDictionary { Source = uri };
                return;
            }
        }

        // First call — theme dict not yet present, add it
        dicts.Add(new ResourceDictionary { Source = uri });
    }
}
