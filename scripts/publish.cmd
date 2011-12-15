@echo off
@setlocal

REM assumes that the binaries have been release built and copied to the binaries submodule

cd %~dp0\..\binaries
call git commit -a -m "updated binaries"
git push 


if exist %~dp0\..\..\devtools\binaries ( 
    cd %~dp0\..\..\devtools\binaries
    git reset --hard HEAD
    git pull 
)