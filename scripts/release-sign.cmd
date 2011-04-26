@echo off
:release-sign.cmd
cd %~dp0
start /wait %~dp0\..\tools\simplesigner\simplesigner ..\..\output\any\release\bin\*.exe ..\..\output\any\release\bin\*.dll
start /wait %~dp0\..\tools\simplesigner\simplesigner --sign-only "..\..\output\any\release\bin\detour*.dll"
