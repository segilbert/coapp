// Include js.js
with(new ActiveXObject("Scripting.FileSystemObject"))for(var x in p=(".;..\coapp-solution\scripts;js;scripts;"+new ActiveXObject("WScript.Shell").Environment("PROCESS")("PATH")).split(";"))if(FileExists(j=BuildPath(p[x],"js.js"))){eval(OpenTextFile(j).ReadAll());break}

var thisScript = fullpath($$.WScript.ScriptFullName);
var ASNfile = fullpath(thisScript.substring(0,thisScript.lastIndexOf("\\"))+"\\..\\source\\AssemblyStrongName.cs");
var rx, major, minor, build, revision;

if( exists(ASNfile) ) {
    origTxt = ReadAll(ASNfile);
    
    newTxt = origTxt;
    
    // signed version
    rx = /\[assembly: AssemblyVersion\("(.*)\.(.*)\.(.*)\.(.*)"\)\] \/\/SIGNED VERSION/ig;
    rx.exec(newTxt);
    
    major = parseInt(RegExp.$1.Trim());
    minor = parseInt(RegExp.$2.Trim());
    build = parseInt(RegExp.$3.Trim());
    revision = parseInt(RegExp.$4.Trim())+1;

    if( major < 1 )
        throw  "FAILURE (1)";
    
    newTxt = newTxt.replace( /\[assembly: AssemblyVersion.*\/\/SIGNED VERSION/ig , '[assembly: AssemblyVersion("'+major+'.'+minor+'.'+build+'.'+revision+'")] //SIGNED VERSION' );
    newTxt = newTxt.replace( /\[assembly: AssemblyFileVersion.*\/\/SIGNED VERSION/ig , '[assembly: AssemblyFileVersion("'+major+'.'+minor+'.'+build+'.'+revision+'")] //SIGNED VERSION' );
    
    // unsigned version
    rx = /\[assembly: AssemblyVersion\("(.*)\.(.*)\.(.*)\.(.*)"\)\] \/\/UNSIGNED VERSION/ig;
    rx.exec(newTxt);
    
    major = parseInt(RegExp.$1.Trim());
    minor = parseInt(RegExp.$2.Trim());
    build = parseInt(RegExp.$3.Trim());
    revision = parseInt(RegExp.$4.Trim())+1;

    if( major < 1 )
        throw  "FAILURE";
    
    newTxt = newTxt.replace( /\[assembly: AssemblyVersion.*\/\/UNSIGNED VERSION/ig , '[assembly: AssemblyVersion("'+major+'.'+minor+'.'+build+'.'+revision+'")] //UNSIGNED VERSION' );
    newTxt = newTxt.replace( /\[assembly: AssemblyFileVersion.*\/\/UNSIGNED VERSION/ig , '[assembly: AssemblyFileVersion("'+major+'.'+minor+'.'+build+'.'+revision+'")] //UNSIGNED VERSION' );

    if( origTxt.length >1000 && newTxt.length > 1000 ) {
        WScript.echo("Incrementing Version Attributes in "+ASNfile);
        
        var f = $$.fso.OpenTextFile(ASNfile, 2, true);
            f.WriteLine(newTxt);
            f.Close();
    }
}

