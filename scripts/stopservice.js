// Include js.js
with(new ActiveXObject("Scripting.FileSystemObject"))for(var x in p=(".;js;scripts;"+WScript.scriptfullname.replace(/(.*\\)(.*)/g,"$1")+";"+new ActiveXObject("WScript.Shell").Environment("PROCESS")("PATH")).split(";"))if(FileExists(j=BuildPath(p[x],"js.js"))){eval(OpenTextFile(j).ReadAll());break}
try {
    var UNIQUEID = "shutdown";
    var requestPipe = $$.fso.OpenTextFile("\\\\.\\pipe\\CoAppInstaller",2,true);

    // With VBScript/JScript we have to use two pipes. 
    // and tell the server that we don't wanna work with async
    requestPipe.Write('start-session?client=test-script&id='+UNIQUEID+'&async=false');

    // wait for response pipe to open
    var responsePipe;

    // open the response pipe
    for(var numTries = 0; numTries < 10; numTries++) {
        try {
            print("connecting...");
            responsePipe = $$.fso.OpenTextFile("\\\\.\\pipe\\CoAppInstaller-"+UNIQUEID,1,true)
        } catch(err) {
            WScript.Sleep(200);
            continue;
        }
        break;
    }

    if( responsePipe == null ) {
        print("Not Connected!");
    } else {
        print("Connected ---");
        print("making request");
        requestPipe.Write('stop-service');
        var count = 0;
        // we can then do work with the engine directly...
        while(count < 5) {
            response = responsePipe.ReadLine().Trim();
            if( response.indexOf( "keep-alive" ) != 0 ) {
                count = 0;
                print(response);
            } else {
                count++;
            }
        }
    }
} catch( e )  {
    
}