// Include js.js
with(new ActiveXObject("Scripting.FileSystemObject"))for(var x in p=(".;js;scripts;"+WScript.scriptfullname.replace(/(.*\\)(.*)/g,"$1")+";"+new ActiveXObject("WScript.Shell").Environment("PROCESS")("PATH")).split(";"))if(FileExists(j=BuildPath(p[x],"js.js"))){eval(OpenTextFile(j).ReadAll());break}
Use("CoApp");
var filename;

/// Sign the binaries.
CoApp.StrongNameBinary(CoApp.$RELEASEDIR("detours.x86.dll"));
CoApp.StrongNameBinary(CoApp.$RELEASEDIR("detours.x64.dll"));

// copy them to the solution directory
CoApp.CopyFiles([CoApp.$RELEASEDIR("detours.x86.dll"),CoApp.$RELEASEDIR("detours.x64.dll")], CoApp.$SOLUTIONEXT("\\detours"));
