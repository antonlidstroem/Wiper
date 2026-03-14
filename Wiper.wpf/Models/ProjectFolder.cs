namespace Wiper.wpf.Models;

public class ProjectFolder
{
    public string Name { get; init; } = string.Empty;
    public string FullPath { get; init; } = string.Empty;
    public bool IsSelected { get; set; } = true;
}