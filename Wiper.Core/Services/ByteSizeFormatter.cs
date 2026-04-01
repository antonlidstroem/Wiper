namespace Wiper.Core.Services;

public static class ByteSizeFormatter
{
    private static readonly string[] Suffixes = ["B", "KB", "MB", "GB", "TB"];

    public static string FormatSize(long bytes)
    {
        double value = bytes;
        int i = 0;
        while (value >= 1024 && i < Suffixes.Length - 1)
        {
            i++;
            value /= 1024;
        }
        return string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0:0.##} {1}", value, Suffixes[i]);
    }
}
