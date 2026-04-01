namespace Wiper.Core.Models;

public class WiperSettings
{
    public string LastSolutionPath { get; set; } = string.Empty;
    public List<string> TargetFolders { get; set; } = ["bin", "obj"];
    public List<string> GhostProcesses { get; set; } = ["VBCSCompiler", "MSBuild", "msbuild", "vbc"];
    public bool DarkMode { get; set; } = false;
    public bool DefaultDryRun { get; set; } = true;
    public bool AutoRebuildAfterClean { get; set; } = true;
}
