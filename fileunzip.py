import os
import getpass
import shutil
import platformcheck
import subprocess

username = getpass.getuser()
unzippath = os.path.join("C:\\Users", username, "SubarashiiGame")
if platformcheck.os() != "Windows":
    unzippath = os.path.join(os.path.dirname(os.path.realpath(__file__)), "SubarashiiGame")

def remove_except(directory, exclude_files):
    """
    directory: 삭제 작업을 수행할 디렉토리 경로
    exclude_files: 제외할 파일/폴더 이름의 리스트
    """
    # 디렉토리의 모든 항목 확인
    for item in os.listdir(directory):
        item_path = os.path.join(directory, item)
        # 제외할 파일/폴더가 아닌 경우 삭제
        if item not in exclude_files:
            if os.path.isdir(item_path):
                shutil.rmtree(item_path)  # 폴더 삭제
            else:
                os.remove(item_path)  # 파일 삭제

def unzip(path):
    try:
        # 압축 해제 경로 초기화
        if os.path.exists(unzippath):
            try:
                remove_except(unzippath, ['db.db'])
            except PermissionError:
                print(f"ERROR: Permission denied when trying to delete {unzippath}")
                return
            except OSError as e:
                print(f"ERROR: Failed to delete directory {unzippath}. {e}")
                return
        os.makedirs(unzippath, exist_ok=True)

        # 압축 해제
        if platformcheck.os() == "Windows":
            shutil.unpack_archive(path, unzippath, 'zip')
        else:
            subprocess.run(['unzip', path, '-d', unzippath], check=True)
        print("Unzipping completed successfully.")
        shutil.rmtree("./temp")
    except FileNotFoundError as fnfe:
        print(f"ERROR: File not found. {fnfe}")
    except PermissionError as pe:
        print(f"ERROR: Permission denied. {pe}")
    except Exception as e:
        print(f"ERROR: An unexpected error occurred. {e}")