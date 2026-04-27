using System.Net.Http;
using System.Text.Json;
using GameLauncherWPF.Models;

namespace GameLauncherWPF.Services;

/// <summary>
/// Fetches release metadata from GitHub for the SubarashiiGame repository,
/// equivalent to the <c>getinfo</c> / <c>fetch_latest_release</c> functions in dl.py.
/// </summary>
public class GitHubReleaseService : IDisposable
{
    private const string ReleasesUrl =
        "https://api.github.com/repos/jeong-jimin-github/Unity-Game/releases/latest";

    /// <summary>Asset filename to download on Windows.</summary>
    public const string WindowsAssetName = "Windows-Build.zip";

    private readonly HttpClient _httpClient;

    public GitHubReleaseService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "GameLauncherWPF/1.0");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
    }

    /// <summary>
    /// Queries the GitHub Releases API and returns the latest release information.
    /// Returns <c>null</c> if the request fails or the Windows asset is not found
    /// (equivalent to Python returning <c>None</c> on API error / rate-limit).
    /// </summary>
    public async Task<ReleaseInfo?> GetLatestReleaseAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(ReleasesUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseRelease(json);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Parses a GitHub release JSON payload and returns a <see cref="ReleaseInfo"/>.</summary>
    public static ReleaseInfo? ParseRelease(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var version = root.TryGetProperty("name", out var nameProp)
                ? nameProp.GetString() ?? string.Empty
                : string.Empty;

            var description = root.TryGetProperty("body", out var bodyProp)
                ? bodyProp.GetString() ?? string.Empty
                : string.Empty;

            if (!root.TryGetProperty("assets", out var assets))
                return null;

            foreach (var asset in assets.EnumerateArray())
            {
                if (!asset.TryGetProperty("name", out var assetNameProp))
                    continue;

                if (assetNameProp.GetString() != WindowsAssetName)
                    continue;

                if (!asset.TryGetProperty("browser_download_url", out var urlProp))
                    continue;

                var downloadUrl = urlProp.GetString() ?? string.Empty;
                return new ReleaseInfo
                {
                    Version = version,
                    DownloadUrl = downloadUrl,
                    Description = description
                };
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public void Dispose() => _httpClient.Dispose();
}
