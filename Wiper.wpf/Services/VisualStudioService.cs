using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace Wiper.wpf.Services
{
    public class VisualStudioService
    {
        [DllImport("ole32.dll")]
        private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);

        [DllImport("ole32.dll")]
        private static extern int CreateBindCtx(int reserved, out IBindCtx ppbc);

        public async Task<bool> CleanAndCloseSolutionAsync(string solutionPath, Action<string> logger)
        {
            DTE2? dte = GetDTE(solutionPath);

            if (dte == null)
            {
                logger("Ingen aktiv Visual Studio-instans hittades för denna solution. Fortsätter utan VS-automation.");
                return true;
            }

            return await Task.Run(() =>
            {
                try
                {
                    logger("Ansluten till Visual Studio. Sparar filer...");
                    dte.ExecuteCommand("File.SaveAll");

                    logger("Kör Solution Clean...");
                    dte.Solution.SolutionBuild.Clean(true);

                    logger("Stänger Visual Studio...");
                    Process vsProcess = Process.GetProcessById(dte.Id);
                    dte.Quit();

                    // Vänta på att processen dör
                    vsProcess.WaitForExit(10000);
                    return true;
                }
                catch (Exception ex)
                {
                    logger($"Fel vid VS-automation: {ex.Message}");
                    return false;
                }
            });
        }

        public void RestartSolution(string solutionPath)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = solutionPath,
                UseShellExecute = true
            });
        }

        private DTE2? GetDTE(string solutionPath)
        {
            GetRunningObjectTable(0, out var rot);
            rot.EnumRunning(out var enumMoniker);

            IMoniker[] moniker = new IMoniker[1];
            IntPtr fetched = IntPtr.Zero;

            while (enumMoniker.Next(1, moniker, fetched) == 0)
            {
                CreateBindCtx(0, out var bindCtx);
                moniker[0].GetDisplayName(bindCtx, null, out var displayName);

                if (displayName.StartsWith("!VisualStudio.DTE"))
                {
                    rot.GetObject(moniker[0], out var comObject);
                    var dte = (DTE2)comObject;

                    if (string.Equals(dte.Solution.FullName, solutionPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return dte;
                    }
                }
            }
            return null;
        }
    }
