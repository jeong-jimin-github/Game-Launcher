using System;
using System.IO;

namespace GameLauncherWPF.Services;

public static class GamePaths
{
    public static string GameRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "SubarashiiGame");

    public static string TempRoot => Path.Combine(AppContext.BaseDirectory, "temp");

    public static string VersionFile => Path.Combine(GameRoot, "version.txt");
}
