Set WshShell = WScript.CreateObject("WScript.Shell")
Dim strPath
strPath = "cmd /c C:\Users\derec\OneDrive\Desktop\trabajo.bat"
WshShell.Run strPath, 0, True