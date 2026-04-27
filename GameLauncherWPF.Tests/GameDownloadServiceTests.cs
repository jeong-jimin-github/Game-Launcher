using System.IO;
using GameLauncherWPF.Services;
using Xunit;

namespace GameLauncherWPF.Tests;

/// <summary>
/// Unit tests for <see cref="GameDownloadService"/> helper methods –
/// equivalent to testing check_for_exe_files() from main.py.
/// </summary>
public class GameDownloadServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public GameDownloadServiceTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void FindGameExecutable_ReturnsNull_WhenFolderEmpty()
    {
        Assert.Null(GameDownloadService.FindGameExecutable(_tempDir));
    }

    [Fact]
    public void FindGameExecutable_ReturnsNull_WhenFolderDoesNotExist()
    {
        Assert.Null(GameDownloadService.FindGameExecutable(
            Path.Combine(_tempDir, "nonexistent")));
    }

    [Fact]
    public void FindGameExecutable_SkipsUnityCrashHandler()
    {
        File.WriteAllText(Path.Combine(_tempDir, "UnityCrashHandler64.exe"), "");
        Assert.Null(GameDownloadService.FindGameExecutable(_tempDir));
    }

    [Fact]
    public void FindGameExecutable_ReturnsMainExe_IgnoresCrashHandler()
    {
        File.WriteAllText(Path.Combine(_tempDir, "UnityCrashHandler64.exe"), "");
        var mainExe = Path.Combine(_tempDir, "SubarashiiGame.exe");
        File.WriteAllText(mainExe, "");

        var result = GameDownloadService.FindGameExecutable(_tempDir);
        Assert.Equal(mainExe, result);
    }

    [Fact]
    public void FindGameExecutable_IsCaseInsensitiveForCrashHandler()
    {
        // Ensure the crash-handler filter is case-insensitive
        File.WriteAllText(Path.Combine(_tempDir, "unityCrashHandler64.EXE"), "");
        Assert.Null(GameDownloadService.FindGameExecutable(_tempDir));
    }
}
