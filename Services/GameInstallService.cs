using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameLauncherWPF.Models;

namespace GameLauncherWPF.Services;

public sealed class GameInstallService
{
    private readonly GitHubReleaseService _releaseService;

    public GameInstallService(GitHubReleaseService releaseService)
    {
        _releaseService = releaseService;
    }

    public static string? FindExecutablePath()
    {
        if (!Directory.Exists(GamePaths.GameRoot))
        {
            return null;
        }

        return Directory
            .EnumerateFiles(GamePaths.GameRoot, "*.exe", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(path => !string.Equals(Path.GetFileName(path), "UnityCrashHandler64.exe", StringComparison.OrdinalIgnoreCase));
    }

    public static string? GetLocalVersion()
    {
        if (!File.Exists(GamePaths.VersionFile))
        {
            return null;
        }

        return File.ReadAllText(GamePaths.VersionFile).Trim();
    }

    public static bool IsInstalled() => FindExecutablePath() is not null;

    public async Task DownloadAndInstallAsync(
        ReleaseInfo release,
        IProgress<double>? progress,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(GamePaths.GameRoot);
        Directory.CreateDirectory(GamePaths.TempRoot);

        var zipPath = Path.Combine(GamePaths.TempRoot, "SubarashiiGame-Windows.zip");
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        var downloadProgress = new Progress<double>(p => progress?.Report(p * 0.8));
        await _releaseService.DownloadAsync(release.DownloadUrl, zipPath, downloadProgress, cancellationToken);

        CleanupGameFolder(new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Path.GetFileName(GamePaths.VersionFile)
        });

        await ExtractZipAsync(zipPath, new Progress<double>(p => progress?.Report(80 + (p * 0.2))), cancellationToken);

        File.WriteAllText(GamePaths.VersionFile, release.Version);

        if (Directory.Exists(GamePaths.TempRoot))
        {
            Directory.Delete(GamePaths.TempRoot, recursive: true);
        }

        progress?.Report(100);
    }

    private static void CleanupGameFolder(HashSet<string> excludeNames)
    {
        foreach (var filePath in Directory.EnumerateFiles(GamePaths.GameRoot))
        {
            var fileName = Path.GetFileName(filePath);
            if (excludeNames.Contains(fileName))
            {
                continue;
            }

            File.Delete(filePath);
        }

        foreach (var dirPath in Directory.EnumerateDirectories(GamePaths.GameRoot))
        {
            var dirName = Path.GetFileName(dirPath);
            if (excludeNames.Contains(dirName))
            {
                continue;
            }

            Directory.Delete(dirPath, recursive: true);
        }
    }

    private static async Task ExtractZipAsync(string zipPath, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            using var archive = ZipFile.OpenRead(zipPath);
            var entries = archive.Entries.Where(entry => !string.IsNullOrEmpty(entry.Name)).ToList();
            var count = entries.Count;

            if (count == 0)
            {
                progress?.Report(100);
                return;
            }

            var extracted = 0;
            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var destinationPath = Path.Combine(GamePaths.GameRoot, entry.FullName);
                var destinationDir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }

                if (string.IsNullOrEmpty(entry.Name))
                {
                    continue;
                }

                entry.ExtractToFile(destinationPath, overwrite: true);
                extracted++;
                progress?.Report(extracted * 100d / count);
            }
        }, cancellationToken);
    }
}
