using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using EnvDTE80;
using Process = System.Diagnostics.Process;

namespace Wiper.wpf.Services
{
    public class VisualStudioService
    {
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("ole32.dll")]
        private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);
        [DllImport("ole32.dll")]
        private static extern int CreateBindCtx(int reserved, out IBindCtx ppbc);

        public async Task<bool> SaveCleanAndCloseAsync(string solutionPath, Action<string> logger)
        {
            var dte = GetDTE(solutionPath);
            if (dte == null)
            {
                logger("VS: Ingen aktiv instans hittad för denna .sln. Fortsätter till radering.");
                return true;
            }

            return await Task.Run(() =>
            {
                try
                {
                    // 1. SAVE
                    logger("VS: Sparar alla filer...");
                    dte.ExecuteCommand("File.SaveAll");

                    // 2. CLEAN
                    logger("VS: Kör Clean Solution...");
                    dte.Solution.SolutionBuild.Clean(true); // true = vänta tills klar

                    // Förbered stängning genom att hämta PID
                    uint processId = 0;
                    GetWindowThreadProcessId((IntPtr)dte.MainWindow.HWnd, out processId);

                    // 3. CLOSE
                    logger("VS: Stänger Visual Studio...");
                    dte.Quit();

                    // Vänta på att processen dör så att fil-låsen släpps inför 'Delete'
                    if (processId > 0)
                    {
                        try
                        {
                            var vsProcess = Process.GetProcessById((int)processId);
                            if (!vsProcess.WaitForExit(15000)) return false;
                        }
                        catch { /* Redan stängd */ }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    logger($"VS Fel under avslutningssekvens: {ex.Message}");
                    return false;
                }
            });
        }

        public async Task RestartAndRebuildAsync(string solutionPath, Action<string> logger)
        {
            // 5. RESTART
            logger("VS: Startar om Visual Studio...");
            Process.Start(new ProcessStartInfo { FileName = solutionPath, UseShellExecute = true });

            DTE2? dte = null;
            int attempts = 0;

            // Vänta på att VS dyker upp i ROT igen
            while (dte == null && attempts < 15)
            {
                await Task.Delay(3000);
                dte = GetDTE(solutionPath);
                attempts++;
                logger($"VS: Väntar på att solution laddas... (försök {attempts})");
            }

            if (dte != null)
            {
                try
                {
                    // 6. REBUILD
                    logger("VS: Triggar Rebuild Solution...");
                    dte.ExecuteCommand("Build.RebuildSolution");
                    logger("VS: Rebuild påbörjad!");
                }
                catch (Exception ex)
                {
                    logger($"VS Fel vid Rebuild: {ex.Message}");
                }
            }
        }

        private DTE2? GetDTE(string solutionPath)
        {
            IRunningObjectTable? rot = null;
            IEnumMoniker? enumMoniker = null;
            try
            {
                GetRunningObjectTable(0, out rot);
                rot.EnumRunning(out enumMoniker);
                IMoniker[] moniker = new IMoniker[1];

                while (enumMoniker.Next(1, moniker, IntPtr.Zero) == 0)
                {
                    CreateBindCtx(0, out var bindCtx);
                    moniker[0].GetDisplayName(bindCtx, null, out var name);

                    if (name.StartsWith("!VisualStudio.DTE"))
                    {
                        rot.GetObject(moniker[0], out var obj);
                        var dte = (DTE2)obj;
                        if (dte.Solution?.FullName?.Equals(solutionPath, StringComparison.OrdinalIgnoreCase) == true)
                        {
                            return dte;
                        }
                    }
                }
            }
            catch { /* Upptagen eller startar */ }
            return null;
        }
    }
}