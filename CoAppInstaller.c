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
#include "BootstrapUtility.h"
#include "BootstrapGUI.h"
#include "CoAppEnginePrototypes.h"

#define BUFSIZE				4096
#define SETSTATUSMESSAGE	WM_USER+1
#define SETPROGRESS			WM_USER+2

#define FINISH(text_msg) {LogMessage(text_msg); goto fin;}

LPWSTR CommandLine;

/// <summary>
///     Linked List structure for package processing
/// </summary>
struct package_t {
	wchar_t* name;
	wchar_t* URL;
	wchar_t* localpath;
	struct package_t* next;
};


HMODULE CoAppModule = NULL;
coapp_install_prototype* coapp_install;
coapp_resolve_prototype* coapp_resolve;
coapp_download_prototype* coapp_download;
struct package_t* packageList = NULL;

HANDLE InstallPackagesThread = NULL;
HANDLE DownloadPackagesThread = NULL;
unsigned InstallPackagesThreadId = 0;
unsigned DownloadPackagesThreadId = 0;
BOOL IsShuttingDown = FALSE;
int TaskCount = 0;

void LogMessageInternal( const wchar_t* text ) {
	// actually log the message
	
}

void LogMessage( const wchar_t* format, ... ) {
	va_list args;
	wchar_t* text = (wchar_t*)malloc(BUFSIZE);

	va_start(args, format);
	vswprintf(text,format, args);
	LogMessageInternal(text);
	
	free(text);
}


void Shutdown() {
	IsShuttingDown = TRUE;
	LogMessage(L"Shutting down");
	PostQuitMessage(0);
}

int IsCoAppInstalled( ) {
	// manually load the DLL 
	SetStatusMessage(L"Validating Installer Engine.");

	if( NULL == CoAppModule )  {
		CoAppModule = LoadLibrary( L"coapp-engine" );

		if( NULL == CoAppModule ) {
			//
			// TODO: We should really do somthing with this error.
			//
			// ??? = GetLastError();
			return 0;
		}
		LogMessage(L"CoApp Engine Loaded");

		coapp_install = (coapp_install_prototype*)GetProcAddress( CoAppModule, "coapp_install");
		if( NULL == coapp_install ){
			LogMessage(L"Unable to get locate coapp_install function");
			FreeLibrary( CoAppModule );
			CoAppModule = NULL;
			return 0;
		}

		coapp_resolve = (coapp_resolve_prototype*)GetProcAddress( CoAppModule, "coapp_resolve");
		if( NULL == coapp_resolve ){
			LogMessage(L"Unable to get locate coapp_resolve function");
			FreeLibrary( CoAppModule );
			CoAppModule = NULL;
			return 0;
		}

		coapp_download = (coapp_download_prototype*)GetProcAddress( CoAppModule, "coapp_download");
		if( NULL == coapp_download ){
			LogMessage(L"Unable to get locate coapp_download function");
			FreeLibrary( CoAppModule );
			CoAppModule = NULL;
			return 0;
		}
	}
	SetStatusMessage(L"Installer Engine Validated.");
	return 1;
}

__int32 ResolvePackageHandler(const wchar_t* name, const wchar_t* location, const wchar_t* url) {
	struct package_t* node = (struct package_t*)malloc( sizeof(struct package_t) );
	
	node->name = DuplicateString(name);
	node->localpath = DuplicateString(location);
	node->URL = DuplicateString(url);
	node->next = packageList;

	packageList = node;

	return 0;
}


int DownloadProgressHandler(const wchar_t* current_message, int download_status, __int64 bytes_downloaded, __int64 total_bytes  ) {
	if( IsShuttingDown ) 
		return 1;

	// TODO: set some message

	return 0;
}
unsigned __stdcall DownloadPackages( void* pArguments ){
	
	struct package_t* downloadList = packageList;

	if( IsShuttingDown )
		goto fin;

	while( downloadList ) {
		if( downloadList->localpath == NULL ) {
			downloadList->localpath = TempFileName( L"name" , L".msi" );
			coapp_download( downloadList->URL, downloadList->localpath , DownloadProgressHandler);
		}

		downloadList = downloadList->next;
	}


	fin:
	_endthreadex( 0 );
	DownloadPackagesThread = NULL;
	return 0;
}


unsigned __stdcall InstallPackages( void* pArguments ){
	int tryCount=4;

	SetStatusMessage(L"");
	SetLargeMessageText(L"");
	SetProgressValue( 100 );

	// stage 1: Ensure CoApp is Installed
	while(--tryCount > 0) {
		if( IsShuttingDown )
			goto fin;

		if( !IsCoAppInstalled() ) {
		}
	}

	if( IsShuttingDown )
		goto fin;

	if( !IsCoAppInstalled() ) {
		// we seem to have failed to download an install the CoApp Engine
		// Too Bad
		Shutdown();
	}

	if( IsShuttingDown )
		goto fin;

	
	// stage 2: Get the dependency graph for the package
	coapp_resolve( L"foo.xml" , ResolvePackageHandler);
	
	// stage 3: Start downloading the dependencies
	DownloadPackagesThread = (HANDLE)_beginthreadex(NULL, 0, &DownloadPackages, NULL, 0, &DownloadPackagesThreadId);

	// stage 4: start installing packages as they are available.
	fin:
	_endthreadex( 0 );
	InstallPackagesThread = NULL;
	return 0;
}



int WINAPI wWinMain( HINSTANCE hInstance, HINSTANCE hPrevInstance, LPWSTR pszCmdLine, int nCmdShow) {

	CommandLine = pszCmdLine;

	// start worker thread
	InstallPackagesThread = (HANDLE)_beginthreadex(NULL, 0, &InstallPackages, NULL, 0, &InstallPackagesThreadId);
	
	// show the pretty gui
	return ShowGUI( hInstance);
}