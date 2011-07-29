cscript resign.js
..\..\..\output\any\release\bin\AUTOPACKAGE.exe --load=WiX.autopkg

erase *.wixpdb
copy *.msi ..\..\..\repository