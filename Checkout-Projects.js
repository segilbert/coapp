// Include js.js
with(new ActiveXObject("Scripting.FileSystemObject"))for(var x in p=(".;js;scripts;"+new ActiveXObject("WScript.Shell").Environment("PROCESS")("PATH")).split(";"))if(FileExists(j=BuildPath(p[x],"js.js"))){eval(OpenTextFile(j).ReadAll());break}

// dump everything to console
var WatchExpression = /.*/i;

// bazaar executable
var BZR = Assert.Executable("bzr.exe");

// checkout projects
$$('{BZR} branch lp:coapp-engine');
$$('{BZR} branch lp:coapp-cli');
$$('{BZR} branch lp:coapp-toolkit');

