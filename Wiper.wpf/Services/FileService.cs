using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Wiper.wpf.Models;


namespace Wiper.wpf.Services
{
    public class FileService
    {
        public async Task<List<ProjectFolder>> ScanFoldersAsync(string solutionPath, List<string> targetFolders)
        {
            return await Task.Run(() =>
            {
                var root = Path.GetDirectoryName(solutionPath);
                if (string.IsNullOrEmpty(root)) return new List<ProjectFolder>();

                return Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories)
                    .Where(d => targetFolders.Contains(Path.GetFileName(d).ToLower()))
                    .Select(d =>
                    {
                        long size = GetDirectorySize(d);
                        // Hämta namnet på mappen ovanför (projektet)
                        var parent = Path.GetDirectoryName(d);
                        var projName = parent != null ? Path.GetFileName(parent) : "Root";

                        return new ProjectFolder
                        {
                            Name = Path.GetFileName(d),
                            ProjectName = projName, // Spara projektnamn
                            FullPath = d,
                            SizeInBytes = size,
                            SizeDisplay = ByteSizeFormatter.FormatSize(size)
                        };
                    }).ToList();
            });
        }

        private long GetDirectorySize(string path)
        {
            try
            {
                return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                                .Sum(f => new FileInfo(f).Length);
            }
            catch { return 0; }
        }

        public async Task DeleteFoldersAsync(IEnumerable<ProjectFolder> folders, Action<string> logger, bool isDryRun)
        {
            await Task.Run(async () =>
            {
                foreach (var folder in folders)
                {
                    string context = $"{folder.ProjectName} > {folder.Name}";
                    if (isDryRun)
                    {
                        logger($"[DRY RUN] Skulle raderat: {context}");
                        continue;
                    }

                    try
                    {
                        if (!Directory.Exists(folder.FullPath)) continue;
                        logger($"Försöker radera: {context}...");

                        int attempts = 0;
                        bool success = false;
                        while (attempts < 3 && !success)
                        {
                            try
                            {
                                Directory.Delete(folder.FullPath, true);
                                logger($"KLART: {context} raderad.");
                                success = true;
                            }
                            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                            {
                                attempts++;
                                logger($"Väntar på {context} (försök {attempts})...");
                                await Task.Delay(1000); // Vänta lite längre vid låsning
                            }
                        }
                    }
                    catch (Exception ex) { logger($"FEL: {context}: {ex.Message}"); }
                }
            });
        }
    }
}