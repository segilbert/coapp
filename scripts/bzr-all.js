// Include js.js
with(new ActiveXObject("Scripting.FileSystemObject"))for(var x in p=(".;js;scripts;"+new ActiveXObject("WScript.Shell").Environment("PROCESS")("PATH")).split(";"))if(FileExists(j=BuildPath(p[x],"js.js"))){eval(OpenTextFile(j).ReadAll());break} Assert.IsConsole();
// show nothing
WatchExpression = /QUIET_PLEASE/i;
// WatchExpression = /.*/i;
    
// change to the directory where this script is.
var $COAPP_DIR = cd('.');

// bazaar executable
var $BZR = Assert.Executable("bzr.exe");

Print("CoApp bzr-all.js\nRuns bzr commands on a colllection of folders.\n-----------------------------------------------");

if( $Arguments.length == 0 ) {
    Print("Use:\n\n   bzr-all  <command> [arguments...]");
    WScript.Quit();
}
var $Command = $Arguments.shift();
var $CmdString = "";

switch( $Command ) {
    case "status":
    case "commit":
        $CmdString = '"{$BZR}" {$Command} "{each}" ';
        break;
        
    case "push":
        $CmdString = '"{$BZR}" {$Command} --directory {each} ';
        break;
        
    default:
        Print("(only supports status, commit)");
        WScript.Quit();
        break;
}



for(var each in set = directories( '{$COAPP_DIR}', 'coapp-') ) {
    if( each.indexOf('.bzr') > 0 )
        continue;   
    
    // cd('{each}');
    results = $$($CmdString+Collection.ToQuotedString($Arguments) );
    
    if( results.length > 0 ) {
        print('[{each}]-----------------\n{results}\n\n' );    
    }
        
}