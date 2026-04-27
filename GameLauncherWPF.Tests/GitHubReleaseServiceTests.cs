using GameLauncherWPF.Models;
using GameLauncherWPF.Services;
using Xunit;

namespace GameLauncherWPF.Tests;

/// <summary>
/// Unit tests for <see cref="GitHubReleaseService.ParseRelease"/> –
/// equivalent to testing the JSON parsing in dl.py's fetch_latest_release().
/// </summary>
public class GitHubReleaseServiceTests
{
    // Minimal valid release JSON with a Windows-Build.zip asset
    private const string ValidReleaseJson = """
        {
          "name": "v1.5.0",
          "body": "Release notes here",
          "assets": [
            {
              "name": "Windows-Build.zip",
              "browser_download_url": "https://example.com/Windows-Build.zip"
            }
          ]
        }
        """;

    [Fact]
    public void ParseRelease_Returns_CorrectVersion()
    {
        var info = GitHubReleaseService.ParseRelease(ValidReleaseJson);
        Assert.NotNull(info);
        Assert.Equal("v1.5.0", info!.Version);
    }

    [Fact]
    public void ParseRelease_Returns_CorrectDownloadUrl()
    {
        var info = GitHubReleaseService.ParseRelease(ValidReleaseJson);
        Assert.NotNull(info);
        Assert.Equal("https://example.com/Windows-Build.zip", info!.DownloadUrl);
    }

    [Fact]
    public void ParseRelease_Returns_CorrectDescription()
    {
        var info = GitHubReleaseService.ParseRelease(ValidReleaseJson);
        Assert.NotNull(info);
        Assert.Equal("Release notes here", info!.Description);
    }

    [Fact]
    public void ParseRelease_ReturnsNull_WhenWindowsAssetMissing()
    {
        var json = """
            {
              "name": "v1.0.0",
              "body": "",
              "assets": [
                {
                  "name": "MacOS-Build.zip",
                  "browser_download_url": "https://example.com/MacOS-Build.zip"
                }
              ]
            }
            """;

        var info = GitHubReleaseService.ParseRelease(json);
        Assert.Null(info);
    }

    [Fact]
    public void ParseRelease_ReturnsNull_OnInvalidJson()
    {
        var info = GitHubReleaseService.ParseRelease("not valid json {{{");
        Assert.Null(info);
    }

    [Fact]
    public void ParseRelease_ReturnsNull_WhenAssetsKeyMissing()
    {
        var json = """{ "name": "v1.0.0", "body": "" }""";
        var info = GitHubReleaseService.ParseRelease(json);
        Assert.Null(info);
    }

    [Fact]
    public void ParseRelease_HandlesMultipleAssets_PicksWindowsBuild()
    {
        var json = """
            {
              "name": "v2.0.0",
              "body": "",
              "assets": [
                {
                  "name": "MacOS-Build.zip",
                  "browser_download_url": "https://example.com/mac.zip"
                },
                {
                  "name": "Windows-Build.zip",
                  "browser_download_url": "https://example.com/win.zip"
                }
              ]
            }
            """;

        var info = GitHubReleaseService.ParseRelease(json);
        Assert.NotNull(info);
        Assert.Equal("https://example.com/win.zip", info!.DownloadUrl);
        Assert.Equal("v2.0.0", info.Version);
    }
}
