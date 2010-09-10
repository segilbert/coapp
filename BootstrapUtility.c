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

#define BUFSIZE 8192
#define FINISH(text_msg) {/* LogMessage(text_msg); */ goto fin;}


wchar_t* DuplicateString( const wchar_t* text ) {
	size_t size;
	wchar_t* result = NULL;
	
	if(text) {
		size = wcslen(text);
		result = (wchar_t*)malloc(BUFSIZE);
		wcsncpy_s(result , BUFSIZE, text, size );
	}
	return result;
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

	return result;
}

wchar_t* GetModulePath( HMODULE module ) {
	wchar_t* result = NULL;

	PASSEMBLY_FILE_DETAILED_INFORMATION pAssemblyInfo = NULL;
	ACTIVATION_CONTEXT_QUERY_INDEX QueryIndex;
	BOOL fSuccess = FALSE;
	SIZE_T cbRequired;
	HANDLE hActCtx = INVALID_HANDLE_VALUE;
	BYTE bTemporaryBuffer[4096];
	PVOID pvDataBuffer = (PVOID)bTemporaryBuffer;
	SIZE_T cbAvailable = sizeof(bTemporaryBuffer);

	// Request the first file in the root assembly
	QueryIndex.ulAssemblyIndex = 1;
	QueryIndex.ulFileIndexInAssembly = 0;

	// Attempt to use our stack-based buffer first - if that's not large
	// enough, allocate from the heap and try again.
	fSuccess = QueryActCtxW( QUERY_ACTCTX_FLAG_ACTCTX_IS_HMODULE,  module,  (PVOID)&QueryIndex,  FileInformationInAssemblyOfAssemblyInActivationContext, pvDataBuffer, cbAvailable, &cbRequired);

	// Failed, because the buffer was too small.
	if (!fSuccess && (GetLastError() == ERROR_INSUFFICIENT_BUFFER)) {
		// Allocate what we need from the heap - fail if there isn't enough memory to do so.        
		pvDataBuffer = malloc(cbRequired);

		if (pvDataBuffer == NULL) {
			// ("Unable to allocate buffer in GetModulePath");
			goto fin;
		}

		cbAvailable = cbRequired;

		// If this fails again, exit out.
		fSuccess = QueryActCtxW( QUERY_ACTCTX_FLAG_ACTCTX_IS_HMODULE,  module, (PVOID)&QueryIndex, FileInformationInAssemblyOfAssemblyInActivationContext,pvDataBuffer, cbAvailable, &cbRequired);
	}

	if (fSuccess) {
		// Now that we've found the assembly info, cast our target buffer back to
		// the assembly info pointer.  Use pAssemblyInfo->lpFileName
		pAssemblyInfo = (PASSEMBLY_FILE_DETAILED_INFORMATION)pvDataBuffer;
	}

fin:

	if (pvDataBuffer && (pvDataBuffer != bTemporaryBuffer)) {
        free(pvDataBuffer);
    }
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

	HINTERNET  session = NULL;
	HINTERNET  connection = NULL;
	HINTERNET  request = NULL;
	DWORD bytesDownloaded = 0;
	DWORD bytesAvailable = 0;
	DWORD bytesWritten = 0;
	HANDLE localFile;

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
	if(!(session = WinHttpOpen( L"CoAppBootstrapper/1.0",  WINHTTP_ACCESS_TYPE_DEFAULT_PROXY,WINHTTP_NO_PROXY_NAME, WINHTTP_NO_PROXY_BYPASS, 0)))
		FINISH( L"Unable to create session for download" );

	// Specify an HTTP server.
	if (!(connection = WinHttpConnect( session, urlHost, urlComponents.nPort, 0)))
		FINISH( L"Unable to connect to URL for download" );

	// Create an HTTP request handle.
	if (!(request = WinHttpOpenRequest( connection, L"GET",urlPath , NULL, WINHTTP_NO_REFERER,  WINHTTP_DEFAULT_ACCEPT_TYPES, 0)))
		FINISH( L"Unable to open request for download" );

	// Send a request.
	if(!(WinHttpSendRequest( request, WINHTTP_NO_ADDITIONAL_HEADERS, 0, WINHTTP_NO_REQUEST_DATA, 0, 0, 0)))
		FINISH( L"Unable to send request for download" );
 
	// End the request.
	if(!(WinHttpReceiveResponse( request, NULL)))
		FINISH( L"Unable to receive response for download" );

	if( INVALID_HANDLE_VALUE == (localFile = CreateFile(destinationFilename, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS,  FILE_ATTRIBUTE_NORMAL,NULL)))
		FINISH( (L"Unable to create output file [%s]",destinationFilename ));

	// Keep checking for data until there is nothing left.
	do  {
		// Check for available data.
		bytesAvailable = 0;

		if (!WinHttpQueryDataAvailable( request, &bytesAvailable))
			FINISH( L"No data available");
			
		// No more available data.
		if (!bytesAvailable)
			break;

		// Allocate space for the buffer.
		pszOutBuffer = malloc(bytesAvailable+1);
		if (!pszOutBuffer) 
			FINISH( L"Allocation Failure" );
			
		// Read the Data.
		ZeroMemory(pszOutBuffer, bytesAvailable+1);

		if (!WinHttpReadData( request, (LPVOID)pszOutBuffer, bytesAvailable, &bytesDownloaded)) 
			FINISH( L"ReadData Failure" )
		else 
			WriteFile( localFile, pszOutBuffer, bytesAvailable, &bytesWritten, NULL ); 
		
		// Free the memory allocated to the buffer.
		free(pszOutBuffer);

		// This condition should never be reached since WinHttpQueryDataAvailable
		// reported that there are bits to read.
		if (!bytesDownloaded)
			break;
				
	} while (bytesAvailable > 0);

	fin:
	// Close any open handles.
	if (localFile)
		CloseHandle( localFile );
	if (request) 
		WinHttpCloseHandle(request);
	if (connection) 
		WinHttpCloseHandle(connection);
	if (session) 
		WinHttpCloseHandle(session);
}
