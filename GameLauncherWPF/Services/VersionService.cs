using System.IO;

namespace GameLauncherWPF.Services;

/// <summary>
/// Stores and retrieves the installed game version using a plain-text file,
/// equivalent to the SQLite db used in the Python launcher (db.py).
/// </summary>
public class VersionService
{
    private readonly string _versionFilePath;

    public VersionService(string gameFolder)
    {
        _versionFilePath = Path.Combine(gameFolder, "version.txt");
    }

    /// <summary>Returns the installed version string, or <c>null</c> if not set.</summary>
    public string? GetInstalledVersion()
    {
        if (!File.Exists(_versionFilePath))
            return null;

        try
        {
            return File.ReadAllText(_versionFilePath).Trim();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Persists the given version string to disk.</summary>
    public void SetInstalledVersion(string version)
    {
        File.WriteAllText(_versionFilePath, version);
    }
}
