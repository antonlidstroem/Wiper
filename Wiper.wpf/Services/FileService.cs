using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
                        // Kolla om mappen finns i vår valda lista (t.ex. bin, obj, .vs)
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

        // Uppdaterad metod i FileService.cs
        public async Task DeleteFoldersAsync(IEnumerable<ProjectFolder> folders, Action<string> logger, bool isDryRun)
        {
            await Task.Run(() =>
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
                            logger($"Raderar: {folder.FullPath}");
                            Directory.Delete(folder.FullPath, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger($"Kunde inte radera {folder.Name}: {ex.Message}");
                    }
                }
            });
        }
    }
}
