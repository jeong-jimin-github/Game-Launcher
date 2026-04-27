using System.IO;
using System.IO.Compression;
using System.Net.Http;

namespace GameLauncherWPF.Services;

/// <summary>
/// Handles downloading the game ZIP and extracting it to the game folder,
/// equivalent to dl.py + fileunzip.py in the Python launcher.
/// Also provides helper methods for finding the game executable (check_for_exe_files).
/// </summary>
public class GameDownloadService : IDisposable
{
    private readonly HttpClient _httpClient;

    public GameDownloadService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "GameLauncherWPF/1.0");
    }

    /// <summary>
    /// Downloads a file from <paramref name="url"/> to <paramref name="destinationPath"/>,
    /// reporting percentage progress (0–100) via <paramref name="progress"/>.
    /// </summary>
    public async Task DownloadAsync(
        string url,
        string destinationPath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? 0;

        var directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        await using var sourceStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var destinationStream = File.Create(destinationPath);

        var buffer = new byte[8192];
        long totalRead = 0;
        int bytesRead;

        while ((bytesRead = await sourceStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await destinationStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalRead += bytesRead;

            if (totalBytes > 0)
                progress?.Report((double)totalRead / totalBytes * 100.0);
        }
    }

    /// <summary>
    /// Extracts <paramref name="zipPath"/> into <paramref name="destinationFolder"/>,
    /// preserving only <c>version.txt</c> (equivalent to Python preserving db.db).
    /// Reports extraction progress (0–100) via <paramref name="progress"/>.
    /// Deletes the ZIP file when done.
    /// </summary>
    public void ExtractZip(
        string zipPath,
        string destinationFolder,
        IProgress<double>? progress = null)
    {
        // Remove existing game files while keeping version.txt
        if (Directory.Exists(destinationFolder))
        {
            foreach (var item in Directory.EnumerateFileSystemEntries(destinationFolder))
            {
                if (Path.GetFileName(item) == "version.txt")
                    continue;

                try
                {
                    if (Directory.Exists(item))
                        Directory.Delete(item, recursive: true);
                    else
                        File.Delete(item);
                }
                catch
                {
                    // Best-effort: skip locked files
                }
            }
        }

        Directory.CreateDirectory(destinationFolder);

        using var archive = ZipFile.OpenRead(zipPath);
        var entries = archive.Entries;
        int total = entries.Count;
        int current = 0;

        foreach (var entry in entries)
        {
            var entryDestPath = Path.GetFullPath(
                Path.Combine(destinationFolder, entry.FullName));

            // Guard against zip-slip attacks
            if (!entryDestPath.StartsWith(
                    Path.GetFullPath(destinationFolder) + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase))
                continue;

            var entryDir = Path.GetDirectoryName(entryDestPath);
            if (!string.IsNullOrEmpty(entryDir))
                Directory.CreateDirectory(entryDir);

            // Skip pure directory entries
            if (!string.IsNullOrEmpty(entry.Name))
                entry.ExtractToFile(entryDestPath, overwrite: true);

            current++;
            progress?.Report((double)current / total * 100.0);
        }

        // Clean up temp ZIP
        try { File.Delete(zipPath); } catch { }
    }

    /// <summary>
    /// Returns the full path of the main game executable inside <paramref name="gameFolder"/>,
    /// skipping UnityCrashHandler64.exe — equivalent to check_for_exe_files() in main.py.
    /// Returns <c>null</c> if no executable is found.
    /// </summary>
    public static string? FindGameExecutable(string gameFolder)
    {
        if (!Directory.Exists(gameFolder))
            return null;

        foreach (var file in Directory.EnumerateFiles(gameFolder, "*.exe"))
        {
            var name = Path.GetFileName(file);
            if (!string.Equals(name, "UnityCrashHandler64.exe",
                    StringComparison.OrdinalIgnoreCase))
                return file;
        }

        return null;
    }

    public void Dispose() => _httpClient.Dispose();
}
