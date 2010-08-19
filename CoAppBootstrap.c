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

#define BUFSIZE 4096
#define SETSTATUSMESSAGE WM_USER+1
#define SETPROGRESS WM_USER+2

#define FINISH(text_msg) {LogMessage(text_msg); goto fin;}

// prototypes for function pointers
typedef __int32 (CoAppResolveCallback)(const wchar_t* name, const wchar_t* location,const wchar_t* url);
typedef __int32 (coapp_install_prototype)(const wchar_t* package_path);
typedef __int32 (coapp_resolve_prototype)(const wchar_t* package_path, CoAppResolveCallback* callback );
typedef __int32 (coapp_download_prototype)(const wchar_t* package_url);

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

HWND hDialog = 0;
HANDLE installerThread = NULL;
HANDLE downloadThread = NULL;
unsigned installerThreadId = 0;
unsigned downloadThreadId = 0;
BOOL shuttingDown = FALSE;
int numberOfTasks = 0;

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

void SetStatusMessage(  const wchar_t* format, ... ) {
	va_list args;
	wchar_t* text = (wchar_t*)malloc(BUFSIZE);

	va_start(args, format);
	vswprintf(text,format, args);
	LogMessageInternal(text);

	// recipient must free the text buffer!
	PostMessage(hDialog, SETSTATUSMESSAGE, 0, (LPARAM)text );
}

wchar_t* DuplicateString( const wchar_t* text ) {
	int size;
	wchar_t* result = NULL;
	
	if(text) {
		size = wcslen(text);
		result = (wchar_t*)malloc(BUFSIZE);
		wcsncpy_s(result , BUFSIZE, text, size );
	}
	return result;
}

void SetProgressValue( int percentage ) {
	PostMessage(hDialog, SETPROGRESS, (WPARAM)percentage,0 );
}

void Shutdown() {
	shuttingDown = TRUE;
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

///
/// <summary> 
///		creates a temporary name for a file 
///		caller must free the memory for the string returned.
///		returns NULL on error.
/// </summary>
 wchar_t* TempFileName(wchar_t* name,wchar_t* extension) {
	DWORD returnValue = 0;
	wchar_t tempFolderPath[BUFSIZE];
	wchar_t* result = (wchar_t*)malloc(BUFSIZE);

	returnValue = GetTempPath(BUFSIZE,  tempFolderPath); 

    if (returnValue > BUFSIZE || (returnValue == 0)) {
		free( result );
		return NULL;
    }

	// grab a unique file name to write the MSI to.
	wsprintf( result, L"%s\\%s[%d].%s", tempFolderPath, name , GetTickCount(), extension );

	LogMessage(L"Temporary filename: %s", result);

	return result;
}

///
/// <summary> 
///		Downloads a file from a URL 
/// </summary>
void DownloadFile(wchar_t* URL, wchar_t* destinationFilename) {

	URL_COMPONENTS urlComponents;

	wchar_t urlPath[BUFSIZE];
	wchar_t urlHost[BUFSIZE];

	void* pszOutBuffer;

    HINTERNET  hSession = NULL;
	HINTERNET  hConnect = NULL;
    HINTERNET  hRequest = NULL;
	DWORD dwDownloaded = 0;
	DWORD dwSize = 0;
	DWORD dwBytesWritten = 0;
	HANDLE hFile;

	ZeroMemory(&urlComponents, sizeof(urlComponents));
    urlComponents.dwStructSize = sizeof(urlComponents);

	urlComponents.dwSchemeLength    = -1;
    urlComponents.dwHostNameLength  = -1;
    urlComponents.dwUrlPathLength   = -1;
    urlComponents.dwExtraInfoLength = -1;

	if(!WinHttpCrackUrl(URL, (DWORD)wcslen(URL), 0, &urlComponents))
		FINISH( L"URL not valid" );

	wcsncpy_s( urlHost , BUFSIZE, URL+urlComponents.dwSchemeLength+3 ,urlComponents.dwHostNameLength );
	wcsncpy_s( urlPath , BUFSIZE, URL+urlComponents.dwSchemeLength+urlComponents.dwHostNameLength+3, urlComponents.dwUrlPathLength );

	// Use WinHttpOpen to obtain a session handle.
    if(!(hSession = WinHttpOpen( L"CoAppBootstrapper/1.0",  WINHTTP_ACCESS_TYPE_DEFAULT_PROXY,WINHTTP_NO_PROXY_NAME, WINHTTP_NO_PROXY_BYPASS, 0)))
		FINISH( L"Unable to create session for download" );

	// Specify an HTTP server.
    if (!(hConnect = WinHttpConnect( hSession, urlHost, urlComponents.nPort, 0)))
		FINISH( L"Unable to connect to URL for download" );

	// Create an HTTP request handle.
    if (!(hRequest = WinHttpOpenRequest( hConnect, L"GET",urlPath , NULL, WINHTTP_NO_REFERER,  WINHTTP_DEFAULT_ACCEPT_TYPES, 0)))
		FINISH( L"Unable to open request for download" );

    // Send a request.
    if(!(WinHttpSendRequest( hRequest, WINHTTP_NO_ADDITIONAL_HEADERS, 0, WINHTTP_NO_REQUEST_DATA, 0, 0, 0)))
		FINISH( L"Unable to send request for download" );
 
    // End the request.
    if(!(WinHttpReceiveResponse( hRequest, NULL)))
		FINISH( L"Unable to receive response for download" );

	if( INVALID_HANDLE_VALUE == (hFile = CreateFile(destinationFilename, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS,  FILE_ATTRIBUTE_NORMAL,NULL)))
		FINISH( (L"Unable to create output file [%s]",destinationFilename ));

    // Keep checking for data until there is nothing left.
    do  {
        // Check for available data.
        dwSize = 0;

        if (!WinHttpQueryDataAvailable( hRequest, &dwSize))
			FINISH( L"No data available");
            
        // No more available data.
        if (!dwSize)
            break;

        // Allocate space for the buffer.
        pszOutBuffer = malloc(dwSize+1);
        if (!pszOutBuffer) 
			FINISH( L"Allocation Failure" );
            
        // Read the Data.
        ZeroMemory(pszOutBuffer, dwSize+1);

        if (!WinHttpReadData( hRequest, (LPVOID)pszOutBuffer, dwSize, &dwDownloaded)) 
            FINISH( L"ReadData Failure" )
		else 
			WriteFile( hFile, pszOutBuffer, dwSize, &dwBytesWritten, NULL ); 
        
        // Free the memory allocated to the buffer.
        free(pszOutBuffer);

        // This condition should never be reached since WinHttpQueryDataAvailable
        // reported that there are bits to read.
        if (!dwDownloaded)
            break;
                
    } while (dwSize > 0);

	fin:
	// Close any open handles.
	if (hFile)
		CloseHandle( hFile );
    if (hRequest) 
		WinHttpCloseHandle(hRequest);
    if (hConnect) 
		WinHttpCloseHandle(hConnect);
    if (hSession) 
		WinHttpCloseHandle(hSession);

}

void InstallCoApp() {
	wchar_t* coappInstallerMSIFile = TempFileName(L"coapp-install", L"msi");
	
	if( NULL != coappInstallerMSIFile ) {
		SetStatusMessage(L"Downloading Installer Engine");
		DownloadFile( L"http://coapp.org/coapp-engine.msi", coappInstallerMSIFile );

		SetStatusMessage(L"Installing Installer Engine");
		MsiInstallProduct( coappInstallerMSIFile, NULL );
		free(coappInstallerMSIFile);
    }
}

__int32 ResolvePackageCallback(const wchar_t* name, const wchar_t* location, const wchar_t* url) {
	struct package_t* node = (struct package_t*)malloc( sizeof(struct package_t) );
	
	node->name = DuplicateString(name);
	node->localpath = DuplicateString(location);
	node->URL = DuplicateString(url);
	node->next = packageList;

	packageList = node;

	return 0;
}

BOOL CALLBACK DialogProc (HWND hwnd,  UINT message, WPARAM wParam,  LPARAM lParam) {
    HDC hdcStatic;
	
    switch (message) {
		case SETSTATUSMESSAGE: 
			SendMessage( GetDlgItem( hwnd, IDC_STATICTEXT1), WM_SETTEXT, wParam, lParam );
			free((void*)lParam); // caller allocated the message, we need to free.
		break;

		case SETPROGRESS: 
			SendMessage( GetDlgItem( hwnd, IDC_PROGRESS2), PBM_SETPOS,  wParam, lParam );
		break;
		
		case WM_CTLCOLORSTATIC: {

			hdcStatic = (HDC) wParam;

			// SetTextColor(hdcStatic, RGB(255,255,255));
			if( lParam == (LPARAM)GetDlgItem( hwnd, IDC_STATICTEXT1) ||  lParam == (LPARAM)GetDlgItem( hwnd, IDC_STATICTEXT2) ) {
				SetBkColor(hdcStatic, RGB(255,255,255));
				return (INT_PTR)CreateSolidBrush(RGB(255,255,255));
			}
			if( lParam == (LPARAM)GetDlgItem( hwnd, IDC_STATICTEXT3 )  ) {
				SetBkMode(hdcStatic , TRANSPARENT );
			}

			return (INT_PTR)GetStockObject(NULL_BRUSH);
        }

		case WM_INITDIALOG:
			return TRUE;
			break;
		case WM_COMMAND:
			PostQuitMessage(0);
			return TRUE;
		case WM_DESTROY:
			PostQuitMessage(0);
			return TRUE;
		case WM_CLOSE:
			DestroyWindow (hwnd);
			return TRUE;
    }
    return FALSE;
}

unsigned __stdcall DownloadThread( void* pArguments ){
	
	if( shuttingDown )
		goto fin;



	fin:
	_endthreadex( 0 );
	downloadThread = NULL;
	return 0;
}


unsigned __stdcall InstallerThread( void* pArguments ){
	int i=4;

	SetStatusMessage(L"");
	SetProgressValue( 100 );
	// stage 1: Ensure CoApp is Installed
	while(--i > 0) {
		if( shuttingDown )
			goto fin;

		if( !IsCoAppInstalled() ) {
			InstallCoApp();
		}
	}

	if( shuttingDown )
		goto fin;

	if( !IsCoAppInstalled() ) {
		// we seem to have failed to download an install the CoApp Engine
		// Too Bad
		Shutdown();
	}

	if( shuttingDown )
		goto fin;

	
	// stage 2: Get the dependency graph for the package
	coapp_resolve( L"foo.xml" , ResolvePackageCallback);
	
	/*
	// stage 3: Start downloading the dependencies
	downloadThread = (HANDLE)_beginthreadex(NULL, 0, &DownloadThread, NULL, 0, &downloadThreadId);

	// stage 4: start installing packages as they are available.
	*/
	fin:
	_endthreadex( 0 );
	installerThread = NULL;
	return 0;
}

void SetLargeMessageText(const wchar_t* ps_text) {
	POINT p;
	RECT rect;
	HWND u_hWnd = GetDlgItem(hDialog,IDC_STATICTEXT3);

	SetWindowText(u_hWnd, ps_text);
	
	GetWindowRect(u_hWnd, &rect);
	p.x=rect.left; p.y=rect.top;
	ScreenToClient(hDialog, &p);
	rect.left = p.x; rect.top = p.y;
	p.x=rect.right; p.y=rect.bottom;
	ScreenToClient(hDialog, &p);
	rect.right = p.x; rect.bottom = p.y;
	RedrawWindow(hDialog, &rect, NULL, RDW_INVALIDATE);
}

int WINAPI WinMain( HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR pszCmdLine, int nCmdShow) {

	MSG  msg;
	int status;
	int i;
	HWND hImg;
	HBITMAP hBitmap ;

	HANDLE hFont=CreateFont (22, 0, 0, 0, FW_DONTCARE, FALSE, FALSE, FALSE, ANSI_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, DEFAULT_PITCH | FF_SWISS, L"Calibri");
  
	hDialog = CreateDialog( hInstance, MAKEINTRESOURCE(IDD_DIALOG1), NULL, DialogProc );
	
	hBitmap = LoadBitmap(hInstance, MAKEINTRESOURCE(IDB_BITMAP_GRADIENT));
	for(i=0;i<40;i++) {
		hImg = CreateWindowEx(0, L"STATIC", L"", WS_CHILD | SS_BITMAP | WS_VISIBLE, 0,i,260,1,hDialog, (HMENU)100+i, hInstance , NULL);
		SendMessage(hImg, STM_SETIMAGE, (WPARAM)IMAGE_BITMAP,(LPARAM)hBitmap);

	}

	// LTEXT           " ",IDC_STATICTEXT3,7,7,240,16
	hImg = CreateWindowEx(0, L"STATIC", L"SAMPLE TEXT", WS_CHILD | WS_VISIBLE, 7,7,240,32,hDialog, (HMENU)IDC_STATICTEXT3, hInstance , NULL);

	// start worker thread
	installerThread = (HANDLE)_beginthreadex(NULL, 0, &InstallerThread, NULL, 0, &installerThreadId);
	



	// set progressbar to 0-100
	SendMessage( GetDlgItem( hDialog, IDC_PROGRESS2), PBM_SETRANGE, 0, MAKELPARAM(0,100) );

	SetProgressValue( 100 );

	// set Large Message Text Font
	SendMessage( GetDlgItem( hDialog, IDC_STATICTEXT3), WM_SETFONT, (WPARAM)hFont ,TRUE);

	SetLargeMessageText(L"Installing Packages...");



	// main thread message pump.
    while ((status = GetMessage(& msg, 0, 0, 0)) != 0){
        if (status == -1)
            return -1;
        if (!IsDialogMessage (hDialog, & msg)){
            TranslateMessage ( & msg );
            DispatchMessage ( & msg );
        }
    }

	/*

	int i;
	// try to install up to three times.
	for(i=0;i<3;i++) {
		if( !IsCoAppInstalled() ) {
			InstallCoApp();
		} else {
			// coapp engine installed. Call the engine with the path provided.
			coapp_resolve( L"foo.xml" , ResolvePackageCallback);

			while(packageList) {
				if( !packageList->localpath )
					coapp_download(packageList->URL);

			}

			return 0;
		}
	} 

	return Fail(COAPP_NOT_FUNCTIONING);
	*/
	return 0;

}