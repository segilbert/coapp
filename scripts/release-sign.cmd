@echo off
:release-sign.cmd
cd %~dp0
start /wait %~dp0\..\tools\simplesigner\simplesigner ..\..\output\any\release\bin\*.exe ..\..\output\any\release\bin\*.dll
start /wait %~dp0\..\tools\simplesigner\simplesigner --sign-only "..\..\output\any\release\bin\detour*.dll"
start /wait %~dp0\..\tools\simplesigner\simplesigner --sign-only "..\..\output\any\release\bin\bootstrap.exe"

: build package files 
REM cd ..\..\output\any\release\bin\
REM erase *.msi
REM start /wait autopackage --load=coapp.autopkg
REM copy *.msi  %~dp0\..\..\repository\
REM we'll have the sync script copy across msi files that are important to actually release

