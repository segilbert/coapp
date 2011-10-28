// Include js.js
with (new ActiveXObject("Scripting.FileSystemObject")) for (var x in p = (".;js;scripts;" + WScript.scriptfullname.replace(/(.*\\)(.*)/g, "$1") + ";" + new ActiveXObject("WScript.Shell").Environment("PROCESS")("PATH")).split(";")) if (FileExists(j = BuildPath(p[x], "js.js"))) { eval(OpenTextFile(j).ReadAll()); break }
Use("CoApp");

var filename, rx, major, minor, build, revision, newTxt;

/// strong name the .net binaries
CoApp.StrongNameBinary([
    CoApp.$RELEASEDIR("CoApp.Toolkit.dll"),
    CoApp.$RELEASEDIR("CoApp.Toolkit.Engine.dll"),
    CoApp.$RELEASEDIR("CoApp.Toolkit.Engine.Client.dll"),
    CoApp.$RELEASEDIR("CoApp.CCI.dll"),
    CoApp.$RELEASEDIR("CoApp.Developer.Toolkit.dll"),
    CoApp.$RELEASEDIR("CoApp.Service.exe"),
    CoApp.$RELEASEDIR("CoApp.exe"),
    CoApp.$RELEASEDIR("detours.x86.dll"),
    CoApp.$RELEASEDIR("detours.x64.dll"),
    CoApp.$RELEASEDIR("simplesigner.exe"),
    CoApp.$RELEASEDIR("autopackage.exe"),
    CoApp.$RELEASEDIR("Azure.exe"),
    CoApp.$RELEASEDIR("ptk.exe"),
    CoApp.$RELEASEDIR("QuickTool.exe"),
    CoApp.$RELEASEDIR("Scan.exe"),
    CoApp.$RELEASEDIR("Toolscanner.exe"),
    CoApp.$RELEASEDIR("trace.exe"),
    CoApp.$RELEASEDIR("trace-x86.exe"),
    CoApp.$RELEASEDIR("trace-x64.exe")    
]);

CoApp.CopyFiles([
    CoApp.$RELEASEDIR("detours.x86.dll"),
    CoApp.$RELEASEDIR("detours.x64.dll")], 
        CoApp.$SOLUTIONEXT("\\detours"));


/// ---- Developer Tools Pacakge --------------------------------------------------------------------------------------------
filename = CoApp.$SOLUTIONSOURCE("CoApp.Devtools.AssemblyStrongName.cs");

if (newTxt = CoApp.LoadTextFile(filename)) {
    // signed version
    rx = /\[assembly: AssemblyVersion\("(.*)\.(.*)\.(.*)\.(.*)"\)\]/ig.exec(newTxt);
    
    major = parseInt(RegExp.$1.Trim());
    minor = parseInt(RegExp.$2.Trim());
    build = parseInt(RegExp.$3.Trim());
    revision = parseInt(RegExp.$4.Trim())+1;

    if( major < 1 )
        throw  "FAILURE (1)";
    
    newTxt = newTxt.replace( /\[assembly: AssemblyVersion.*/ig , '[assembly: AssemblyVersion("'+major+'.'+minor+'.'+build+'.'+revision+'")]' );
    newTxt = newTxt.replace( /\[assembly: AssemblyFileVersion.*/ig , '[assembly: AssemblyFileVersion("'+major+'.'+minor+'.'+build+'.'+revision+'")]' );

    WScript.echo("Incrementing Version Attributes in " + filename);
    CoApp.SaveTextFile(filename, newTxt);
}

/// ---- Toolkit Pacakge --------------------------------------------------------------------------------------------
filename = CoApp.$SOLUTIONSOURCE("CoApp.Toolkit.AssemblyStrongName.cs");

if (newTxt = CoApp.LoadTextFile(filename)) {
    rx = /\[assembly: AssemblyVersion\("(.*)\.(.*)\.(.*)\.(.*)"\)\]/ig.exec(newTxt); // Get Assembly Version
    
    major = parseInt(RegExp.$1.Trim());
    minor = parseInt(RegExp.$2.Trim());
    build = parseInt(RegExp.$3.Trim());
    revision = parseInt(RegExp.$4.Trim())+1;

    if( major < 1 )
        throw  "FAILURE (1)";
    
    newTxt = newTxt.replace( /\[assembly: AssemblyVersion.*/ig , '[assembly: AssemblyVersion("'+major+'.'+minor+'.'+build+'.'+revision+'")]' );
    newTxt = newTxt.replace( /\[assembly: AssemblyFileVersion.*/ig , '[assembly: AssemblyFileVersion("'+major+'.'+minor+'.'+build+'.'+revision+'")]' );
   
    WScript.echo("Incrementing Version Attributes in "+filename);
    CoApp.SaveTextFile(filename, newTxt);
}