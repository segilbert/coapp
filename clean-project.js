// Include js.js
with(new ActiveXObject("Scripting.FileSystemObject"))for(var x in p=(".;js;scripts;"+new ActiveXObject("WScript.Shell").Environment("PROCESS")("PATH")).split(";"))if(FileExists(j=BuildPath(p[x],"js.js"))){eval(OpenTextFile(j).ReadAll());break}

// change to the directory where this script is.
var $COAPP_DIR = cd(Assert.Folder($ScriptPath));

// remove 'obj' directories
for(var each in set = tree( '{$COAPP_DIR}', /\\obj$/ ) )
    rmdir(set[each]);
    
    
rmdir('{$COAPP_DIR}\\intermediate');
rmdir('{$COAPP_DIR}\\output');
erase('{$COAPP_DIR}\\script.log');
