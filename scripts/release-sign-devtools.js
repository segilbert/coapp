// Include js.js
with(new ActiveXObject("Scripting.FileSystemObject"))for(var x in p=(".;js;scripts;"+WScript.scriptfullname.replace(/(.*\\)(.*)/g,"$1")+";"+new ActiveXObject("WScript.Shell").Environment("PROCESS")("PATH")).split(";"))if(FileExists(j=BuildPath(p[x],"js.js"))){eval(OpenTextFile(j).ReadAll());break}
Use("CoApp");

/// Sign the SimpleSigner with itself.
print("Signing SimpleSigner itself");
$$('"{0}" "{0}"', CoApp.$RELEASEDIR("simplesigner.exe"));
if( $ERRORLEVEL ) {
    // let's fall back to the old version, perhaps someone has already done one of the dependencies.
    var so = $StdOut; 
    var se = $StdErr;
    CoApp.StrongNameBinary(CoApp.$RELEASEDIR("simplesigner.exe"));
    if( $ERRORLEVEL ) {
        for (each in so) { print(so[each]); }
        for (each in se) { print(se[each]); }
        
        for (each in $StdOut) { print($StdOut[each]); }
        for (each in $StdErr) { print($StdErr[each]); }
        
        throw new "Failed to strong-name simplesigner";
    }
} 

print("Updating solution's copy of SimpleSigner");
// copy the simplesigner and its dependencies to the solution tools folder.
CoApp.CopyFiles([
    CoApp.$RELEASEDIR("simplesigner.exe"), 
    CoApp.$RELEASEDIR("CoApp.Toolkit.dll"), 
    CoApp.$RELEASEDIR("CoApp.Developer.Toolkit.dll"),
    CoApp.$RELEASEDIR("CoApp.CCI.dll")], CoApp.$SOLUTIONTOOLS("\\simplesigner") ); 

// strong name everything else.
print("Strong naming everything else");
CoApp.StrongNameBinary([
    // CoApp.$RELEASEDIR("autopackage.exe"),
    CoApp.$RELEASEDIR("Azure.exe"),
    CoApp.$RELEASEDIR("CoApp.CCI.dll"),
    CoApp.$RELEASEDIR("CoApp.Developer.Toolkit.dll"),
    CoApp.$RELEASEDIR("ptk.exe"),
    CoApp.$RELEASEDIR("QuickTool.exe"),
    CoApp.$RELEASEDIR("Scan.exe"),
    // CoApp.$RELEASEDIR("TestPackageMaker.exe"),
    CoApp.$RELEASEDIR("Toolscanner.exe") 
 ]);


/*
print("Making All-in-one EXE for Azure.exe (az.exe)");
// make the all-in-one binary for Azure (az.exe)
// gotta remove the pdbs for azure and coapp.toolkit
erase( CoApp.$RELEASEDIR("Az.exe")  );
erase( CoApp.$RELEASEDIR("Azure.pdb")  );
erase( CoApp.$RELEASEDIR("CoApp.Toolkit.pdb")  );
$$('ilmerge /v4 /t:exe "/out:{0}" "{1}" "{2}" "{3}"', CoApp.$RELEASEDIR("Az.exe"), CoApp.$RELEASEDIR("Azure.exe"), CoApp.$RELEASEDIR("CoApp.Toolkit.dll"), CoApp.$RELEASEDIR("Microsoft.WindowsAzure.Storageclient.dll") );
print('ilmerge /v4 /t:exe "/out:{0}" "{1}" "{2}" "{3}"', CoApp.$RELEASEDIR("Az.exe"), CoApp.$RELEASEDIR("Azure.exe"), CoApp.$RELEASEDIR("CoApp.Toolkit.dll"), CoApp.$RELEASEDIR("Microsoft.WindowsAzure.Storageclient.dll") );
// no sweat if that didn't work ;)
CoApp.StrongNameBinary(CoApp.$RELEASEDIR("Az.exe"));
*/


// no release/publish step at this point...

/// ****
/// Update the source code with a new version number for the next build
/// ****
var rx, major, minor, build, revision, filename;

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

