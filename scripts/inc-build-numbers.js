// Include js.js
with(new ActiveXObject("Scripting.FileSystemObject"))for(var x in p=(".;..\coapp-solution\scripts;js;scripts;"+new ActiveXObject("WScript.Shell").Environment("PROCESS")("PATH")).split(";"))if(FileExists(j=BuildPath(p[x],"js.js"))){eval(OpenTextFile(j).ReadAll());break}

Use("MD5");

var thisScript = fullpath($$.WScript.ScriptFullName);
var ASNfile = fullpath(thisScript.substring(0,thisScript.lastIndexOf("\\"))+"\\..\\source\\AssemblyStrongName.cs");
var rx, major, minor, build, revision;

if( exists(ASNfile) ) {
    origTxt = ReadAll(ASNfile);
    newTxt = origTxt;
    
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
    
   
    if( origTxt.length >1000 && newTxt.length > 1000 ) {
        WScript.echo("Incrementing Version Attributes in "+ASNfile);
        
        var f = $$.fso.OpenTextFile(ASNfile, 2, true);
            f.WriteLine(newTxt);
            f.Close();
    }
}

var installerManifest = fullpath(thisScript.substring(0,thisScript.lastIndexOf("\\"))+"\\..\\..\\coapp-interim-stub\\coapp.installer.manifest");
var installerWix = fullpath(thisScript.substring(0,thisScript.lastIndexOf("\\"))+"\\..\\..\\coapp-interim-stub\\coapp.installer.wxs");
var installerPolicy = fullpath(thisScript.substring(0,thisScript.lastIndexOf("\\"))+"\\..\\..\\coapp-interim-stub\\policy.1.0.coapp.installer.manifest");
var mmbr = ''+major+'.'+minor+'.'+build+'.'+revision;
var range = '1.0.0.0-'+major+'.'+minor+'.'+build+'.'+(revision-1);
    
if( exists(installerManifest) ) {

    print("Incrementing build number data in {0}",installerManifest);
    
    origTxt = ReadAll(installerManifest);
    newTxt = origTxt;

    newTxt = newTxt.replace( /assemblyIdentity type="win32" name="coapp.installer" version=".*" processorArchitecture="x86"/ig , 'assemblyIdentity type="win32" name="coapp.installer" version="'+mmbr+'" processorArchitecture="x86"' );
    
    var f = $$.fso.OpenTextFile(installerManifest, 2, true);
    f.WriteLine(newTxt);
    f.Close();

    print("Incrementing build number data in {0}",installerWix);
    
    origTxt = ReadAll(installerWix);
    newTxt = origTxt;

    newTxt = newTxt.replace( /Product Id=".*" Name="CoApp Installer" Language="1033" Manufacturer="Outercurve Foundation .CoApp Project." Version=".*"/ig , 'Product Id="*" Name="CoApp Installer" Language="1033" Manufacturer="Outercurve Foundation (CoApp Project)" Version="'+mmbr+'"' );
    newTxt = newTxt.replace( /Component Id="coappinstaller_component" Guid=".*"/ig , 'Component Id="coappinstaller_component" Guid="'+GUID('coappinstaller_component'+mmbr)+'"' );
    newTxt = newTxt.replace( /Component Id="coappinstaller_policy_component" Guid=".*"/ig , 'Component Id="coappinstaller_policy_component" Guid="'+GUID('coappinstaller_policy_component'+mmbr)+'"' );
    
    var f = $$.fso.OpenTextFile(installerWix, 2, true);
    f.WriteLine(newTxt);
    f.Close();

    print("Incrementing build number data in {0}",installerPolicy);
    
    origTxt = ReadAll(installerPolicy);
    newTxt = origTxt;

    newTxt = newTxt.replace( /assemblyIdentity type="win32-policy" publicKeyToken="820d50196d4e8857" name="policy.1.0.coapp.installer" version=".*" processorArchitecture="x86"/ig , 'assemblyIdentity type="win32-policy" publicKeyToken="820d50196d4e8857" name="policy.1.0.coapp.installer" version="'+mmbr+'" processorArchitecture="x86"' );
    newTxt = newTxt.replace( /bindingRedirect oldVersion=".*" newVersion=".*"/ig , 'bindingRedirect oldVersion="'+range+'" newVersion="'+mmbr+'"' );
    
    var f = $$.fso.OpenTextFile(installerPolicy, 2, true);
    f.WriteLine(newTxt);
    f.Close();
}

var bootstrapManifest = fullpath(thisScript.substring(0,thisScript.lastIndexOf("\\"))+"\\..\\..\\coapp-bootstrap\\BootstrapInstaller.manifest.xml");
if( exists(bootstrapManifest) ) {
 print("Incrementing build number data in {0}",bootstrapManifest);
    
    origTxt = ReadAll(bootstrapManifest);
    newTxt = origTxt;

    newTxt = newTxt.replace( /assemblyIdentity type="win32" name="coapp.installer" version=".*" publicKeyToken="820d50196d4e8857"/ig , 'assemblyIdentity type="win32" name="coapp.installer" version="'+mmbr+'" publicKeyToken="820d50196d4e8857"' );
    
    var f = $$.fso.OpenTextFile(bootstrapManifest, 2, true);
    f.WriteLine(newTxt);
    f.Close();
}