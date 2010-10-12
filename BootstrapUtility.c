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
void SetProgressValue( int percentage );
void SetStatusMessage(  const wchar_t* format, ... );

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

wchar_t* GetPathFromRegistry() {
	LSTATUS status;
	HKEY key;
	int index=0;
	wchar_t* name = (wchar_t*)malloc(BUFSIZE);
	wchar_t* value = (wchar_t*)malloc(BUFSIZE);
	DWORD nameSize = BUFSIZE;
	DWORD valueSize = BUFSIZE;
	DWORD dataType = REG_SZ;

	status = RegOpenKey(HKEY_LOCAL_MACHINE, L"Software\\CoApp",&key);
	if( status != ERROR_SUCCESS ) {
		status = RegOpenKey(HKEY_LOCAL_MACHINE, L"Software\\Wow6432Node",&key);
		if( status != ERROR_SUCCESS )
			goto release_value;

	}

	do {

		status = RegEnumValue(key, index, name, &nameSize, NULL, &dataType,(LPBYTE)value, &valueSize);
		if( status != ERROR_SUCCESS )
			goto release_value;
		
		if( lstrcmpi(L"CoAppInstaller", name) == 0 )
			goto release_name;

		index++;
	}while( status != ERROR_SUCCESS );


release_value:  // called when the keys don't exist.
		free(value);
		value = NULL;

release_name:
		free(name);
		name = NULL;
		
	return value;

}

wchar_t* GetModulePath( HMODULE module ) {
	wchar_t* result = (wchar_t*)malloc(BUFSIZE);
	wchar_t* position = NULL;
	int length=0;

	ZeroMemory(result, BUFSIZE);

	length = GetModuleFileName(module, result, BUFSIZE);
	position = result+length;
	while( position >= result && position[0] != L'\\')
		position--;
	position[1] = 0;

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
	DWORD contentLength;
	DWORD tmp = sizeof(DWORD);
	HANDLE localFile;

	ZeroMemory(&urlComponents, sizeof(urlComponents));
	urlComponents.dwStructSize = sizeof(urlComponents);

	urlComponents.dwSchemeLength    = -1;
	urlComponents.dwHostNameLength  = -1;
	urlComponents.dwUrlPathLength   = -1;
	urlComponents.dwExtraInfoLength = -1;

	if(!WinHttpCrackUrl(URL, (DWORD)wcslen(URL), 0, &urlComponents))
		FINISH( L"URL not valid" );

	SetProgressValue( 15 );
	wcsncpy_s( urlHost , BUFSIZE, URL+urlComponents.dwSchemeLength+3 ,urlComponents.dwHostNameLength );
	wcsncpy_s( urlPath , BUFSIZE, URL+urlComponents.dwSchemeLength+urlComponents.dwHostNameLength+3, urlComponents.dwUrlPathLength );

	SetStatusMessage(L"Contacting Server");

	// Use WinHttpOpen to obtain a session handle.
	if(!(session = WinHttpOpen( L"CoAppBootstrapper/1.0",  WINHTTP_ACCESS_TYPE_DEFAULT_PROXY,WINHTTP_NO_PROXY_NAME, WINHTTP_NO_PROXY_BYPASS, 0)))
		FINISH( L"Unable to create session for download" );

	// Specify an HTTP server.
	if (!(connection = WinHttpConnect( session, urlHost, urlComponents.nPort, 0)))
		FINISH( L"Unable to connect to URL for download" );

	SetProgressValue( 20 );
	// Create an HTTP request handle.
	if (!(request = WinHttpOpenRequest( connection, L"GET",urlPath , NULL, WINHTTP_NO_REFERER,  WINHTTP_DEFAULT_ACCEPT_TYPES, 0)))
		FINISH( L"Unable to open request for download" );

	// Send a request.
	if(!(WinHttpSendRequest( request, WINHTTP_NO_ADDITIONAL_HEADERS, 0, WINHTTP_NO_REQUEST_DATA, 0, 0, 0)))
		FINISH( L"Unable to send request for download" );
 
	SetProgressValue( 25 );
	// End the request.
	if(!(WinHttpReceiveResponse( request, NULL)))
		FINISH( L"Unable to receive response for download" );

	
	if( INVALID_HANDLE_VALUE == (localFile = CreateFile(destinationFilename, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS,  FILE_ATTRIBUTE_NORMAL,NULL)))
		FINISH( (L"Unable to create output file [%s]",destinationFilename ));

	SetStatusMessage(L"Downloading");

	WinHttpQueryHeaders( request, WINHTTP_QUERY_CONTENT_LENGTH | WINHTTP_QUERY_FLAG_NUMBER, NULL, &contentLength, &tmp , NULL);
	tmp=0;

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
		
		WriteFile( localFile, pszOutBuffer, bytesDownloaded, &bytesWritten, NULL ); 
		tmp+=bytesDownloaded;

		SetProgressValue( (tmp*25/contentLength )+25);
		
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

wchar_t* GetWinSxSResourcePathViaManifest(HMODULE module, int resourceIdForManifest, wchar_t* itemInAssembly ) {
	wchar_t* result = NULL;

	ACTCTX_SECTION_KEYED_DATA ReturnedData;
	ACTCTX ActivationContext;
	ULONG_PTR WinSxSCookie = 0;
	BOOL fSuccess = FALSE;
	HANDLE ActivationContextHandle = INVALID_HANDLE_VALUE;
	
	ZeroMemory(&ActivationContext,sizeof(ACTCTX));
	ZeroMemory(&ReturnedData,sizeof(ACTCTX_SECTION_KEYED_DATA));
	ReturnedData.cbSize = sizeof(ACTCTX_SECTION_KEYED_DATA);
	ActivationContext.cbSize = sizeof(ACTCTX);
	ActivationContext.dwFlags = ACTCTX_FLAG_RESOURCE_NAME_VALID | ACTCTX_FLAG_HMODULE_VALID ; 
	
	ActivationContext.hModule = module;
	
	ActivationContext.lpSource = GetModulePath( module );
	ActivationContext.lpResourceName = MAKEINTRESOURCE(resourceIdForManifest);

	ActivationContextHandle = CreateActCtx(&ActivationContext);
	
	if( ActivationContextHandle == INVALID_HANDLE_VALUE )
		goto fin;

	fSuccess = ActivateActCtx(ActivationContextHandle, &WinSxSCookie);
	if( !fSuccess  )
		goto release;
	
	fSuccess = FindActCtxSectionString(FIND_ACTCTX_SECTION_KEY_RETURN_HACTCTX,NULL, ACTIVATION_CONTEXT_SECTION_DLL_REDIRECTION, itemInAssembly, &ReturnedData);
	if( !fSuccess  )
		goto deactivate;

	result = (wchar_t*)malloc(BUFSIZE);
	if( !SearchPath(NULL, itemInAssembly, NULL, BUFSIZE, result, NULL) ) {
		free( result );
		result = NULL;
	}

deactivate:
	DeactivateActCtx(0, WinSxSCookie);

release:
	ReleaseActCtx(ActivationContextHandle);

fin:
	free((void*)ActivationContext.lpSource);
	return result;
}