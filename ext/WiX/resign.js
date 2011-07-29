// Include js.js
with(new ActiveXObject("Scripting.FileSystemObject"))for(var x in p=(".;..\..\scripts;js;scripts;"+new ActiveXObject("WScript.Shell").Environment("PROCESS")("PATH")).split(";"))if(FileExists(j=BuildPath(p[x],"js.js"))){eval(OpenTextFile(j).ReadAll());break}

$MAJOR = $Globals.$MAJOR || 3;
$MINOR = $Globals.$MINOR || 5;
$BUILD = $Globals.$BUILD || 2519;
$REVISION = $Globals.$REVISION || 74;

var $ASSEMBLYVER = "{$MAJOR}.{$MINOR}.{$BUILD}.{$REVISION}";
var $MSIFILE = "DeploymentToolsFoundation-{$ASSEMBLYVER}-any.msi";

var $ILMERGE = Assert.Executable("ilmerge.exe");
var $RESHACK = Assert.Executable("reshacker.exe");
var $RC = Assert.Executable("rc.exe");
var $AL = Assert.Executable("al.exe");
var $SIMPLESIGNER = Assert.Executable("..\\..\\..\\output\\any\\release\\bin\\simplesigner.exe")
var $AUTOPACKAGE= Assert.Executable("..\\..\\..\\output\\any\\release\\bin\\AUTOPACKAGE.exe")

var $COAPP = "..\\..\\..\\";
var $COAPP_SIGNING = "{$COAPP}signing\\";
var $COAPP_KEY = "{$COAPP_SIGNING}coapp-release-public-key.snk";
var $COAPP_PFX = "{$COAPP_SIGNING}coapp-private.pfx";

function WaitFor(filename,maxWaitMsec) {
    filename = FormatArguments(filename, arguments);
    maxWaitMsec = maxWaitMsec || 1000;
    
    while(!exists(filename)) { 
        $$.WScript.Sleep(100);
        maxWaitMsec -= 100;
        if( maxWaitMsec <= 0 ) {
            print("Timed out waiting for [{0}]".Format(filename));
            throw "Timed out waiting for [{0}]".Format(filename);
        }
    }
    $$.WScript.Sleep(100);
}

// automatically update this script for new revision number...
{
    var thisScript = ReadAll($$.WScript.ScriptFullName);
    thisScript = thisScript.replace( /\$REVISION \= \$Globals.\$REVISION .*;/ig , '$REVISION ='     + ' $Globals.$REVISION || '+ ($REVISION +1) + ';');

    var f = $$.fso.OpenTextFile($$.WScript.ScriptFullName, 2, true);
        f.WriteLine(thisScript.Trim());
        f.Close();
        
    var packageXml =  ReadAll("wix.autopkg" );
    packageXml = packageXml.replace( /binding-max: ".*"/ , 'binding-max: "{$MAJOR}.{$MINOR}.{$BUILD}.{$REVISION -1}"' );
    WriteAll( "wix.autopkg", packageXml.Trim()  );
}

var dlls = { 
    "Microsoft.Deployment.WindowsInstaller.dll":"Microsoft.Deployment.WindowsInstaller.orig.dll",
    "Microsoft.Deployment.Compression.dll":"Microsoft.Deployment.Compression.orig.dll",
    "Microsoft.Deployment.Compression.Cab.dll":"Microsoft.Deployment.Compression.Cab.orig.dll",
    "Microsoft.Deployment.Compression.Zip.dll":"Microsoft.Deployment.Compression.Zip.orig.dll",
    "Microsoft.Deployment.Resources.dll":"Microsoft.Deployment.Resources.orig.dll",
    "Microsoft.Deployment.WindowsInstaller.Linq.dll":"Microsoft.Deployment.WindowsInstaller.Linq.orig.dll",
    "Microsoft.Deployment.WindowsInstaller.Package.dll":"Microsoft.Deployment.WindowsInstaller.Package.orig.dll" 
};

$REVISION = $Globals.$REVISION 

// clean up old crap first
for(each in files('.',/\.msi/)) 
    erase(each);
for(each in files('.',/\.new/)) 
    erase(each);
for(each in files('.',/\.wixpdb/)) 
    erase(each);
for(each in files('.',/policy.3.5.*/)) 
    erase(each);
for(each in files('.',/\.rc/)) 
    erase(each);
for(each in files('.',/\.wixpdb/)) 
    erase(each);

for(each in dlls) {
    var targDLL = each;
    var origDLL = dlls[each];
    
    if(!exists( origDLL ) ) { 
        rename( targDLL, origDLL );
    }

    erase( targDLL );
}


if( $Arguments.length > 0 && $Arguments[0] == 'clean' ) {
    print("Cleaned.");
    WScript.Quit();
}

for(each in dlls) {
    var targDLL = each;
    var origDLL = dlls[each];

    print("Rewriting [{0}]", targDLL);
    $$("{$ILMERGE} /ver:{$ASSEMBLYVER} /copyattrs /targetplatform:2 /keyfile:{$COAPP_KEY} /delaysign /out:{0} {1}", targDLL, origDLL);
    erase( targDLL.replace(".dll", ".pdb" ) );
    
    $$("{$RESHACK} -extract {targDLL},{targDLL}.rc,,,");
    WaitFor("{targDLL}.rc",8000);
    
    var txt = ReadAll("{targDLL}.rc");
    
    txt = txt.replace( /FILEVERSION .*,.*,.*,.*/ig , "FILEVERSION {$MAJOR},{$MINOR},{$BUILD},{$REVISION}");
    txt = txt.replace( /PRODUCTVERSION .*,.*,.*,.*/ig , "PRODUCTVERSION {$MAJOR},{$MINOR},{$BUILD},{$REVISION}");
    txt = txt.replace( /VALUE "FileVersion", ".*"/ig , 'VALUE "FileVersion", "{$ASSEMBLYVER}"');
    txt = txt.replace( /VALUE "ProductVersion", ".*"/ig , 'VALUE "ProductVersion", "{$ASSEMBLYVER}"');
    txt = txt.replace( /VALUE "Assembly Version", ".*"/ig , 'VALUE "Assembly Version", "{$ASSEMBLYVER}"');
    
    WriteAll("{targDLL}.rc", txt );
    
    $$("{$RC} /l 00 {targDLL}.rc" );
    
    $$("{$RESHACK} -addoverwrite {targDLL}, {targDLL}.new, {targDLL}.res, VERSIONINFO ,,");
    WaitFor("{targDLL}.new",18000);
    
    erase( targDLL );
    erase( "{targDLL}.res" );
    erase( "{targDLL}.rc" );
    rename( "{targDLL}.new", targDLL);
}
