// Include js.js
with(new ActiveXObject("Scripting.FileSystemObject"))for(var x in p=(".;js;scripts;"+new ActiveXObject("WScript.Shell").Environment("PROCESS")("PATH")).split(";"))if(FileExists(j=BuildPath(p[x],"js.js"))){eval(OpenTextFile(j).ReadAll());break}

// WDK Download:
// http://www.microsoft.com/downloads/en/confirmation.aspx?familyId=36a2630f-5d56-43b5-b996-7633f2ec14ff&displayLang=en

// bazaar executable
var $BZR = Assert.Executable("bzr.exe");

// change to the directory where this script is.
var $COAPP_DIR = cd(Assert.Folder($ScriptPath));

//Log File
var $LOGFILENAME = "{$COAPP_DIR}\\script.log";

// checkout projects
Print('Checking out Engine...');
$$('"{$BZR}" branch lp:coapp-engine');

Print('Checking out Bootstrap...');
$$('"{$BZR}" branch lp:coapp-bootstrap');

Print('Checking out Toolkit...');
$$('"{$BZR}" branch lp:coapp-toolkit');

Print('Checking out Cli...');
$$('"{$BZR}" branch lp:coapp-cli');

Print('Checking out Scan tool...');
$$('"{$BZR}" branch lp:coapp-scan');

Print('Checking out mkProject tool...');
$$('"{$BZR}" branch lp:coapp-mkproject');

Print('Checking out mkPackage tool...');
$$('"{$BZR}" branch lp:coapp-mkpackage');

Print('Checking out mkSpec tool...');
$$('"{$BZR}" branch lp:coapp-mkspec');

Print('Checking out SmartManifest tool...');
$$('"{$BZR}" branch lp:coapp-smartmanifest');


function Fail(message) {
    Print("[{$LASTCMD}]");
    Print("---[StdOut]-----------");
    Print( $StdOut.join("\r\n"));
    Print("---[StdError]-----------");
    Print( $StdErr.join("\r\n"));
    WScript.Quit(1);
}