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

#include "tchar.h"
#include "malloc.h"
#include <winhttp.h>
#include <winbase.h>

const TCHAR* COAPP_INSTALL_FAILED = _T( "CoApp-Engine failed to install" ) ;
const TCHAR* COAPP_NOT_FUNCTIONING = _T( "CoApp-Engine failed to start" ) ;
const TCHAR* UNABLE_TO_GET_TEMP_PATH = _T( "Unable to get Temporary Path" ) ;
const TCHAR* UNABLE_TO_CREATE_TEMP_FILE= _T( "Unable to create temporary file for CoApp-Engine.MSI");
const TCHAR* CANT_ALLOCATE_MEMORY = _T("Failure to allocate memory");
const TCHAR* FAILURE_TO_DOWNLOAD = _T("Failure to download. Way Uncool.");
const TCHAR* NO_DATA_AVAILABLE = _T("No Data Available");
#define BUFSIZE 1024

int rc;

HMODULE CoAppModule = NULL;

#define RETURNONFAIL( call ) if( rc = call ) return rc;

int Fail(const TCHAR* failReason ) {
	// dunno what we're gonna do with this, but ... whatever :D
	MessageBox( NULL, failReason, _T("Package Installation Problem"), 0);
	return 1;
}


int IsCoAppInstalled( ) {
	// manually load the DLL 
	DWORD result;

	if( NULL != CoAppModule )  {
		CoAppModule = LoadLibrary( _T("coapp-engine") );

		if( NULL == CoAppModule ) {
			//
			// TODO: We should really do somthing with this error.
			//
			result = GetLastError();
			return 0;
		}
	}
	return 1;
}

int InstallCoApp() {
	DWORD dwSize = 0;
	DWORD dwBytesWritten = 0;
	DWORD dwRetVal = 0;
	HANDLE hFile;

    DWORD dwDownloaded = 0;
    void* pszOutBuffer;
    BOOL  bResults = FALSE;
    HINTERNET  hSession = NULL;
	HINTERNET  hConnect = NULL;
    HINTERNET  hRequest = NULL;
	TCHAR lpTempFolderBuffer[BUFSIZE];
	TCHAR lpTempPathBuffer[BUFSIZE];
	
	     
    dwRetVal = GetTempPath(BUFSIZE,  lpTempFolderBuffer); 

    if (dwRetVal > BUFSIZE || (dwRetVal == 0)) {
        return Fail( UNABLE_TO_GET_TEMP_PATH );
    }

	// grab a unique file name to write the MSI to.
	wsprintf( lpTempPathBuffer, _T("%s\\coapp-engine[%d].msi"), lpTempFolderBuffer, GetTickCount() );

    // Use WinHttpOpen to obtain a session handle.
    hSession = WinHttpOpen( L"CoAppBootstrapper/1.0",  WINHTTP_ACCESS_TYPE_DEFAULT_PROXY,WINHTTP_NO_PROXY_NAME, WINHTTP_NO_PROXY_BYPASS, 0);

    // Specify an HTTP server.
    if (hSession)
        hConnect = WinHttpConnect( hSession, L"coapp.org", INTERNET_DEFAULT_HTTP_PORT, 0);

    // Create an HTTP request handle.
    if (hConnect)
        hRequest = WinHttpOpenRequest( hConnect, L"GET",L"/coapp-engine.msi" , NULL, WINHTTP_NO_REFERER,  WINHTTP_DEFAULT_ACCEPT_TYPES,  NULL);

    // Send a request.
    if (hRequest)
        bResults = WinHttpSendRequest( hRequest, WINHTTP_NO_ADDITIONAL_HEADERS, 0, WINHTTP_NO_REQUEST_DATA, 0, 0, 0);
 
    // End the request.
    if (bResults)
        bResults = WinHttpReceiveResponse( hRequest, NULL);

	hFile = CreateFile(lpTempPathBuffer, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS,  FILE_ATTRIBUTE_NORMAL,NULL);

    if (hFile == INVALID_HANDLE_VALUE) { 
        bResults = Fail( UNABLE_TO_CREATE_TEMP_FILE );
    } 

    // Keep checking for data until there is nothing left.
    if (bResults) {
        do  {
            // Check for available data.
            dwSize = 0;
            if (!WinHttpQueryDataAvailable( hRequest, &dwSize)) {
				Fail( NO_DATA_AVAILABLE );
                break;
            }
            
            // No more available data.
            if (!dwSize)
                break;

            // Allocate space for the buffer.
            pszOutBuffer = malloc(dwSize+1);
            if (!pszOutBuffer) {
                Fail(CANT_ALLOCATE_MEMORY);
                break;
            }
            
            // Read the Data.
            ZeroMemory(pszOutBuffer, dwSize+1);

            if (!WinHttpReadData( hRequest, (LPVOID)pszOutBuffer, dwSize, &dwDownloaded)) {                                  
                Fail(FAILURE_TO_DOWNLOAD);
				break;
            }
            else {
				WriteFile( hFile, pszOutBuffer, dwSize, &dwBytesWritten, NULL ); 
            }
        
            // Free the memory allocated to the buffer.
            free(pszOutBuffer);

            // This condition should never be reached since WinHttpQueryDataAvailable
            // reported that there are bits to read.
            if (!dwDownloaded)
                break;
                
        } while (dwSize > 0);
		CloseHandle( hFile );
		MsiInstallProduct( lpTempPathBuffer, NULL );
    }
    else {
        // Report any errors.
        wprintf( _T( "Error %d has occurred.\n"), GetLastError() );
    }

    // Close any open handles.
    if (hRequest) 
		WinHttpCloseHandle(hRequest);
    if (hConnect) 
		WinHttpCloseHandle(hConnect);
    if (hSession) 
		WinHttpCloseHandle(hSession);


	return 0;
}



int WINAPI WinMain( HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR pszCmdLine, int nCmdShow) {
	
	if( !IsCoAppInstalled() )
	{
		RETURNONFAIL( InstallCoApp() );

		if( !IsCoAppInstalled() )
			return Fail(COAPP_NOT_FUNCTIONING);


	}

	return 0;
}

