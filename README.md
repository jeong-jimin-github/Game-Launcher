# SubarashiiGame Launcher

SubarashiiGame Launcher – Windows & macOS game launcher for the [SubarashiiGame](https://github.com/jeong-jimin-github/Unity-Game) Unity project.

[Download release (Windows & macOS)](https://github.com/jeong-jimin-github/Game-Launcher/releases)

## Screenshots
![스크린샷 2025-03-24 오후 7 08 29](https://github.com/user-attachments/assets/205cafb7-a20c-494d-a7d8-9ed09073ba3f)

---

## Python Launcher

The Python launcher is the original implementation and supports **Windows** and **macOS**.

### Prerequisites
- Python 3.10+
- Install dependencies:
  ```bash
  pip install -r requirements.txt
  ```

### Run
```bash
python main.py
```

### Build (stand-alone executable)
```bash
# Windows
pyinstaller --onefile --windowed --noconsole \
    --add-data web:web --add-data font:font \
    --icon icon.ico --upx-dir upx main.py

# macOS
pyinstaller --onefile --windowed --noconsole \
    --add-data web:web --icon icon.icns main.py
```

---

## C# Launcher (WPF)

The C# launcher is a Windows-only WPF application that provides **feature parity** with the Python launcher.

### Prerequisites
- .NET 8 SDK
- Windows (WPF)

### Build
```powershell
dotnet build GameLauncher.sln --configuration Release
```

### Run
```powershell
dotnet run --project GameLauncherWPF/GameLauncherWPF.csproj
```

### Run tests
```powershell
dotnet test GameLauncherWPF.Tests/GameLauncherWPF.Tests.csproj --verbosity normal
```

### Publish (self-contained, win-x64)
```powershell
dotnet publish GameLauncherWPF/GameLauncherWPF.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output publish/GameLauncherWPF
```

---

## Feature Parity

| Feature | Python | C# (WPF) |
|---|---|---|
| Frameless window | ✅ (WebView 946×646) | ✅ (WPF 946×646) |
| Korean UI | ✅ | ✅ |
| Download game from GitHub Releases | ✅ | ✅ |
| Download progress bar | ✅ | ✅ |
| Extract ZIP with progress | ✅ | ✅ |
| Version check (update detection) | ✅ (SQLite) | ✅ (version.txt) |
| Play button (launches .exe) | ✅ | ✅ |
| Update button (when outdated) | ✅ | ✅ |
| Playing state (button grayed) | ✅ | ✅ |
| Exit confirmation during download | ✅ | ✅ |
| Discord server button | ✅ | ✅ |
| macOS support | ✅ | ❌ (WPF is Windows-only) |
| Background drag (frameless) | ✅ | ✅ |
| Game folder (\`~/SubarashiiGame\`) | ✅ | ✅ |
| Skip \`UnityCrashHandler64.exe\` | ✅ | ✅ |

### Notes
- **macOS**: The C# version is Windows-only because WPF does not run on macOS. The Python launcher continues to support both platforms.
- **Version storage**: Python uses SQLite (`db.db`); C# uses a plain `version.txt` file in the same game folder — functionally equivalent.
- **UI framework**: Python uses a custom HTML/CSS UI via pywebview+eel; C# uses native WPF with the same color scheme and layout.
