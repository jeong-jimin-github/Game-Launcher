using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace GameLauncherWPF.Services;

public static class GameProcessService
{
    public static async Task RunGameAndWaitAsync(string executablePath, CancellationToken cancellationToken = default)
    {
        var info = new ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = true,
            WorkingDirectory = System.IO.Path.GetDirectoryName(executablePath) ?? GamePaths.GameRoot
        };

        using var process = Process.Start(info) ?? throw new InvalidOperationException("게임 실행에 실패했습니다.");
        await process.WaitForExitAsync(cancellationToken);
    }
}
