namespace GameLauncherWPF.Models;

/// <summary>
/// Represents metadata for the latest game release fetched from GitHub.
/// </summary>
public class ReleaseInfo
{
    public string Version { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
