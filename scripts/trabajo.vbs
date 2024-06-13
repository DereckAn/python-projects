Set WshShell = WScript.CreateObject("WScript.Shell")
Dim strPath
strPath = "cmd /c C:\Users\derec\OneDrive\Documents\scripts\trabajo.bat"
WshShell.Run strPath, 0, True