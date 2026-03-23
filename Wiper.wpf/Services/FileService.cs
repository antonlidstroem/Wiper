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
                    .Where(d =>
                    {
                        var name = Path.GetFileName(d).ToLower();
                        return targetFolders.Contains(name);
                    })
                    .Select(d =>
                    {
                        long size = GetDirectorySize(d);
                        return new ProjectFolder
                        {
                            Name = Path.GetFileName(d),
                            FullPath = d,
                            SizeInBytes = size,
                            SizeDisplay = FormatSize(size)
                        };
                    })
                    .ToList();
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

        private string FormatSize(long bytes)
        {
            string[] Suffix = { "B", "KB", "MB", "GB", "TB" };
            int i;
            double dblSByte = bytes;
            for (i = 0; i < Suffix.Length && bytes >= 1024; i++, bytes /= 1024) dblSByte = bytes / 1024.0;
            return $"{dblSByte:0.##} {Suffix[i]}";
        }

        public async Task DeleteFoldersAsync(IEnumerable<ProjectFolder> folders, Action<string> logger, bool isDryRun)
        {
            // Notera 'async' framför lambdan här nere!
            await Task.Run(async () =>
            {
                foreach (var folder in folders)
                {
                    if (isDryRun)
                    {
                        logger($"[DRY RUN] Skulle ha raderat: {folder.FullPath}");
                        continue;
                    }

                    try
                    {
                        if (Directory.Exists(folder.FullPath))
                        {
                            logger($"Försöker radera: {folder.Name}...");

                            int attempts = 0;
                            bool success = false;

                            while (attempts < 3 && !success)
                            {
                                try
                                {
                                    Directory.Delete(folder.FullPath, true);
                                    logger($"KLART: {folder.Name} raderad.");
                                    success = true;
                                }
                                catch (IOException) when (attempts < 2)
                                {
                                    attempts++;
                                    logger($"Väntar på låst fil i {folder.Name} (försök {attempts})...");
                                    await Task.Delay(500); // Nu fungerar await här
                                }
                                catch (Exception ex)
                                {
                                    logger($"FEL: Kunde inte radera {folder.Name}: {ex.Message}");
                                    break; // Avbryt retry vid andra typer av fel
                                }
                            }

                            if (!success && attempts >= 2)
                            {
                                logger($"MISSLYC_KADES: {folder.Name} är fortfarande låst efter flera försök.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger($"Systemfel vid hantering av {folder.Name}: {ex.Message}");
                    }
                }
            });
        }
    }
}