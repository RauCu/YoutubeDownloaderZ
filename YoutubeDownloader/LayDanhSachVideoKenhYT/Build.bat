pip install pyinstaller scrapetube pyperclip openpyxl
del /s /q .\LayDanhSachVideoKenhYT.spec
pyinstaller --onefile .\LayDanhSachVideoKenhYT.py
move /Y dist\LayDanhSachVideoKenhYT.exe ..\LayDanhSachVideoKenhYT.exe
rmdir /s /q .\build 
rmdir /s /q .\dist
del /s /q .\LayDanhSachVideoKenhYT.spec