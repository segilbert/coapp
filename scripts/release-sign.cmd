@echo off
:release-sign.cmd
cd %~dp0
start /wait coapp-simplesigner ..\..\output\any\release\bin\*.exe ..\..\output\any\release\bin\*.dll