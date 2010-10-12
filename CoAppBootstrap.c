//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

#define _WIN32_WINNT _WIN32_WINNT_WS03 
#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers

#include <SDKDDKVer.h>
#include <windows.h>
#include <Msi.h>
#include <MsiQuery.h>
#include <winhttp.h>
#include <process.h>
#include <wchar.h>
#include <malloc.h>
#include <winhttp.h>
#include <winbase.h>
#include <stdarg.h>
#include <Commctrl.h>

#include "resource.h"
#include "BootstrapGUI.h"
#include "BootstrapUtility.h"


HANDLE ApplicationInstance;
HANDLE WorkerThread = NULL;
unsigned WorkerThreadId = 0;
BOOL IsShuttingDown = FALSE;
LPWSTR CommandLine;

wchar_t* CoAppInstallerPath = NULL;

void Shutdown() {
    IsShuttingDown = TRUE;
    PostQuitMessage(0);
}

int FileExists(const wchar_t* installerPath) {
    WIN32_FILE_ATTRIBUTE_DATA fileData;

    if( installerPath == NULL )
        return 0;

    return GetFileAttributesEx( installerPath, GetFileExInfoStandard, &fileData);
}

int IsCoAppInstalled( ) {
    // check if there is a registry setting for the tiny installer
    CoAppInstallerPath = GetPathFromRegistry();

    if( FileExists(CoAppInstallerPath) )
        return TRUE;

    if(NULL != CoAppInstallerPath)
        free(CoAppInstallerPath);

    // if not, there damn well better be one in the WinSxS assembly
    CoAppInstallerPath = GetWinSxSResourcePathViaManifest((HMODULE)ApplicationInstance, INSTALLER_MANFIEST_ID, L"coapp-installer.exe");
    return (NULL != CoAppInstallerPath);
}
int maxTicks;
int tickValue;

int _stdcall BasicUIHandler(LPVOID pvContext, UINT iMessageType, LPCWSTR szMessage) {
    INSTALLMESSAGE mt;
    UINT uiFlags;
    int value[4];
    int index;
    const wchar_t* pChar;

    ZeroMemory(&value, sizeof(value) );

    if( IsShuttingDown )
        return IDCANCEL;

    if (!szMessage)
        return 0;
    
    mt = (INSTALLMESSAGE)(0xFF000000 & (UINT)iMessageType);
    uiFlags = 0x00FFFFFF & iMessageType;

    switch (mt) {
        case INSTALLMESSAGE_PROGRESS:
            pChar = szMessage;

            while(*pChar) { // real men do real pointer 'rithmatic
                index = *pChar++ - L'1';
        
                while(*pChar < L'0' || *pChar > L'9' )
                    pChar++; // skip up to next number

                while(*pChar >= L'0' && *pChar <= L'9' ) {
                    value[index]*=10;
                    value[index]+=*pChar++-L'0';
                }

                while(*pChar && (*pChar < L'0' || *pChar > L'9' ))
                    pChar++; // skip up to next number	
            }
            switch( value[0] ) {
                case 0: // reset
                    if( maxTicks == 0 && value[1] > 0 ) {
                        maxTicks = value[1];
                        tickValue = 0;
                    }
                    break;
                case 1: // Progress Message
                    break;
                case 2: // Increment
                    if( maxTicks > 0 ){
                        tickValue+=value[1];
                        SetProgressValue(50+ ((tickValue*100/maxTicks)/2) );
                    }
                    break;
                case 3: // customaction
                    break;

            }
            break;
    }
    return IDOK;
}

void doInstallCoApp() {
    wchar_t* coappInstallerMSIFile = TempFileName(L"coapp-install", L"msi");
    SetProgressValue( 25 );
    if( NULL != coappInstallerMSIFile ) {
        SetStatusMessage(L"Downloading Installer Engine");
        DownloadFile( L"http://coapp.org/coapp-engine.msi", coappInstallerMSIFile );
        SetProgressValue( 50 );
        
        SetStatusMessage(L"Installing Engine");
        Sleep(500);
        MsiSetInternalUI( INSTALLUILEVEL_NONE , 0); 
        MsiSetExternalUI( BasicUIHandler, INSTALLLOGMODE_PROGRESS, L"COAPP");
        MsiInstallProduct( coappInstallerMSIFile, NULL);

        free(coappInstallerMSIFile);
    }	
}

int Launch() {
    wchar_t commandLine[32768];

    STARTUPINFO StartupInfo;
    PROCESS_INFORMATION ProcInfo;

    ZeroMemory(&StartupInfo, sizeof(STARTUPINFO) );
    StartupInfo.cb = sizeof( STARTUPINFO );
    wsprintf( commandLine, L"\"%s\" %s", CoAppInstallerPath, CommandLine);

    CreateProcess( CoAppInstallerPath, commandLine, NULL, NULL, TRUE, 0, NULL, NULL, &StartupInfo, &ProcInfo );
    
    ExitProcess(0);
    return 0;
}

unsigned __stdcall InstallCoApp( void* pArguments ){
    while(!Ready)
        Sleep(300);

    SetStatusMessage(L"");
    SetLargeMessageText(L"Installing CoApp...");
    SetProgressValue( 10 );

    if( IsShuttingDown )
        goto fin;

    doInstallCoApp();

    if( IsShuttingDown )
        goto fin;

    if( IsCoAppInstalled() ) {
        SetProgressValue( 100 );
        Launch();
    }

fin:
    ExitProcess(0);
    _endthreadex( 0 );
    WorkerThread = NULL;
    
    return 0;
}

int WINAPI wWinMain( HINSTANCE hInstance, HINSTANCE hPrevInstance, LPWSTR pszCmdLine, int nCmdShow) {
    
    INITCOMMONCONTROLSEX iccs;
    CommandLine = pszCmdLine;
    ApplicationInstance = hInstance;

    // load comctl32 v6, in particular the progress bar class
    iccs.dwSize = sizeof(INITCOMMONCONTROLSEX); // Naughty! :)
    iccs.dwICC  = ICC_PROGRESS_CLASS;
    InitCommonControlsEx(&iccs);

    // check for CoApp 
    if( IsCoAppInstalled() ) 
        return Launch();

    // not there? install it.--- start worker thread
    WorkerThread = (HANDLE)_beginthreadex(NULL, 0, &InstallCoApp, NULL, 0, &WorkerThreadId);

    // And, show the GUI
    return ShowGUI(hInstance);
}