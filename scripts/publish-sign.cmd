@echo off
setlocal
cd %~dp0


cscript //E:JScript publish-sign-detours.js
cscript //E:JScript publish-sign-devtools.js
cscript //E:JScript publish-sign-toolkit.js
cscript //E:JScript publish-sign-trace.js