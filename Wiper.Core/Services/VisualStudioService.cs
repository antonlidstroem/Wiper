using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using EnvDTE80;
using Wiper.Core.Interfaces;
using Process = System.Diagnostics.Process;

namespace Wiper.Core.Services;

public class VisualStudioService
{
    private readonly ISettingsService _settings;

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("ole32.dll")]
    private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);

    [DllImport("ole32.dll")]
    private static extern int CreateBindCtx(int reserved, out IBindCtx ppbc);

    public VisualStudioService(ISettingsService settings)
    {
        _settings = settings;
    }

    /// <summary>Returnerar true om en VS-instans med given solution är öppen.</summary>
    public bool IsSolutionOpen(string solutionPath) =>
        GetDTE(solutionPath) != null;

    public async Task<bool> SaveCleanAndCloseAsync(
        string solutionPath,
        Action<string> logger,
        CancellationToken cancellationToken = default)
    {
        // GetDTE must run on the STA (UI) thread — call before any Task.Run
        var dte = GetDTE(solutionPath);
        if (dte == null)
        {
            logger("VS: Ingen öppen instans hittades — hoppar över Save & Close.");
            return true;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // DTE commands must execute on the calling STA thread
            logger("VS: Sparar alla filer...");
            dte.ExecuteCommand("File.SaveAll");

            cancellationToken.ThrowIfCancellationRequested();

            logger("VS: Kör Clean Solution...");
            dte.Solution.SolutionBuild.Clean(true);

            uint pid = 0;
            GetWindowThreadProcessId(new IntPtr(dte.MainWindow.HWnd), out pid);

            logger("VS: Stänger Visual Studio...");
            dte.Quit();

            // Wait for process exit on a background thread so we don't block the UI
            if (pid > 0)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        var p = Process.GetProcessById((int)pid);
                        p.WaitForExit(10_000);
                    }
                    catch { /* Process already exited */ }
                }, cancellationToken);
            }

            KillGhostProcesses(logger);
            return true;
        }
        catch (OperationCanceledException)
        {
            logger("VS: Åtgärd avbruten.");
            return false;
        }
        catch (Exception ex)
        {
            logger($"ERROR VS: {ex.Message}");
            return false;
        }
    }

    public async Task RestartAndRebuildAsync(
        string solutionPath,
        Action<string> logger,
        CancellationToken cancellationToken = default)
    {
        logger("VS: Startar om Visual Studio...");
        Process.Start(new ProcessStartInfo { FileName = solutionPath, UseShellExecute = true });

        DTE2? dte = null;
        int attempts = 0;

        while (dte == null && attempts < 15 && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(3000, cancellationToken);
            dte = GetDTE(solutionPath);
            attempts++;
            logger($"VS: Väntar på att solution laddas... (försök {attempts}/15)");
        }

        if (dte == null)
        {
            logger("ERROR VS: Timeout — kunde inte ansluta till Visual Studio.");
            return;
        }

        if (cancellationToken.IsCancellationRequested) return;

        if (_settings.Settings.AutoRebuildAfterClean)
        {
            try
            {
                logger("VS: Triggar Rebuild Solution...");
                dte.ExecuteCommand("Build.RebuildSolution");
                logger("VS: Rebuild påbörjad!");
            }
            catch (Exception ex)
            {
                logger($"ERROR VS vid Rebuild: {ex.Message}");
            }
        }
        else
        {
            logger("VS: AutoRebuild inaktiverat — hoppar över rebuild.");
        }
    }

    private void KillGhostProcesses(Action<string> logger)
    {
        foreach (var name in _settings.Settings.GhostProcesses)
        {
            foreach (var p in Process.GetProcessesByName(name))
            {
                try
                {
                    p.Kill();
                    logger($"System: Tvingade avslut av '{name}' (frigjorde fillås).");
                }
                catch { /* Processen hann avsluta av sig själv */ }
            }
        }
    }

    private DTE2? GetDTE(string solutionPath)
    {
        try
        {
            GetRunningObjectTable(0, out var rot);
            rot.EnumRunning(out var enumMoniker);
            var moniker = new IMoniker[1];

            while (enumMoniker.Next(1, moniker, IntPtr.Zero) == 0)
            {
                CreateBindCtx(0, out var bindCtx);
                moniker[0].GetDisplayName(bindCtx, null, out var name);

                if (!name.StartsWith("!VisualStudio.DTE")) continue;

                rot.GetObject(moniker[0], out var obj);
                if (obj is DTE2 dte &&
                    dte.Solution?.FullName?.Equals(solutionPath, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return dte;
                }
            }
        }
        catch { /* VS startar upp eller är temporärt otillgänglig */ }

        return null;
    }
}
