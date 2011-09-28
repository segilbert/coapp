@echo off
setlocal
cd %~dp0

cscript //E:JScript release-sign-bootstrap.js
cscript //E:JScript release-sign-detours.js
cscript //E:JScript release-sign-devtools.js
cscript //E:JScript release-sign-toolkit.js
cscript //E:JScript release-sign-trace.js