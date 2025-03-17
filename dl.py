import requests
from bs4 import BeautifulSoup
import os
import subprocess
import platformcheck

# Gogs 릴리스 URL
GOGS_RELEASES_URL = "http://112.147.160.124:8080/Kuuhaku/Unity-Game/releases"
BASE_URL = "http://112.147.160.124:8080"  # Gogs 서버의 도메인

def hide_folder_windows(folder_path):
    subprocess.run(["attrib", "+h", folder_path], shell=True)

def fetch_latest_release():
    """
    Gogs 릴리스 페이지에서 최신 릴리스의 이름, 다운로드 URL, 그리고 변경사항을 가져옵니다.
    """
    try:
        # Gogs 페이지 요청
        response = requests.get(GOGS_RELEASES_URL)
        response.raise_for_status()

        # BeautifulSoup으로 HTML 파싱
        soup = BeautifulSoup(response.content, "html.parser")

        # 최신 릴리스 이름 찾기
        release_tag = soup.find("span", {"class": "tag text blue"})
        if not release_tag:
            raise Exception("No releases found.")
        latest_version = release_tag.text.strip()  # 릴리스 버전 (예: 0.0.5)

        # 최신 릴리스 변경사항 (description)
        desc_container = soup.find("div", {"class": "markdown desc"})
        latest_description = desc_container.text.strip() if desc_container else "No description available."

        # 최신 릴리스 다운로드 링크 찾기 ("Windows" 텍스트 포함) - 새로운 로직
        download_list = soup.find_all("li")
        download_url = None
        if platformcheck.os() == "Windows":
            for item in download_list:
                link = item.find("a")
                if link and "Windows" in link.text:  # 파일명이 "Windows"를 포함하는지 확인
                    download_url = BASE_URL + link["href"]  # 상대 경로 -> 절대 경로 변환
                    break
        else:
            for item in download_list:
                link = item.find("a")
                if link and "macOS" in link.text:  # 파일명이 "Windows"를 포함하는지 확인
                    download_url = BASE_URL + link["href"]  # 상대 경로 -> 절대 경로 변환
                    break

        if not download_url:
            raise Exception("No Windows release found.")

        return latest_version, download_url, latest_description
    except Exception as e:
        print(f"Error fetching release information: {e}")
        return None, None, None
    
def download():
    # 다운로드 경로
    DOWNLOAD_FOLDER = "./temp"
    if not os.path.exists(DOWNLOAD_FOLDER):
        os.makedirs(DOWNLOAD_FOLDER)
        hide_folder_windows(DOWNLOAD_FOLDER)
    print("Download Start")
    download_url = fetch_latest_release()[1]

    filename = f"";
    if platformcheck.os() == "Windows":
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

    return file_path

def getinfo():
    version, download_url, description = fetch_latest_release()
    return [version, download_url, description]