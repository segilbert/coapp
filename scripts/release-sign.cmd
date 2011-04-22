@echo off
:release-sign.cmd
cd %~dp0
start /wait coapp-simplesigner ..\..\output\any\release\bin\*.exe ..\..\output\any\release\bin\*.dll
start /wait coapp-simplesigner --sign-only "..\..\output\any\release\bin\detour*.dll"
