import os
import getpass
import shutil
import platform

username = getpass.getuser()
unzippath = os.path.join("C:\\Users", username, "SubarashiiGame")
if platform.platform() != "Windows":
    unzippath = os.path.join(os.path.dirname(os.path.realpath(__file__)), "SubarashiiGame")

def unzip(path):
    try:
        # 압축 해제 경로 초기화
        if os.path.exists(unzippath):
            try:
                shutil.rmtree(unzippath)  # 기존 디렉토리 삭제
            except PermissionError:
                print(f"ERROR: Permission denied when trying to delete {unzippath}")
                return
            except OSError as e:
                print(f"ERROR: Failed to delete directory {unzippath}. {e}")
                return
        os.makedirs(unzippath, exist_ok=True)

        # 압축 해제
        shutil.unpack_archive(path, unzippath, 'zip')
        print("Unzipping completed successfully.")
        shutil.rmtree("./temp")
    except FileNotFoundError as fnfe:
        print(f"ERROR: File not found. {fnfe}")
    except PermissionError as pe:
        print(f"ERROR: Permission denied. {pe}")
    except Exception as e:
        print(f"ERROR: An unexpected error occurred. {e}")