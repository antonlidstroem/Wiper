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

                return Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories)
                    .Where(d =>
                    {
                        var name = Path.GetFileName(d).ToLower();
                        // Kolla att det är bin eller obj OCH att det finns ett .csproj i samma projektmapp
                        var parent = Directory.GetParent(d)?.FullName;
                        bool hasCsproj = parent != null && Directory.EnumerateFiles(parent, "*.csproj").Any();
                        return (name == "bin" || name == "obj") && hasCsproj;
                    })
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
