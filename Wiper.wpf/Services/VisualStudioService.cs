using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using EnvDTE;
using EnvDTE80;
using Process = System.Diagnostics.Process;

namespace Wiper.wpf.Services;

public class VisualStudioService
{
    [DllImport("ole32.dll")]
    private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);

    [DllImport("ole32.dll")]
    private static extern int CreateBindCtx(int reserved, out IBindCtx ppbc);

    public async Task CleanAndCloseAsync(string solutionPath, Action<string> logger)
    {
        DTE2? dte = GetDTE(solutionPath);
        if (dte == null) return;

        await Task.Run(() =>
        {
            try
            {
                logger("VS: Sparar alla filer...");
                dte.ExecuteCommand("File.SaveAll");
                logger("VS: Rensar lösning (Clean)...");
                dte.Solution.SolutionBuild.Clean(true);
                logger("VS: Stänger ner...");
                dte.Quit();
            }
            catch (Exception ex) { logger($"VS Fel: {ex.Message}"); }
        });
    }

    public void Restart(string path) =>
        Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });

    private DTE2? GetDTE(string solutionPath)
    {
        GetRunningObjectTable(0, out var rot);
        rot.EnumRunning(out var enumMoniker);
        IMoniker[] moniker = new IMoniker[1];
        while (enumMoniker.Next(1, moniker, IntPtr.Zero) == 0)
        {
            CreateBindCtx(0, out var bindCtx);
            moniker[0].GetDisplayName(bindCtx, null, out var name);
            if (name.StartsWith("!VisualStudio.DTE"))
            {
                rot.GetObject(moniker[0], out var obj);
                var dte = (DTE2)obj;
                if (string.Equals(dte.Solution.FullName, solutionPath, StringComparison.OrdinalIgnoreCase))
                    return dte;
            }
        }
        return null;
    }
}