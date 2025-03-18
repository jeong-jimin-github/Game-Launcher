import requests
from bs4 import BeautifulSoup
import os
import subprocess
import db
import platformcheck
import getpass
platform = platformcheck.os()

RELEASES_URL = "https://api.github.com/repos/jeong-jimin-github/Unity-Game/releases/latest"
username = getpass.getuser()

unzippath = os.path.join("C:\\Users", username, "SubarashiiGame")
if platform != "Windows":
    unzippath = os.path.join(os.path.dirname(os.path.realpath(__file__)), "SubarashiiGame")

def hide_folder_windows(folder_path):
    subprocess.run(["attrib", "+h", folder_path], shell=True)

def fetch_latest_release(auth):
    try:
        if auth[0] == "":
            global response
            response = requests.get(RELEASES_URL)
        else:
            global response
            response = requests.get(RELEASES_URL, auth=(auth[0],auth[1]))

        if response.status_code != 200:
            print("릴리스 체크 실패")
            
        receive = response.json()
        download_url = ""
        assets = receive['assets']
        for aa in assets:
            if platform == "Windows":
                if aa['name'] == "Windows-Build.zip":
                    download_url = aa['url']
                    continue

            if platform == "macOS":
                if aa['name'] == "MacOS-Build.zip":
                    download_url = aa['url']
                    continue
        if auth[0] == "":
            global dlurl
            dlurl = requests.get(download_url).json()['browser_download_url']
        else:
            global dlurl
            dlurl = requests.get(download_url, auth=(auth[0],auth[1])).json()['browser_download_url']

        latest_version, latest_description = receive['name'], receive['body']

        return latest_version, dlurl, latest_description
    except Exception as e:
        print(f"릴리스 체크 실패: {e}")
        return None, None, None
    
def download(auth):
    DOWNLOAD_FOLDER = "./temp"
    if not os.path.exists(DOWNLOAD_FOLDER):
        os.makedirs(DOWNLOAD_FOLDER)
        hide_folder_windows(DOWNLOAD_FOLDER)
    print("다운로드 시작")
    latest_version, download_url, latest_description = fetch_latest_release(auth)

    filename = ""
    if platform == "Windows":
        if(os.path.exists(os.path.join(DOWNLOAD_FOLDER, "SubarashiiGame-Windows.zip"))):
            os.remove(os.path.join(DOWNLOAD_FOLDER, "SubarashiiGame-Windows.zip"))
        filename = f"SubarashiiGame-Windows.zip";
    else:
        if(os.path.exists(os.path.join(DOWNLOAD_FOLDER, "SubarashiiGame-macOS.zip"))):
            os.remove(os.path.join(DOWNLOAD_FOLDER, "SubarashiiGame-macOS.zip"))
        filename = f"SubarashiiGame-macOS.zip";
    
    # 파일 다운로드 및 저장
    file_path = os.path.join(DOWNLOAD_FOLDER, filename)
    with requests.get(download_url, stream=True) as file_response:
        file_response.raise_for_status()
        with open(file_path, "wb") as f:
            for chunk in file_response.iter_content(chunk_size=8192):
                f.write(chunk)

    db.setversion(os.path.join(unzippath, "db.db"), latest_version)

def getinfo(auth):
    version, download_url, description = fetch_latest_release(auth)
    return [version, download_url, description]