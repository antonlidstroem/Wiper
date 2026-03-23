using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using EnvDTE80;
using Process = System.Diagnostics.Process;

namespace Wiper.wpf.Services
{
    public class VisualStudioService
    {
        // --- P/Invoke för att hantera processer och COM ---
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("ole32.dll")]
        private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);

        [DllImport("ole32.dll")]
        private static extern int CreateBindCtx(int reserved, out IBindCtx ppbc);

        public async Task<bool> CleanAndCloseAsync(string solutionPath, Action<string> logger)
        {
            var dte = GetDTE(solutionPath);
            if (dte == null)
            {
                logger("VS: Ingen öppen instans hittades för denna lösning. Fortsätter...");
                return true;
            }

            return await Task.Run(() =>
            {
                try
                {
                    logger("VS: Sparar alla filer...");
                    dte.ExecuteCommand("File.SaveAll");

                    // Hämta Process ID via fönstret innan vi stänger
                    uint processId = 0;
                    try
                    {
                        GetWindowThreadProcessId((IntPtr)dte.MainWindow.HWnd, out processId);
                    }
                    catch { /* Ignonera om vi inte kan hämta HWND */ }

                    logger("VS: Stänger ned Visual Studio...");
                    dte.Quit();

                    // Vänta på att processen faktiskt dör så att fil-låsen släpps
                    if (processId > 0)
                    {
                        try
                        {
                            var vsProcess = Process.GetProcessById((int)processId);
                            logger($"Väntar på att processen ({processId}) avslutas...");

                            // Vänta max 15 sekunder
                            if (!vsProcess.WaitForExit(15000))
                            {
                                logger("FEL: Visual Studio stängdes inte i tid. Radering avbruten.");
                                return false;
                            }
                        }
                        catch (ArgumentException) { /* Processen hann redan stängas */ }
                    }

                    logger("VS: Stängning bekräftad.");
                    return true;
                }
                catch (Exception ex)
                {
                    logger($"VS Fel: {ex.Message}");
                    return false;
                }
            });
        }

        public void Restart(string path)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
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
                        if (string.Equals(dte.Solution.FullName, solutionPath, StringComparison.OrdinalIgnoreCase))
                            return dte;
                    }
                }
            }
            catch
            {
                return null;
            }
            return null;
        }
    }
}