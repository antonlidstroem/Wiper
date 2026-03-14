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
        public async Task<List<ProjectFolder>> ScanFoldersAsync(string solutionPath)
        {
            return await Task.Run(() =>
            {
                var root = Path.GetDirectoryName(solutionPath);
                if (string.IsNullOrEmpty(root)) return new List<ProjectFolder>();

                var targets = new[] { "bin", "obj" };
                return Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories)
                    .Where(d => targets.Contains(Path.GetFileName(d).ToLower()))
                    .Select(d => new ProjectFolder { Name = Path.GetFileName(d), FullPath = d })
                    .ToList();
            });
        }

        public async Task DeleteFoldersAsync(IEnumerable<ProjectFolder> folders, Action<string> logger)
        {
            await Task.Run(() =>
            {
                foreach (var folder in folders)
                {
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
