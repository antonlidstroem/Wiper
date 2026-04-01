using Wiper.Core.Models;

namespace Wiper.Core.Services;

public class FileService
{
    public async Task<List<ProjectFolder>> ScanFoldersAsync(
        string solutionPath,
        List<string> targetFolders,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var root = Path.GetDirectoryName(solutionPath);
            if (string.IsNullOrEmpty(root)) return [];

            var results = new List<ProjectFolder>();

            foreach (var dir in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var folderName = Path.GetFileName(dir);
                if (!targetFolders.Contains(folderName, StringComparer.OrdinalIgnoreCase))
                    continue;

                // Hoppa över mappar som redan är undermappar av hittade mappar
                if (results.Any(f => dir.StartsWith(f.FullPath + Path.DirectorySeparatorChar)))
                    continue;

                long size = GetDirectorySize(dir, cancellationToken);
                var parent = Path.GetDirectoryName(dir);
                var projName = parent != null ? Path.GetFileName(parent) : "Root";

                results.Add(new ProjectFolder
                {
                    Name = folderName,
                    ProjectName = projName,
                    FullPath = dir,
                    SizeInBytes = size,
                    SizeDisplay = ByteSizeFormatter.FormatSize(size)
                });
            }

            return results;
        }, cancellationToken);
    }

    private static long GetDirectorySize(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                .TakeWhile(_ => !cancellationToken.IsCancellationRequested)
                .Sum(f =>
                {
                    try { return new FileInfo(f).Length; }
                    catch { return 0L; }
                });
        }
        catch { return 0; }
    }

    public async Task DeleteFoldersAsync(
        IEnumerable<ProjectFolder> folders,
        Action<string> logger,
        bool isDryRun,
        CancellationToken cancellationToken = default)
    {
        await Task.Run(async () =>
        {
            foreach (var folder in folders)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string context = $"{folder.ProjectName} > {folder.Name}";

                if (isDryRun)
                {
                    logger($"[DRY RUN] Skulle raderat: {context} ({folder.SizeDisplay})");
                    continue;
                }

                if (!Directory.Exists(folder.FullPath))
                {
                    logger($"Hoppar över (finns inte längre): {context}");
                    continue;
                }

                logger($"Raderar: {context}...");

                int attempts = 0;
                bool success = false;

                while (attempts < 3 && !success && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        Directory.Delete(folder.FullPath, true);
                        logger($"DONE: {context} raderad ({folder.SizeDisplay} frigjord).");
                        success = true;
                    }
                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                    {
                        attempts++;
                        logger($"Väntar på fillås i {context} (försök {attempts}/3)...");
                        await Task.Delay(1500, cancellationToken);
                    }
                }

                if (!success)
                    logger($"ERROR: Kunde inte radera {context} efter 3 försök.");
            }
        }, cancellationToken);
    }
}
