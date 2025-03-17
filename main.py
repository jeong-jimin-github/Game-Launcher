import eel
from dl import download, getinfo
import threading
import time
from fileunzip import unzip
import getpass
import os
import webview
import subprocess
import webbrowser
import platformcheck
import db

if platformcheck.os() == "Windows":
    print("Your OS: Windows")

elif platformcheck.os() == "macOS":
    print("Your OS: macOS")

else:
    print("Your OS is not support!")
    exit()

username = getpass.getuser()
unzippath = os.path.join("C:\\Users", username, "SubarashiiGame")

if platformcheck.os() != "Windows":
    unzippath = os.path.join(os.path.dirname(os.path.realpath(__file__)), "SubarashiiGame")

print(unzippath)

downloaded = False
if os.path.exists(unzippath) == False:
    os.mkdir(unzippath)

db.init(os.path.join(unzippath, "db.db"))

# dl 함수를 위한 thread-safe 플래그
downloading = True
unarchiving = False

# Downloading 메시지를 출력하는 함수
def display_downloading_message():
    global downloading
    dots = ["", ".", "..", "..."]
    i = 0
    while downloading:  # 다운로드가 완료되지 않았을 동안에는 계속 표시
        eel.print(f"다운로드 중{dots[i % len(dots)]}")  # eel.print 표시
        i += 1
        time.sleep(0.5)

# Unarchiving 메시지를 출력하는 함수
def display_unarchiving_message():
    global unarchiving
    dots = ["", ".", "..", "..."]
    i = 0
    while unarchiving:  # 다운로드가 완료되지 않았을 동안에는 계속 표시
        eel.print(f"압축 푸는 중{dots[i % len(dots)]}")  # eel.print 표시
        i += 1
        time.sleep(0.5)

def check_for_exe_files(folder_path):
    try:
        # 폴더 안의 파일과 디렉토리 목록 가져오기
        files = os.listdir(folder_path)
        
        # UnityCrashHandler64.exe 이외의 exe 파일 찾기
        for file in files:
            if file.endswith('.exe') and file != 'UnityCrashHandler64.exe':
                print(f"Found other .exe file: {file}")
                return file
        
        print("No other .exe files found in the folder.")
        return False
    except FileNotFoundError:
        print(f"The folder path '{folder_path}' does not exist.")
        return False
    except Exception as e:
        print(f"An error occurred: {e}")
        return False
    
def check_for_mac_files(folder_path):
    try:
        # 폴더 안의 파일과 디렉토리 목록 가져오기
        files = os.listdir(folder_path)
        
        # UnityCrashHandler64.exe 이외의 exe 파일 찾기
        for file in files:
            if file.endswith('.app'):
                print(f"Found other .exe file: {file}")
                return file
        
        print("No other .exe files found in the folder.")
        return False

    except FileNotFoundError:
        print(f"The folder path '{folder_path}' does not exist.")
        return False
    except Exception as e:
        print(f"An error occurred: {e}")
        return False
    
if platformcheck.os() == "Windows":
    if check_for_exe_files(unzippath) != False:
        downloaded = True

else:
    if check_for_mac_files(unzippath) != False:
        downloaded = True

def start_eel():
    """Eel 서버 실행"""
    try:
        eel.init('web')  # 'web' 폴더 안에 HTML/JS/CSS가 들어 있어야 함
        # block=False 및 mode=None로 설정 (브라우저 창 비활성화)
        eel.start('index.html', host="localhost", port=8080, block=True, mode=None)  
    except Exception as e:
        print(f"Error starting Eel: {e}")


def start_gui(scaled_width, scaled_height):
    """PyWebView 창 실행"""
    try:
        # webview 창 생성
        window = webview.create_window(
            'My App',
            url='http://localhost:8080',
            frameless=True,
            width=scaled_width,
            height=scaled_height,
        )

        # WebView 시작
        webview.start(debug=False)
    except Exception as e:
        print(f"Error starting WebView: {e}")

# Eel 초기화
eel.init('web')  # 'web' 디렉토리가 제대로 확인되었는지 꼭 확인

def open_web_browser():
    # 웹 브라우저에서 Discord 링크 열기
    webbrowser.open_new("https://discord.gg/xMeUY8JsaR")

@eel.expose
def contact():
    # open_web_browser 함수를 별도의 스레드에서 호출
    browser_thread = threading.Thread(target=open_web_browser)
    browser_thread.start()
    
@eel.expose
def drag_window():
    print("drag_window called")  # 함수 호출 확인
    # 웹뷰 창 이동 함수
    webview.windows[0].drag()


@eel.expose
def dlcheck():
    if downloaded == True:
        print("게임 파일 찾음")
        vs = getinfo()[0]
        print("현재 게임 버전: " + db.getversion(os.path.join(unzippath, "db.db")))
        if db.getversion(os.path.join(unzippath, "db.db")) == vs:
            print("게임 파일 최신버전임")
            eel.print("「플레이」 버튼을 누르세요.")
            eel.dlcomp()
        else:
            print("최신 버전 발견: " + vs)
            eel.print("업데이트가 필요합니다.")
            eel.youp

@eel.expose
def pexit():
    try:
        # Webview의 모든 창 닫기
        for window in webview.windows:
            window.closed = True
        os._exit(0)  # 프로그램 강제 종료
    except Exception as e:
        print(f"Error encountered while trying to exit: {e}")
        

@eel.expose
def play():
    def run_game():
        # 게임 실행 전 pyn 실행
        eel.print("게임이 이미 실행중입니다.")
        eel.pyn()
        
        # 실행 가능한 EXE 파일 확인

        file = check_for_exe_files(unzippath)

        if platformcheck.os() != "Windows":
            file = check_for_mac_files(unzippath)
        if not file:
            print("No executable file found.")
            return

        print(f"Launching game: {file}")
        
        # EXE 파일 실행 및 대기
        if platformcheck.os() == "Windows":
            process = subprocess.Popen(f'"{unzippath}\\{file}"', shell=True)  # EXE 파일 실행
        
            process.wait()  # 게임 프로세스가 종료될 때까지 대기

        else:
            process = subprocess.call(
        ["/usr/bin/open", "-W", "-n", "-a", f"{unzippath}/{file}"]
        )

        print("Game finished.")

        # 게임 종료 후 pyc 호출
        eel.print("「플레이」 버튼을 누르세요.")
        eel.pyc()

    # 게임 실행을 위해 별도 스레드에서 처리
    game_thread = threading.Thread(target=run_game, daemon=True)
    game_thread.start()
    
@eel.expose
def dl():
    global downloading, unarchiving
    try:
        # 다운로드 메시지를 시작
        downloading_thread = threading.Thread(target=display_downloading_message)
        downloading = True
        downloading_thread.start()

        eel.print("다운로드 중...")
        download()  # 다운로드 작업 수행
        downloading = False  # 다운로드 완료
        downloading_thread.join()

        # 압축 해제 메시지를 시작
        unarchiving_thread = threading.Thread(target=display_unarchiving_message)
        unarchiving = True
        unarchiving_thread.start()

        eel.print("압축 푸는 중...")
        if platformcheck.os() == "Windows":
            unzip('./temp/SubarashiiGame-Windows.zip')  # 압축 해제 시작
        else:
            unzip('./temp/SubarashiiGame-macOS.zip')  # 압축 해제 시작   
        eel.print("압축 풀기가 완료되었습니다.")

        unarchiving = False
        unarchiving_thread.join()

        eel.print("다운로드가 완료되었습니다.")
        eel.dlcomp()
    except Exception as e:
        downloading = False
        unarchiving = False
        eel.print(f"에러: {e}")

if __name__ == '__main__':
    print("EEL 및 Webview 시작")

    # Eel 서버를 별도의 스레드에서 실행
    eel_thread = threading.Thread(target=start_eel, daemon=True)
    eel_thread.start()  # Eel 서버 시작

    # PyWebView를 메인 스레드에서 실행
    viewport_height = 1000  # 고정값 또는 입력값
    time.sleep(1)  # Eel 서버가 완전히 구동될 때까지 대기 (1초)
    start_gui(946, 646)