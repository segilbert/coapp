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


HMODULE CoAppModule = NULL;

HANDLE WorkerThread = NULL;
unsigned WorkerThreadId = 0;
BOOL IsShuttingDown = FALSE;
LPWSTR CommandLine;

void Shutdown() {
	IsShuttingDown = TRUE;
	PostQuitMessage(0);
}

int IsCoAppInstalled( ) {
	// manually load the DLL 
	if( NULL == CoAppModule )  {
		CoAppModule = LoadLibrary( L"coapp-engine" );

		if( NULL == CoAppModule ) {
			return 0;
		}
	}
	return 1;
}

void doInstallCoApp() {
	wchar_t* coappInstallerMSIFile = TempFileName(L"coapp-install", L"msi");
	
	if( NULL != coappInstallerMSIFile ) {
		SetStatusMessage(L"Downloading Installer Engine");
		DownloadFile( L"http://coapp.org/coapp-engine.msi", coappInstallerMSIFile );

		SetStatusMessage(L"Installing Installer Engine");
		MsiInstallProduct( coappInstallerMSIFile, NULL );
		free(coappInstallerMSIFile);
	}	
}

int Launch() {
	return 0;
}

unsigned __stdcall InstallCoApp( void* pArguments ){
	int tryCount=3;

	SetStatusMessage(L"");
	SetLargeMessageText(L"Installing CoApp...");
	SetProgressValue( 10 );

	// Ensure CoApp is Installed
	do{
		if( IsShuttingDown )
			goto fin;

		doInstallCoApp();

		if( IsCoAppInstalled() ) {
			Launch();
			break;
		}
	} while( --tryCount > 0);


fin:
	_endthreadex( 0 );
	WorkerThread = NULL;
	return 0;
}



int WINAPI wWinMain( HINSTANCE hInstance, HINSTANCE hPrevInstance, LPWSTR pszCmdLine, int nCmdShow) {
	CommandLine = pszCmdLine;

	// check for CoApp 
	if( IsCoAppInstalled() ) 
		return Launch();

	// not there? install it.--- start worker thread
	WorkerThread = (HANDLE)_beginthreadex(NULL, 0, &InstallCoApp, NULL, 0, &WorkerThreadId);

	// And, show the GUI
	return ShowGUI(hInstance);
}