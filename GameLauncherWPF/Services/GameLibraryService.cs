using System.IO;
using System.Text.Json;
using GameLauncherWPF.Models;

namespace GameLauncherWPF.Services;

public class GameLibraryService
{
    private static readonly string DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Game-Launcher");

    private static readonly string DataFile = Path.Combine(DataDirectory, "games.json");

    public List<GameEntry> Load()
    {
        if (!File.Exists(DataFile))
            return new List<GameEntry>();

        try
        {
            var json = File.ReadAllText(DataFile);
            return JsonSerializer.Deserialize<List<GameEntry>>(json) ?? new List<GameEntry>();
        }
        catch (JsonException)
        {
            // File is corrupted or malformed; return an empty list
            return new List<GameEntry>();
        }
        catch (IOException)
        {
            // File access error; return an empty list
            return new List<GameEntry>();
        }
    }

    public void Save(IEnumerable<GameEntry> games)
    {
        Directory.CreateDirectory(DataDirectory);
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(games.ToList(), options);
        File.WriteAllText(DataFile, json);
    }
}
