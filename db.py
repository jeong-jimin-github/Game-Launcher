import sqlite3
import os

def init(db_path):
    if not os.path.exists(db_path):
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS versions (
                version TEXT
            )
        ''')
        conn.commit()
        conn.close()



def setversion(pa,ver):
    connection = sqlite3.connect(pa)
    cursor = connection.cursor()

    cursor.execute(f"INSERT INTO versions VALUES('{ver}')")

    # 체인지 커밋
    connection.commit()

    connection.close()

def getversion(pa):
    connection = sqlite3.connect(pa)
    cursor = connection.cursor()

    # 가장 최신 버전 가져오기 (단일 값을 원하므로 LIMIT 1 추가 가능)
    cursor.execute("SELECT version FROM versions")

    # 첫 번째 데이터만 가져오기 (fetchone은 튜플을 반환)
    result = cursor.fetchone()

    # 연결을 종료합니다.
    connection.close()

    # 결과가 없는 경우 None 처리
    if result:
        return result[0]  # 튜플에서 첫 번째 값 추출
    else:
        return None  # 데이터가 없을 경우 반환