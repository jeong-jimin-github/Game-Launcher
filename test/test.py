import customtkinter
import os
import sys

try:
    os.chdir(sys._MEIPASS)
    print(sys._MEIPASS)
except:
    os.chdir(os.getcwd())
customtkinter.FontManager.load_font("font/PretendardVariable.ttf")
def center_window(window):
    screen_width = window.winfo_screenwidth()
    screen_height = window.winfo_screenheight()
    x = (screen_width - window.winfo_reqwidth()) // 2
    y = (screen_height - window.winfo_reqheight()) // 2
    window.geometry(f"+{x}+{y}")


# app = customtkinter.CTk()
# app.geometry("300x130")
# center_window(app)
# my_font = customtkinter.CTkFont(family="Pretendard Variable", size=15, weight='normal')

# def no():
#     app.destroy()

# def yes():
#     print("HEllo")

# label = customtkinter.CTkLabel(master=app, text="다운로드가 진행중입니다.\n중단하고 나가시겠습니까?", anchor='center', font=my_font)
# label.place(relx=0.5, rely=0.3, anchor=customtkinter.CENTER)

# button = customtkinter.CTkButton(master=app, text="아니오", command=no, font=my_font, width=70)
# button.place(relx=0.4, rely=0.6, anchor=customtkinter.NE)

# button1 = customtkinter.CTkButton(master=app, text="네", command=yes, font=my_font, width=70)
# button1.place(relx=0.6, rely=0.6, anchor=customtkinter.NW)

# app.mainloop()





app = customtkinter.CTk()
app.geometry("300x130")
center_window(app)
my_font = customtkinter.CTkFont(family="Pretendard Variable", size=15, weight='normal')

def no():
    app.destroy()

def yes():
    print("HEllo")

label = customtkinter.CTkLabel(master=app, text="요청 할당량이 상한을 초과했습니다.\n1시간 후에 다시 시도해 주십시오.", anchor='center', font=my_font)
label.place(relx=0.5, rely=0.3, anchor=customtkinter.CENTER)

button1 = customtkinter.CTkButton(master=app, text="확인", command=yes, font=my_font, width=70)
button1.place(relx=0.5, rely=0.6, anchor=customtkinter.CENTER)

app.mainloop()