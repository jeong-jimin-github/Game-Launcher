using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using GameLauncherWPF.Services;

namespace GameLauncherWPF;

/// <summary>
/// Launcher states that mirror the Python launcher's UI flow:
///   NotDownloaded → Downloading → Extracting → Ready
///   Ready → UpdateAvailable (if newer version on GitHub)
///   Ready → Playing → Ready
/// </summary>
public enum LauncherState
{
    CheckingVersion,
    NotDownloaded,
    Downloading,
    Extracting,
    UpdateAvailable,
    Ready,
    Playing
}

public partial class MainWindow : Window
{
    // ── Game folder: equivalent to Python's unzippath ──────────────────────
    private static readonly string GameFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "SubarashiiGame");

    // Discord invite URL (same as Python launcher)
    private const string DiscordUrl = "https://discord.gg/xMeUY8JsaR";

    private LauncherState _state = LauncherState.CheckingVersion;
    private CancellationTokenSource _cts = new();
    private readonly VersionService _versionService = new(GameFolder);
    private readonly GitHubReleaseService _releaseService = new();
    private readonly GameDownloadService _downloadService = new();

    // ── Constructor ─────────────────────────────────────────────────────────
    public MainWindow()
    {
        InitializeComponent();
        LoadBackgroundImage();
        Loaded += async (_, _) => await CheckGameStateAsync();
        Closed += (_, _) =>
        {
            _cts.Dispose();
            _releaseService.Dispose();
            _downloadService.Dispose();
        };
    }

    // ── Background image ────────────────────────────────────────────────────
    private void LoadBackgroundImage()
    {
        // Look for the background image in the Assets folder next to the executable
        var imgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "image-4.png");

        if (File.Exists(imgPath))
        {
            try
            {
                BackgroundImage.Source = new BitmapImage(new Uri(imgPath, UriKind.Absolute));
            }
            catch { /* Background stays dark */ }
        }
    }

    // ── Game state check (equivalent to Python's dlcheck) ───────────────────
    private async Task CheckGameStateAsync()
    {
        SetState(LauncherState.CheckingVersion, "버전 확인 중...");

        Directory.CreateDirectory(GameFolder);

        var exe = GameDownloadService.FindGameExecutable(GameFolder);
        if (exe is null)
        {
            // Not installed yet – show Download button
            SetState(LauncherState.NotDownloaded, string.Empty);
            return;
        }

        // Game is installed: check for updates
        var latest = await _releaseService.GetLatestReleaseAsync();
        if (latest is null)
        {
            // GitHub API unavailable / rate-limited (equivalent to Python's apialert)
            SetState(LauncherState.Ready,
                "요청 할당량이 상한을 초과했습니다. 1시간 후에 다시 시도해 주십시오.");
            return;
        }

        var installed = _versionService.GetInstalledVersion();
        if (installed == latest.Version)
        {
            SetState(LauncherState.Ready, "「플레이」 버튼을 누르세요.");
        }
        else
        {
            SetState(LauncherState.UpdateAvailable, "업데이트가 필요합니다.");
        }
    }

    // ── Action button handler ────────────────────────────────────────────────
    private async void Action_Click(object sender, RoutedEventArgs e)
    {
        switch (_state)
        {
            case LauncherState.NotDownloaded:
            case LauncherState.UpdateAvailable:
                await DownloadAndInstallAsync();
                break;

            case LauncherState.Ready:
                await PlayGameAsync();
                break;
        }
    }

    // ── Download + extract (equivalent to Python's dlstart) ─────────────────
    private async Task DownloadAndInstallAsync()
    {
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        try
        {
            // ① Get release info
            SetState(LauncherState.Downloading, "대기중...");

            var release = await _releaseService.GetLatestReleaseAsync(_cts.Token);
            if (release is null)
            {
                SetState(LauncherState.NotDownloaded,
                    "다운로드 정보를 가져오는 데 실패했습니다.");
                return;
            }

            // ② Download ZIP
            var tempZip = Path.Combine(Path.GetTempPath(), "SubarashiiGame-Windows.zip");
            ShowProgressBar();

            var downloadProgress = new Progress<double>(p =>
            {
                Dispatcher.Invoke(() =>
                {
                    ProgressBar.Value = p;
                    InfoText.Text = $"다운로드 중... {p:F1}%";
                });
            });

            await _downloadService.DownloadAsync(
                release.DownloadUrl, tempZip, downloadProgress, _cts.Token);

            // ③ Extract ZIP
            SetState(LauncherState.Extracting, "압축 푸는 중...");
            ProgressBar.Value = 0;

            var extractProgress = new Progress<double>(p =>
            {
                Dispatcher.Invoke(() => ProgressBar.Value = p);
            });

            await Task.Run(
                () => _downloadService.ExtractZip(tempZip, GameFolder, extractProgress),
                _cts.Token);

            // ④ Save version
            _versionService.SetInstalledVersion(release.Version);

            HideProgressBar();
            SetState(LauncherState.Ready, "다운로드가 완료되었습니다.");
        }
        catch (OperationCanceledException)
        {
            HideProgressBar();
            SetState(LauncherState.NotDownloaded, string.Empty);
        }
        catch (Exception ex)
        {
            HideProgressBar();
            SetState(LauncherState.NotDownloaded, $"에러 발생: {ex.Message}");
        }
    }

    // ── Play game (equivalent to Python's play) ──────────────────────────────
    private async Task PlayGameAsync()
    {
        var exe = GameDownloadService.FindGameExecutable(GameFolder);
        if (exe is null)
        {
            SetState(LauncherState.NotDownloaded, "실행 파일을 찾을 수 없습니다.");
            return;
        }

        SetState(LauncherState.Playing, "게임이 이미 실행중입니다.");

        try
        {
            await Task.Run(() =>
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exe,
                        UseShellExecute = true,
                        WorkingDirectory = GameFolder
                    }
                };
                process.Start();
                process.WaitForExit();
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"게임 실행에 실패했습니다:\n{ex.Message}", "실행 오류",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        SetState(LauncherState.Ready, "「플레이」 버튼을 누르세요.");
    }

    // ── State machine helpers ────────────────────────────────────────────────
    private void SetState(LauncherState state, string infoMessage)
    {
        _state = state;

        Dispatcher.Invoke(() =>
        {
            InfoText.Text = infoMessage;

            ActionButton.IsEnabled = state is LauncherState.NotDownloaded
                                            or LauncherState.UpdateAvailable
                                            or LauncherState.Ready;

            ActionButton.Content = state switch
            {
                LauncherState.NotDownloaded    => "다운로드",
                LauncherState.Downloading      => "대기중",
                LauncherState.Extracting       => "압축 해제중",
                LauncherState.UpdateAvailable  => "업데이트",
                LauncherState.Ready            => "플레이",
                LauncherState.Playing          => "플레이 중",
                LauncherState.CheckingVersion  => "확인중...",
                _                              => "다운로드"
            };

            // Gray out button while busy; otherwise clear local override so the Style value applies
            if (state is LauncherState.Playing
                       or LauncherState.Downloading
                       or LauncherState.Extracting
                       or LauncherState.CheckingVersion)
            {
                ActionButton.Background = System.Windows.Media.Brushes.Gray;
            }
            else
            {
                ActionButton.ClearValue(BackgroundProperty);
            }
        });
    }

    private void ShowProgressBar()
    {
        Dispatcher.Invoke(() => ProgressBar.Visibility = Visibility.Visible);
    }

    private void HideProgressBar()
    {
        Dispatcher.Invoke(() =>
        {
            ProgressBar.Visibility = Visibility.Collapsed;
            ProgressBar.Value = 0;
        });
    }

    // ── Window dragging ──────────────────────────────────────────────────────
    private void DragRegion_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            DragMove();
    }

    // ── Close / exit (equivalent to Python's pexit + exitalert) ─────────────
    private void Close_Click(object sender, RoutedEventArgs e)
    {
        if (_state is LauncherState.Downloading or LauncherState.Extracting)
        {
            var result = MessageBox.Show(
                "다운로드가 진행중입니다.\n중단하고 나가시겠습니까?",
                "종료 확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            _cts.Cancel();
        }

        Application.Current.Shutdown();
    }

    // ── Discord ──────────────────────────────────────────────────────────────
    private void Discord_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(DiscordUrl) { UseShellExecute = true });
        }
        catch { /* Ignore if browser cannot be opened */ }
    }
}
