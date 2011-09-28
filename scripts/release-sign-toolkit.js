// Include js.js
with(new ActiveXObject("Scripting.FileSystemObject"))for(var x in p=(".;js;scripts;"+WScript.scriptfullname.replace(/(.*\\)(.*)/g,"$1")+";"+new ActiveXObject("WScript.Shell").Environment("PROCESS")("PATH")).split(";"))if(FileExists(j=BuildPath(p[x],"js.js"))){eval(OpenTextFile(j).ReadAll());break}
Use("CoApp");

// sign the rehash binaries
CoApp.SignBinary([CoApp.$RELEASEDIR("CoApp.Rehash.x64.dll"), CoApp.$RELEASEDIR("CoApp.Rehash.x86.dll")]);

// should we re-embed those two dlls into coapp.toolkit?
// we're gonna be a version behind...
CoApp.CopyFiles([CoApp.$RELEASEDIR("CoApp.Rehash.x64.dll"), CoApp.$RELEASEDIR("CoApp.Rehash.x86.dll")], CoApp.$SOLUTIONEXT("\\rehash"));

CoApp.StrongNameBinary([
    CoApp.$RELEASEDIR("CoApp.Toolkit.dll"),
    CoApp.$RELEASEDIR("CoApp.Toolkit.Engine.dll"),
    CoApp.$RELEASEDIR("CoApp.Toolkit.Engine.Client.dll"),
    CoApp.$RELEASEDIR("CoApp.Service.exe"),
    CoApp.$RELEASEDIR("CoApp.exe")
 ]);


/// Every time we do a release-sign, we increment the build number.
var rx, major, minor, build, revision, filename;

/// ---- Developer Tools Pacakge --------------------------------------------------------------------------------------------
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



// update the reference to the CoApp toolkit package in devtools
// this should be getting automatically handled by autopackage. 
/*
if( exists(filename = GetRelativePath( "\\..\\package-scripts\\coapp.devtools.autopkg" )) ) {
    newTxt = ReadAll(filename);

    // link to coapp-toolkit... hmmm.    
    newTxt = newTxt.replace( /add: "coapp.toolkit-.*-any";/ig , 'add: "coapp.toolkit-'+major+'.'+minor+'.'+build+'.'+revision+'-any";' );
    
    WScript.echo("Incrementing Version Attributes in "+filename);
    WriteAll(filename, newTxt);
}


// update the reference to the CoApp toolkit package in trace
if( exists(filename = GetRelativePath( "\\..\\package-scripts\\coapp.trace.autopkg" )) ) {
    newTxt = ReadAll(filename);

    // link to coapp-toolkit... hmmm.    
    newTxt = newTxt.replace( /add: "coapp.toolkit-.*-any";/ig , 'add: "coapp.toolkit-'+major+'.'+minor+'.'+build+'.'+revision+'-any";' );
    
    WScript.echo("Incrementing Version Attributes in "+filename);
    WriteAll(filename, newTxt);
}
*/

