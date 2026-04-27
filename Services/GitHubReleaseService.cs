using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GameLauncherWPF.Models;

namespace GameLauncherWPF.Services;

public sealed class GitHubRateLimitException : Exception
{
    public GitHubRateLimitException(string message) : base(message)
    {
    }
}

public sealed class GitHubReleaseService
{
    private const string ReleaseEndpoint = "https://api.github.com/repos/jeong-jimin-github/Unity-Game/releases/latest";
    private static readonly HttpClient Http = BuildClient();

    private static HttpClient BuildClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("SubarashiiGameLauncher/1.0");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    public async Task<ReleaseInfo> GetLatestReleaseAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ReleaseEndpoint);
        using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Forbidden &&
            response.Headers.TryGetValues("X-RateLimit-Remaining", out var values) &&
            string.Equals("0", System.Linq.Enumerable.FirstOrDefault(values), StringComparison.Ordinal))
        {
            throw new GitHubRateLimitException("요청 할당량이 상한을 초과했습니다. 잠시 후 다시 시도해 주세요.");
        }

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var root = doc.RootElement;
        var version = root.GetProperty("name").GetString() ?? string.Empty;
        var description = root.TryGetProperty("body", out var bodyNode) ? bodyNode.GetString() ?? string.Empty : string.Empty;

        string? downloadUrl = null;
        foreach (var asset in root.GetProperty("assets").EnumerateArray())
        {
            var assetName = asset.GetProperty("name").GetString();
            if (!string.Equals(assetName, "Windows-Build.zip", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            downloadUrl = asset.GetProperty("browser_download_url").GetString();
            break;
        }

        if (string.IsNullOrWhiteSpace(downloadUrl))
        {
            throw new InvalidOperationException("릴리스 자산에서 Windows-Build.zip 파일을 찾지 못했습니다.");
        }

        return new ReleaseInfo(version, description, downloadUrl);
    }

    public async Task DownloadAsync(
        string downloadUrl,
        string destinationPath,
        IProgress<double>? progress,
        CancellationToken cancellationToken = default)
    {
        using var response = await Http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var destination = System.IO.File.Create(destinationPath);

        var buffer = new byte[81920];
        long totalRead = 0;

        while (true)
        {
            var read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (read == 0)
            {
                break;
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            totalRead += read;

            if (totalBytes.HasValue && totalBytes > 0)
            {
                progress?.Report(totalRead * 100d / totalBytes.Value);
            }
        }
    }
}
