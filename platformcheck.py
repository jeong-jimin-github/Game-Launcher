import platform

def os():
    try:
        platformall = str(platform.platform()).split("-")[0]
        return platformall
    except:
        print("Platform Check Error")