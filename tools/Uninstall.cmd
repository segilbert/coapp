@echo off
setlocal
cd %~dp0
c:\Windows\Microsoft.NET\Framework\v2.0.50727\RegAsm.exe /unregister /codebase XsdTool.dll
endlocal