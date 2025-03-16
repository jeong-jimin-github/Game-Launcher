
source ./.venv/bin/activate
pyinstaller --onefile --noconsole --add-data=web:web --icon=icon.ico main.py
