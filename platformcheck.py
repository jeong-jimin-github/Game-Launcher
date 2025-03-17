import platform

def os():
    try:
        platformall = platform.platform()
        return platformall.split("-")[0]
    except:
        print("Platform Check Error")