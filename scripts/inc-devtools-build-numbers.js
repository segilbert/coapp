// Include js.js
with(new ActiveXObject("Scripting.FileSystemObject"))for(var x in p=(".;js;scripts;"+WScript.scriptfullname.replace(/(.*\\)(.*)/g,"$1")+";"+new ActiveXObject("WScript.Shell").Environment("PROCESS")("PATH")).split(";"))if(FileExists(j=BuildPath(p[x],"js.js"))){eval(OpenTextFile(j).ReadAll());break}

Use("MD5");

function WriteAll( filename, text ) {
    var f = $$.fso.OpenTextFile(filename, 2, true);
    f.WriteLine(text.Trim());
    f.Close();
}

function GetRelativePath(filename)  {
    var thisScript = fullpath($$.WScript.ScriptFullName);
    return fullpath(thisScript.substring(0,thisScript.lastIndexOf("\\"))+filename);
}

var rx, major, minor, build, revision;
var filename;

/// ---- Developer Tools Pacakge --------------------------------------------------------------------------------------------
if( exists(filename = GetRelativePath( "\\..\\source\\CoApp.Devtools.AssemblyStrongName.cs" )) ) {
    origTxt = ReadAll(filename);
    newTxt = origTxt;
    
    // signed version
    rx = /\[assembly: AssemblyVersion\("(.*)\.(.*)\.(.*)\.(.*)"\)\]/ig;
    rx.exec(newTxt);
    
    major = parseInt(RegExp.$1.Trim());
    minor = parseInt(RegExp.$2.Trim());
    build = parseInt(RegExp.$3.Trim());
    revision = parseInt(RegExp.$4.Trim())+1;

    if( major < 1 )
        throw  "FAILURE (1)";
    
    newTxt = newTxt.replace( /\[assembly: AssemblyVersion.*/ig , '[assembly: AssemblyVersion("'+major+'.'+minor+'.'+build+'.'+revision+'")]' );
    newTxt = newTxt.replace( /\[assembly: AssemblyFileVersion.*/ig , '[assembly: AssemblyFileVersion("'+major+'.'+minor+'.'+build+'.'+revision+'")]' );
    
   
    if( origTxt.length >1000 && newTxt.length > 1000 ) {
        WScript.echo("Incrementing Version Attributes in "+filename);
        WriteAll( filename, newTxt );
    }
}

