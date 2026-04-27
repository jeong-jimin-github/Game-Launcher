using System.IO;
using GameLauncherWPF.Services;
using Xunit;

namespace GameLauncherWPF.Tests;

/// <summary>
/// Unit tests for <see cref="VersionService"/> – equivalent to testing db.py
/// (setversion / getversion).
/// </summary>
public class VersionServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public VersionServiceTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void GetInstalledVersion_ReturnsNull_WhenFileDoesNotExist()
    {
        var svc = new VersionService(_tempDir);
        Assert.Null(svc.GetInstalledVersion());
    }

    [Fact]
    public void SetAndGet_RoundTrip_ReturnsCorrectVersion()
    {
        var svc = new VersionService(_tempDir);
        svc.SetInstalledVersion("v1.2.3");
        Assert.Equal("v1.2.3", svc.GetInstalledVersion());
    }

    [Fact]
    public void SetInstalledVersion_Overwrites_PreviousValue()
    {
        var svc = new VersionService(_tempDir);
        svc.SetInstalledVersion("v1.0.0");
        svc.SetInstalledVersion("v2.0.0");
        Assert.Equal("v2.0.0", svc.GetInstalledVersion());
    }

    [Fact]
    public void GetInstalledVersion_Trims_WhitespaceFromFile()
    {
        var versionFile = Path.Combine(_tempDir, "version.txt");
        File.WriteAllText(versionFile, "  v3.0.0  \n");

        var svc = new VersionService(_tempDir);
        Assert.Equal("v3.0.0", svc.GetInstalledVersion());
    }
}
