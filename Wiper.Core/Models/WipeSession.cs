namespace Wiper.Core.Models;

public class WipeSession
{
    public string SolutionPath { get; init; } = string.Empty;
    public List<ProjectFolder> Folders { get; init; } = [];
    public bool VisualStudioWasOpen { get; init; }
    public CancellationToken Token { get; init; }
    public PipelineState CurrentState { get; set; } = PipelineState.Idle;
}

public enum PipelineState
{
    Idle,
    Scanning,
    LockCheck,
    Cleaning,
    Restarting,
    Done,
    Cancelled,
    Error
}
