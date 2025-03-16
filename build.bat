@echo off
call .venv/Scripts/activate.bat
REM PyInstaller 명령 실행
pyinstaller --onefile --upx-dir upx --noconsole --add-data "web;web" --icon=icon.ico main.py

REM 빌드 완료 메시지
echo Build completed!
