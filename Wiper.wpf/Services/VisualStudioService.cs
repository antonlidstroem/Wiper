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

    public async Task<bool> CleanAndCloseAsync(string solutionPath, Action<string> logger)
    {
        DTE2? dte = GetDTE(solutionPath);
        if (dte == null)
        {
            logger("VS: Lösningen hittades inte i någon öppen Visual Studio-instans. Går vidare till radering.");
            return true;
        }

        return await Task.Run(() =>
        {
            try
            {
                logger("VS: Sparar alla filer...");
                dte.ExecuteCommand("File.SaveAll");

                logger("VS: Kör Clean Solution...");
                // true betyder att vi väntar tills clean är klar
                dte.Solution.SolutionBuild.Clean(true);

                logger("VS: Stänger ner...");
                int processId = GetVsProcessId(dte);
                dte.Quit();

                // Vänta på att processen faktiskt dör så att fil-låsen släpps
                if (processId > 0)
                {
                    var vsProcess = Process.GetProcessById(processId);
                    logger("Väntar på att processen avslutas...");
                    vsProcess.WaitForExit(10000); // Max 10 sekunder
                }

                return true;
            }
            catch (Exception ex)
            {
                logger($"VS Fel: {ex.Message}");
                return false;
            }
        });
    }

    // Hjälpmetod för att hitta rätt process
    private int GetVsProcessId(DTE2 dte)
    {
        try { return Process.GetProcessesByName("devenv").FirstOrDefault(p => p.MainWindowTitle.Contains(Path.GetFileNameWithoutExtension(dte.Solution.FullName)))?.Id ?? 0; }
        catch { return 0; }
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