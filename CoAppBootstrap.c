//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

#define _WIN32_WINNT _WIN32_WINNT_WS03 
#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers

#include <SDKDDKVer.h>
#include <windows.h>
#include <Shellapi.h>

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
#include <Strsafe.h>

#include "resource.h"
#include "BootstrapGUI.h"
#include "BootstrapUtility.h"

const wchar_t* dot_net_4_full_1 = L"http://download.microsoft.com/download/1/B/E/1BE39E79-7E39-46A3-96FF-047F95396215/dotNetFx40_Full_setup.exe";
const wchar_t* dot_net_4_full_2 = L"http://coapp.org/dotNetFx40_Full_setup.exe";
const wchar_t* dot_net_4_local_1 = L"dotNetFx40_Full_setup.exe";
const wchar_t* dot_net_4_local_2 = L"dotNetFx40_Full_x86_x64.exe";
const wchar_t* dot_net_regkey = L"Software\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full#Install";
const wchar_t* BootstrapperUIFilename = L"BootstrapperUI.exe";

HANDLE ApplicationInstance = 0;
HANDLE WorkerThread = NULL;
unsigned WorkerThreadId = 0;
BOOL IsShuttingDown = FALSE;

wchar_t BootstrapPath[MAX_PATH];

wchar_t* BootstrapperUIPath;
wchar_t* MsiFile = NULL;
wchar_t* MsiDirectory = NULL;

void Shutdown() {
    IsShuttingDown = TRUE;
	AbortChain();
    PostQuitMessage(0);
}

#define CLOSE_MSI( HANDLE ) { if ( HANDLE != 0) MsiCloseHandle(HANDLE); HANDLE= 0; } 
#define FAIL_EXTRACT( ERRORCODE ) { subCode = ERRORCODE; goto fin; }

wchar_t* GetSecondStage() {
	MSIHANDLE packageDatabase= 0;
	MSIHANDLE view = 0;
	MSIHANDLE record = 0;
	DWORD bufferSize = 0;
	char* byteBuffer = NULL;
	HANDLE localFile = NULL;
	
	DWORD bytesWritten = 0;
	int subCode = 0;
	wchar_t* folder;

	if( BootstrapperUIPath != NULL ) {
		return BootstrapperUIPath;
	}

	// check to see if we have a copy of BootstrapperUI locally already.
	folder = GetFolderFromPath(BootstrapPath);
	BootstrapperUIPath = UrlOrPathCombine( folder, BootstrapperUIFilename, L'\\');
	if( FileExists(BootstrapperUIPath) ) {
		return BootstrapperUIPath;
	}
	DeleteString( &BootstrapperUIPath );
	DeleteString( &folder );

	// we can extract the bootstrapperUI
	
	if( IsNullOrEmpty(MsiFile) ) {
		FAIL_EXTRACT( 1 );
	}
	if( ERROR_SUCCESS != MsiOpenDatabase(MsiFile, MSIDBOPEN_READONLY, &packageDatabase) ) {
		FAIL_EXTRACT( 1 );
	}
	if (ERROR_SUCCESS != MsiDatabaseOpenView(packageDatabase, L"SELECT `Data` FROM `Binary` where `Name`='ManagedBootstrap'", &view)) {
		FAIL_EXTRACT( 2 );
	}
	if( ERROR_SUCCESS != MsiViewExecute(view, 0) ) {
		FAIL_EXTRACT( 3 );
	}
	if( ERROR_SUCCESS != MsiViewFetch(view, &record) ) {
		FAIL_EXTRACT( 4 );
	}

	bufferSize = MsiRecordDataSize(record, 1);
	if( bufferSize > 600*1024*1024 || bufferSize == 0 ) {  //bigger than 600k?
		FAIL_EXTRACT( 5 );
	}

	byteBuffer = (char*)malloc(bufferSize);
		
	if( ERROR_SUCCESS != MsiRecordReadStream(record, 1, byteBuffer, &bufferSize) ) {
		FAIL_EXTRACT( 6 );
	}
		
	// close these right away...
	CLOSE_MSI( record );
	CLOSE_MSI( view );
	CLOSE_MSI( packageDatabase );

	// got the whole file
	BootstrapperUIPath = TempFileName(BootstrapperUIFilename);
	if( INVALID_HANDLE_VALUE == (localFile = CreateFile(BootstrapperUIPath, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS,  FILE_ATTRIBUTE_NORMAL,NULL))) {
		FAIL_EXTRACT( 7 );		
	}

	// write out the seconds-stage-bootstrapper
	WriteFile( localFile, byteBuffer, bufferSize, &bytesWritten, NULL ); 
	CloseHandle( localFile );


fin:
	CLOSE_MSI( record );
	CLOSE_MSI( view );
	CLOSE_MSI( packageDatabase );
	if( byteBuffer != NULL ) {
		free( (void*) byteBuffer );
		byteBuffer = NULL;
	}

	if( subCode != 0 ) {
		TerminateApplicationWithError(EXIT_ERROR_EXTRACTING_NEXT_BOOTSTRAP, L"Can't open package file to extract bootstrap (%d).", subCode  );
	}

    return BootstrapperUIPath ;
}

int Launch() {
	wchar_t* commandLine = NULL;
	STARTUPINFO StartupInfo;
    PROCESS_INFORMATION ProcInfo;

	if( GetSecondStage() == NULL) {
		TerminateApplicationWithError(EXIT_ERROR_EXTRACTING_NEXT_BOOTSTRAP, L"Can't find second stage bootstrap.");
	}
	
	ZeroMemory(&StartupInfo, sizeof(STARTUPINFO) );
    StartupInfo.cb = sizeof( STARTUPINFO );

	commandLine = Sprintf(L"\"%s\" \"%s\"", BootstrapperUIPath, MsiFile);
	// launch the second-stage-bootstrapper.
	CreateProcess( BootstrapperUIPath, commandLine, NULL, NULL, TRUE, 0, NULL, NULL, &StartupInfo, &ProcInfo );

fin:
	DeleteString(&commandLine);
    ExitProcess(0);
    return 0;
}

void OnProgress(wchar_t* step, unsigned int progress ) {
	SetStatusMessage(step);
	SetProgressValue( progress+15 );
}

unsigned __stdcall InstallCoApp( void* pArguments ){
	STARTUPINFO StartupInfo;
    PROCESS_INFORMATION ProcInfo;
	wchar_t* commandLine = NULL;
	wchar_t* destinationFilename; 

	while(!Ready)
        Sleep(300);

    SetStatusMessage(L"");
    SetLargeMessageText(L"Installing .NET 4.0 Framework...");
	SetProgressValue( 1 );

	if( IsShuttingDown )
        goto fin;

	// before we go off downloading the .NET framework, 
	// let's see if it's already local somewhere.
	destinationFilename = UrlOrPathCombine( MsiDirectory, dot_net_4_local_1, L'\\');
	if( !IsEmbeddedSignatureValid(destinationFilename) ) {
		DeleteString(&destinationFilename);
		destinationFilename = UrlOrPathCombine( MsiDirectory, dot_net_4_local_2, L'\\');
		if( !IsEmbeddedSignatureValid(destinationFilename) ) {
			// download the framework
			// dot_net_4_full
			DeleteString(&destinationFilename);
			destinationFilename = UniqueTempFileName(L"DotNetFx40Full", L".exe");
			if( DownloadFile( dot_net_4_full_1 ,destinationFilename) == -1 || !IsEmbeddedSignatureValid(destinationFilename) ) {
				if( DownloadFile( dot_net_4_full_2 ,destinationFilename) == -1 || !IsEmbeddedSignatureValid(destinationFilename) ) {
					TerminateApplicationWithError(EXIT_UNABLE_TO_DOWNLOAD_DOTNET ,L"Failed to download the .NET Framework 4.0. Installer (Required)" );
				}
			}
		}
	}

	// install the framework
    if( IsShuttingDown )
        goto fin;

	SetProgressValue( 15 );

	// (run install)
	ZeroMemory(&StartupInfo, sizeof(STARTUPINFO) );
    StartupInfo.cb = sizeof( STARTUPINFO );

	commandLine = Sprintf(L"\"%s\" /q /pipe CoAppBootstrapper /ChainingPackage \"CoApp Bootstrapper\"", destinationFilename);
	// launch the second-stage-bootstrapper.
	CreateProcess( destinationFilename, commandLine, NULL, NULL, TRUE, 0, NULL, NULL, &StartupInfo, &ProcInfo );

	if( MonitorChainer(ProcInfo.hProcess, OnProgress) != S_OK ) {
		// hmm. bailed out of installing .NET
		TerminateApplicationWithError(EXIT_UNABLE_TO_DOWNLOAD_DOTNET, L".NET Framework installation was cancelled.");
		goto fin;
	}

	// after that's done
    if( IsShuttingDown )
        goto fin;

	SetStatusMessage(L"");
	SetProgressValue( 100 );

	// check to see if .NET 4.0 is installed.
    if( RegistryKeyPresent(dot_net_regkey) ) {
        SetStatusMessage(L"Launching CoApp Installer.");
		return Launch();
    }
	else {
		TerminateApplicationWithError(EXIT_UNABLE_TO_DOWNLOAD_DOTNET, L"No Installation errors occurred, yet the .NET 4.0 isn't installed.");
	}

fin:
    ExitProcess(0);
    _endthreadex( 0 );
    WorkerThread = NULL;
    
    return 0;
}

int WINAPI wWinMain( HINSTANCE hInstance, HINSTANCE hPrevInstance, wchar_t* pszCmdLine, int nCmdShow) {
    SHELLEXECUTEINFO sei;
	DWORD dwError;
	wchar_t *p;
    INITCOMMONCONTROLSEX iccs;

    ApplicationInstance = hInstance;

	GetModuleFileName(NULL, BootstrapPath, ARRAYSIZE(BootstrapPath));

	 // Elevate the process if it is not run as administrator.
	if (!IsRunAsAdmin()){
		// Launch itself as administrator.
		sei.lpVerb = L"runas";
		
		sei.lpFile = BootstrapPath;
		sei.lpParameters = pszCmdLine;
		sei.hwnd = NULL;
		sei.nShow = SW_NORMAL;

		if (!ShellExecuteEx(&sei)) {
			dwError = GetLastError();
			if (dwError == ERROR_CANCELLED) {
				// The user refused the elevation.
				// Do nothing ...
				TerminateApplicationWithError(EXIT_ADMIN_RIGHTS_REQUIRED , L"This package requires Administrator access to install.\r\n.");
			}
		}
		else {
			// we are done here!
			return 0;
		}
	}

    // load comctl32 v6, in particular the progress bar class
    iccs.dwSize = sizeof(INITCOMMONCONTROLSEX); // Naughty! :)
    iccs.dwICC  = ICC_PROGRESS_CLASS;
    InitCommonControlsEx(&iccs);

	// we're gonna leak this. :p
	MsiFile = DuplicateString(pszCmdLine);
	if( !IsNullOrEmpty(MsiFile) ) {
		if( *MsiFile == L'"' ) {
			// quoted command line. *sigh*.
			MsiFile++;
			p = MsiFile;
			while( *p != 0 && *p != L'"' ) {
				p++;
			}
			*p = 0; 
		} else {
			// no quoted parameter, break on space.
			// p = MsiFile;
			// while( *p != 0 && *p != L' ' ) {
			//	p++;
			//}
			// *p = 0; 
		}

		// MessageBox(NULL, MsiFile, L"Msi Filename", MB_OK );

		// and we're gonna leak this. :p
		MsiDirectory = GetFolderFromPath(MsiFile);
	}
	
	// check to see if .NET 4.0 is installed.
    if( RegistryKeyPresent(dot_net_regkey) ) 
        return Launch();

    // not there? install it.--- start worker thread
    WorkerThread = (HANDLE)_beginthreadex(NULL, 0, &InstallCoApp, NULL, 0, &WorkerThreadId);
	
    // And, show the GUI
    return ShowGUI(hInstance);
}