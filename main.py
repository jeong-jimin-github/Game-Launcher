import eel
import dl
import threading
import time
import fileunzip
import getpass
import os
import webview
import subprocess
import webbrowser
import platformcheck
import db
import customtkinter
import sys
from dotenv import load_dotenv

GITHUB_USERNAME = ""
TOKEN = ""

try:
    os.chdir(sys._MEIPASS)
    print(sys._MEIPASS)
except:
    os.chdir(os.getcwd())

isDebug = False

if os.path.exists(".env"):
    isDebug = True

    load_dotenv()
    GITHUB_USERNAME = os.environ.get('USERNAME')
    TOKEN = os.environ.get('TOKEN')

customtkinter.set_appearance_mode("System")
customtkinter.set_default_color_theme("blue")

platform = platformcheck.os()

if platform == "Windows":
    print("현재 OS: Windows")
    customtkinter.FontManager.load_font("font/PretendardVariable.ttf")
    import hPyT

    class Win(customtkinter.CTk):
        def __init__(self):
            super().__init__()
            self._offsetx = 0
            self._offsety = 0
            super().bind("<Button-1>" ,self.clickwin)
            super().bind("<B1-Motion>", self.dragwin)

        def dragwin(self,event):
            x = super().winfo_pointerx() - self._offsetx
            y = super().winfo_pointery() - self._offsety
            super().geometry(f"+{x}+{y}")

        def clickwin(self,event):
            self._offsetx = super().winfo_pointerx() - super().winfo_rootx()
            self._offsety = super().winfo_pointery() - super().winfo_rooty()

elif platform == "macOS":
    print("현재 OS: macOS")

else:
    print("현재 OS는 지원하지 않습니다.")
    exit()

username = getpass.getuser()
unzippath = os.path.join("C:\\Users", username, "SubarashiiGame")

if platform != "Windows":
    unzippath = os.path.join(os.path.dirname(os.path.realpath(__file__)), "SubarashiiGame")

print(unzippath)

downloaded = False
if os.path.exists(unzippath) == False:
    os.mkdir(unzippath)

db.init(os.path.join(unzippath, "db.db"))

# dl 함수를 위한 thread-safe 플래그
downloading = False
unarchiving = False

def center_window(window):
    screen_width = window.winfo_screenwidth()
    screen_height = window.winfo_screenheight()
    x = (screen_width - window.winfo_reqwidth()) // 2
    y = (screen_height - window.winfo_reqheight()) // 2
    window.geometry(f"+{x}+{y}")

def exitalert():
    if platform == "Windows":
        app = Win()
        app.geometry("300x90")
        center_window(app)
        hPyT.title_bar.hide(app)
        my_font = customtkinter.CTkFont(family="Pretendard Variable", size=15, weight='normal')

        def no():
            app.destroy()

        def yes():
            # Webview의 모든 창 닫기
            for window in webview.windows:
                window.closed = True
            os._exit(0)  # 프로그램 강제 종료

        label = customtkinter.CTkLabel(master=app, text="다운로드가 진행중입니다.\n중단하고 나가시겠습니까?", anchor='center', font=my_font)
        label.place(relx=0.5, rely=0.3, anchor=customtkinter.CENTER)

        button = customtkinter.CTkButton(master=app, text="아니오", command=no, font=my_font, width=70)
        button.place(relx=0.4, rely=0.6, anchor=customtkinter.NE)

        button1 = customtkinter.CTkButton(master=app, text="네", command=yes, font=my_font, width=70)
        button1.place(relx=0.6, rely=0.6, anchor=customtkinter.NW)

        app.mainloop()

def apialert():
    if platform == "Windows":
        app = Win()
        app.geometry("300x90")
        center_window(app)
        hPyT.title_bar.hide(app)
        my_font = customtkinter.CTkFont(family="Pretendard Variable", size=15, weight='normal')

        def yes():
            # Webview의 모든 창 닫기
            for window in webview.windows:
                window.closed = True
            os._exit(0)  # 프로그램 강제 종료

        label = customtkinter.CTkLabel(master=app, text="요청 할당량이 상한을 초과했습니다.\n1시간 후에 다시 시도해 주십시오.", anchor='center', font=my_font)
        label.place(relx=0.5, rely=0.3, anchor=customtkinter.CENTER)

        button1 = customtkinter.CTkButton(master=app, text="확인", command=yes, font=my_font, width=70)
        button1.place(relx=0.5, rely=0.6, anchor=customtkinter.CENTER)

        app.mainloop()

    if platform == "macOS":
        app = customtkinter.CTk()
        app.geometry("300x130")
        center_window(app)
        my_font = customtkinter.CTkFont(family="Pretendard Variable", size=15, weight='normal')

        def no():
            app.destroy()

        def yes():
            # Webview의 모든 창 닫기
            for window in webview.windows:
                window.closed = True
            os._exit(0)  # 프로그램 강제 종료

        label = customtkinter.CTkLabel(master=app, text="다운로드가 진행중입니다.\n중단하고 나가시겠습니까?", anchor='center', font=my_font)
        label.place(relx=0.5, rely=0.3, anchor=customtkinter.CENTER)

        button = customtkinter.CTkButton(master=app, text="아니오", command=no, font=my_font, width=70)
        button.place(relx=0.4, rely=0.6, anchor=customtkinter.NE)

        button1 = customtkinter.CTkButton(master=app, text="네", command=yes, font=my_font, width=70)
        button1.place(relx=0.6, rely=0.6, anchor=customtkinter.NW)

        app.mainloop()

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
    
if platform == "Windows":
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
    print("Launching WebView...")

    try:
        # WebView 창 생성
        window = webview.create_window(
            'Launcher',
            url='http://localhost:8080',
            frameless=True,
            resizable=False,
            width=scaled_width,
            height=scaled_height,
        )
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
        vs = dl.getinfo([GITHUB_USERNAME, TOKEN])[0]
        if vs == None:
            apialert()
        else:
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
    if downloading == True:
        exitalert()
    else:
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

        if platform != "Windows":
            file = check_for_mac_files(unzippath)
        if not file:
            print("No executable file found.")
            return

        print(f"Launching game: {file}")
        
        # EXE 파일 실행 및 대기
        if platform == "Windows":
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

def show_progress(downloaded, total):
    percent = (downloaded / total) * 100
    eel.fillRectangle(f"{percent:.2f}")
    print(f"\r다운로드 진행률: {percent:.2f}%", end="")

def show_unzip_progress(current, total):
    percent = (current / total) * 100
    eel.fillRectangle(f"{percent:.2f}")
    print(f"\r압축 해제 진행률: {percent:.2f}%", end="")

@eel.expose
def dlstart():
    """다운로드 및 압축 해제"""
    global downloading, unarchiving

    def download_and_extract():
        global downloading, unarchiving

        try:
            downloading = True
            downloading_thread = threading.Thread(target=display_downloading_message)
            downloading_thread.start()  # 다운로드 메시지 표시 스레드 시작

            dl.download([GITHUB_USERNAME, TOKEN], progress_callback=show_progress)

            downloading = False  # 다운로드 완료 후 스레드 종료
            downloading_thread.join()
            print("\n다운로드 완료!")
            eel.fillRectangle(0)
            # 압축 해제 메시지를 시작
            unarchiving = True
            unarchiving_thread = threading.Thread(target=display_unarchiving_message)
            unarchiving_thread.start()  # 압축 해제 메시지 표시 스레드 시작

            eel.print("압축 푸는 중...")
            zip_path = "./temp/SubarashiiGame-Windows.zip" if platform == "Windows" else "./temp/SubarashiiGame-macOS.zip"
            fileunzip.unzip(zip_path, progress_callback=show_unzip_progress)  # 압축 해제 시작

            unarchiving = False
            unarchiving_thread.join()  # 스레드 종료
            eel.fillRectangle(0)
            print("\n압축 해제 완료!")

            # 다운로드 및 압축 해제 완료 메시지
            eel.print("다운로드가 완료되었습니다.")
            eel.dlcomp()

        except Exception as e:
            # 에러 발생 시 상태 플래그 해제 및 메시지 출력
            downloading = False
            unarchiving = False
            print(f"에러 발생: {e}")
            eel.print(f"에러 발생: {e}")

    # 다운로드 및 압축 해제를 별도의 스레드에서 처리
    threading.Thread(target=download_and_extract, daemon=True).start()

if __name__ == '__main__':
    print("EEL 및 Webview 시작")

    # Eel 서버를 별도의 스레드에서 실행
    eel_thread = threading.Thread(target=start_eel, daemon=True)
    eel_thread.start()  # Eel 서버 시작

    # PyWebView를 메인 스레드에서 실행
    time.sleep(1)  # Eel 서버가 완전히 구동될 때까지 대기 (1초)
    if platform == "Windows":
        start_gui(946, 646)

    if platform == "macOS":
        start_gui(928, 600)
