using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using GameLauncherWPF.Models;
using GameLauncherWPF.Services;

namespace GameLauncherWPF;

public partial class MainWindow : Window
{
    private readonly GameLibraryService _libraryService = new();
    private readonly ObservableCollection<GameEntry> _games = new();

    public MainWindow()
    {
        InitializeComponent();
        GameListBox.ItemsSource = _games;
        LoadGames();
    }

    private void LoadGames()
    {
        _games.Clear();
        foreach (var game in _libraryService.Load())
            _games.Add(game);
    }

    private void SaveGames() => _libraryService.Save(_games);

    private void GameListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GameListBox.SelectedItem is GameEntry game)
        {
            NameTextBox.Text = game.Name;
            PathTextBox.Text = game.ExecutablePath;
        }
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*",
            Title = "Select Game Executable"
        };

        if (dialog.ShowDialog() == true)
            PathTextBox.Text = dialog.FileName;
    }

    private void AddGame_Click(object sender, RoutedEventArgs e)
    {
        var name = NameTextBox.Text.Trim();
        var path = PathTextBox.Text.Trim();

        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show("Please enter a game name.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrEmpty(path))
        {
            MessageBox.Show("Please enter or browse for the game executable.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Update existing entry if selected, otherwise add new
        if (GameListBox.SelectedItem is GameEntry selected)
        {
            selected.Name = name;
            selected.ExecutablePath = path;
        }
        else
        {
            _games.Add(new GameEntry { Name = name, ExecutablePath = path });
        }

        SaveGames();
        NameTextBox.Clear();
        PathTextBox.Clear();
        GameListBox.SelectedItem = null;
    }

    private void RemoveGame_Click(object sender, RoutedEventArgs e)
    {
        if (GameListBox.SelectedItem is not GameEntry game)
        {
            MessageBox.Show("Please select a game to remove.", "No Selection",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _games.Remove(game);
        SaveGames();
        NameTextBox.Clear();
        PathTextBox.Clear();
    }

    private void LaunchGame_Click(object sender, RoutedEventArgs e)
    {
        if (GameListBox.SelectedItem is not GameEntry game)
        {
            MessageBox.Show("Please select a game to launch.", "No Selection",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!File.Exists(game.ExecutablePath))
        {
            MessageBox.Show($"Executable not found:\n{game.ExecutablePath}", "File Not Found",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = game.ExecutablePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to launch game:\n{ex.Message}", "Launch Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
