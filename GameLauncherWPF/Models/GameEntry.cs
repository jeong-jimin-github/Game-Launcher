using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GameLauncherWPF.Models;

public class GameEntry : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _executablePath = string.Empty;

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public string ExecutablePath
    {
        get => _executablePath;
        set { _executablePath = value; OnPropertyChanged(); }
    }

    public override string ToString() => Name;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
