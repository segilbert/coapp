@echo off
cd %~dp0

erase *.msi 
erase *.wixpdb

echo Killing old service.
net stop "CoApp Package Installer Service" > NUL
pskill coapp.service > NUL

start ..\output\any\Release\bin\coapp.service.exe --interactive

..\ext\tools\autopackage outercurve.autopkg coapp.toolkit.autopkg  || goto EOF:

for %%v  in (*.msi) do curl -T  %%v http://coapp.org/upload/ || goto EOF:
echo "Uploaded to repository"