# Game Launcher (WPF)

간단한 WPF 기반 게임 런처 프로젝트입니다.

## 기술 스택

- .NET 8
- WPF (Windows)
- C#

## 프로젝트 구조

- `ViewModels/` : UI 상태 및 명령 처리
- `Services/` : 설치, 실행, GitHub 릴리즈 조회 등 비즈니스 로직
- `Models/` : 데이터 모델
- `Infrastructure/` : `RelayCommand`, `AsyncRelayCommand` 등 공통 인프라
- `Assets/` : 아이콘/이미지 등 정적 리소스

## 실행 방법

1. .NET SDK 8.0 이상 설치
2. 루트 폴더에서 빌드

```bash
dotnet build GameLauncherWPF.sln
```

3. 실행

```bash
dotnet run --project GameLauncherWPF.csproj
```

## 배포 빌드

```bash
dotnet publish GameLauncherWPF.csproj -c Release
```

- 결과물: `bin/Release/net8.0-windows/win-x64/publish/GameLauncherWPF.exe` (단일 파일)
- 결과물: `bin/Release/net8.0-windows/win-x64/publish/GameLauncherWPF.exe` (실제 1개 파일로 출력)

## 참고

- 이 프로젝트는 Windows 환경(WPF)에서 동작합니다.
- 로컬 빌드 산출물(`bin/`, `obj/`)과 IDE 설정(`.vs/`)은 `.gitignore`에 의해 추적되지 않습니다.
