// Include js.js
with(new ActiveXObject("Scripting.FileSystemObject"))for(var x in p=(".;js;scripts;"+WScript.scriptfullname.replace(/(.*\\)(.*)/g,"$1")+";"+new ActiveXObject("WScript.Shell").Environment("PROCESS")("PATH")).split(";"))if(FileExists(j=BuildPath(p[x],"js.js"))){eval(OpenTextFile(j).ReadAll());break}
Use("CoApp");

// sign the rehash binaries
CoApp.SignBinary([CoApp.$RELEASEDIR("CoApp.Rehash.x64.dll"), CoApp.$RELEASEDIR("CoApp.Rehash.x86.dll")]);

// we embed those two dlls into coapp.toolkit
CoApp.CopyFiles([CoApp.$RELEASEDIR("CoApp.Rehash.x64.dll"), CoApp.$RELEASEDIR("CoApp.Rehash.x86.dll")], CoApp.$SOLUTIONEXT("\\rehash"));
