using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using GameLauncherWPF.Infrastructure;
using GameLauncherWPF.Models;
using GameLauncherWPF.Services;

namespace GameLauncherWPF.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly GitHubReleaseService _releaseService = new();
    private readonly GameInstallService _installService;

    private ReleaseInfo? _latestRelease;
    private CancellationTokenSource? _downloadCts;

    private string _statusMessage = "런처를 초기화하는 중입니다...";
    private string _actionButtonText = "대기 중";
    private bool _isDownloading;
    private bool _isPlaying;
    private bool _isInstalled;
    private bool _isUpdateRequired;
    private double _progressPercent;
    private Brush _actionButtonBrush = new SolidColorBrush(Color.FromRgb(255, 191, 0));

    public MainViewModel()
    {
        _installService = new GameInstallService(_releaseService);

        PrimaryActionCommand = new AsyncRelayCommand(ExecutePrimaryActionAsync, () => !IsDownloading && !IsPlaying);
        RefreshCommand = new AsyncRelayCommand(InitializeAsync, () => !IsDownloading && !IsPlaying);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand PrimaryActionCommand { get; }
    public ICommand RefreshCommand { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public string ActionButtonText
    {
        get => _actionButtonText;
        private set => SetField(ref _actionButtonText, value);
    }

    public bool IsDownloading
    {
        get => _isDownloading;
        private set
        {
            if (SetField(ref _isDownloading, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        private set
        {
            if (SetField(ref _isPlaying, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public bool IsInstalled
    {
        get => _isInstalled;
        private set => SetField(ref _isInstalled, value);
    }

    public bool IsUpdateRequired
    {
        get => _isUpdateRequired;
        private set => SetField(ref _isUpdateRequired, value);
    }

    public double ProgressPercent
    {
        get => _progressPercent;
        private set => SetField(ref _progressPercent, value);
    }

    public Brush ActionButtonBrush
    {
        get => _actionButtonBrush;
        private set => SetField(ref _actionButtonBrush, value);
    }

    public async Task InitializeAsync()
    {
        ProgressPercent = 0;
        IsInstalled = GameInstallService.IsInstalled();

        if (!IsInstalled)
        {
            StatusMessage = "게임이 설치되어 있지 않습니다. 다운로드를 시작하세요.";
            IsUpdateRequired = false;
            SetPrimaryAction("다운로드", Color.FromRgb(255, 191, 0));
            return;
        }

        StatusMessage = "버전 정보를 확인하는 중입니다...";

        try
        {
            _latestRelease = await _releaseService.GetLatestReleaseAsync();
            var localVersion = GameInstallService.GetLocalVersion();

            if (string.Equals(localVersion, _latestRelease.Version, StringComparison.OrdinalIgnoreCase))
            {
                IsUpdateRequired = false;
                SetPrimaryAction("플레이", Color.FromRgb(255, 191, 0));
                StatusMessage = "최신 버전입니다. 플레이 버튼을 눌러주세요.";
            }
            else
            {
                IsUpdateRequired = true;
                SetPrimaryAction("업데이트", Color.FromRgb(255, 191, 0));
                StatusMessage = "새 버전이 있습니다. 업데이트를 진행하세요.";
            }
        }
        catch (GitHubRateLimitException)
        {
            StatusMessage = "요청 할당량이 상한을 초과했습니다. 잠시 후 다시 시도해 주세요.";
            SetPrimaryAction("재시도", Color.FromRgb(108, 117, 125));
        }
        catch (Exception ex)
        {
            StatusMessage = $"버전 확인 실패: {ex.Message}";
            SetPrimaryAction("재시도", Color.FromRgb(108, 117, 125));
        }
    }

    public bool TryRequestClose()
    {
        if (!IsDownloading)
        {
            return true;
        }

        return false;
    }

    public void CancelCurrentDownload() => _downloadCts?.Cancel();

    private async Task ExecutePrimaryActionAsync()
    {
        if (!IsInstalled || IsUpdateRequired)
        {
            await DownloadAndInstallAsync();
            return;
        }

        await PlayAsync();
    }

    private async Task DownloadAndInstallAsync()
    {
        try
        {
            _latestRelease ??= await _releaseService.GetLatestReleaseAsync();

            _downloadCts = new CancellationTokenSource();
            IsDownloading = true;
            SetPrimaryAction("대기 중", Color.FromRgb(108, 117, 125));
            StatusMessage = "다운로드를 시작합니다...";
            ProgressPercent = 0;

            var progress = new Progress<double>(value =>
            {
                ProgressPercent = Math.Clamp(value, 0, 100);

                if (ProgressPercent < 80)
                {
                    StatusMessage = $"다운로드 중... {ProgressPercent:0.0}%";
                }
                else
                {
                    StatusMessage = $"압축 해제 중... {ProgressPercent:0.0}%";
                }
            });

            await _installService.DownloadAndInstallAsync(_latestRelease, progress, _downloadCts.Token);

            IsInstalled = true;
            IsUpdateRequired = false;
            SetPrimaryAction("플레이", Color.FromRgb(255, 191, 0));
            StatusMessage = "다운로드가 완료되었습니다. 플레이 버튼을 눌러주세요.";
            ProgressPercent = 0;
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "다운로드가 취소되었습니다.";
            ProgressPercent = 0;
        }
        catch (Exception ex)
        {
            StatusMessage = $"설치 중 오류가 발생했습니다: {ex.Message}";
            SetPrimaryAction("재시도", Color.FromRgb(220, 53, 69));
        }
        finally
        {
            IsDownloading = false;
            _downloadCts?.Dispose();
            _downloadCts = null;
        }
    }

    private async Task PlayAsync()
    {
        var executablePath = GameInstallService.FindExecutablePath();
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            StatusMessage = "실행 파일을 찾지 못했습니다. 다시 설치해 주세요.";
            IsInstalled = false;
            IsUpdateRequired = true;
            SetPrimaryAction("다운로드", Color.FromRgb(255, 191, 0));
            return;
        }

        try
        {
            IsPlaying = true;
            SetPrimaryAction("플레이 중", Color.FromRgb(108, 117, 125));
            StatusMessage = "게임이 이미 실행중입니다.";

            await GameProcessService.RunGameAndWaitAsync(executablePath);

            SetPrimaryAction("플레이", Color.FromRgb(255, 191, 0));
            StatusMessage = "게임이 종료되었습니다. 다시 플레이할 수 있습니다.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"게임 실행 실패: {ex.Message}";
            SetPrimaryAction("플레이", Color.FromRgb(255, 191, 0));
        }
        finally
        {
            IsPlaying = false;
        }
    }

    private void SetPrimaryAction(string text, Color color)
    {
        ActionButtonText = text;
        ActionButtonBrush = new SolidColorBrush(color);
    }

    private void RaiseCommandStates()
    {
        if (PrimaryActionCommand is AsyncRelayCommand primary)
        {
            primary.RaiseCanExecuteChanged();
        }

        if (RefreshCommand is AsyncRelayCommand refresh)
        {
            refresh.RaiseCanExecuteChanged();
        }
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
