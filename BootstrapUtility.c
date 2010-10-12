//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
#define _WIN32_WINNT _WIN32_WINNT_WS03 
//#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers

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

#include <Softpub.h>
#include <wincrypt.h>
#include <wintrust.h>


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

wchar_t* GetModuleFolder( HMODULE module ) {
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

wchar_t* GetModuleFullPath( HMODULE module ) {
	wchar_t* result = (wchar_t*)malloc(BUFSIZE);
	ZeroMemory(result, BUFSIZE);
	GetModuleFileName(module, result, BUFSIZE);
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
	
	ActivationContext.lpSource = GetModuleFullPath( module );
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

BOOL IsEmbeddedSignatureValid(LPCWSTR pwszSourceFile)
{
    LONG lStatus;
    DWORD dwLastError;

    // Initialize the WINTRUST_FILE_INFO structure.

    WINTRUST_FILE_INFO FileData;
    GUID WVTPolicyGUID = WINTRUST_ACTION_GENERIC_VERIFY_V2;
    WINTRUST_DATA WinTrustData;

    memset(&FileData, 0, sizeof(FileData));
    FileData.cbStruct = sizeof(WINTRUST_FILE_INFO);
    FileData.pcwszFilePath = pwszSourceFile;
    FileData.hFile = NULL;
    FileData.pgKnownSubject = NULL;

    /*
    WVTPolicyGUID specifies the policy to apply on the file
    WINTRUST_ACTION_GENERIC_VERIFY_V2 policy checks:
    
    1) The certificate used to sign the file chains up to a root 
    certificate located in the trusted root certificate store. This 
    implies that the identity of the publisher has been verified by 
    a certification authority.
    
    2) In cases where user interface is displayed (which this example
    does not do), WinVerifyTrust will check for whether the  
    end entity certificate is stored in the trusted publisher store,  
    implying that the user trusts content from this publisher.
    
    3) The end entity certificate has sufficient permission to sign 
    code, as indicated by the presence of a code signing EKU or no 
    EKU.
    */


    // Initialize the WinVerifyTrust input data structure.

    // Default all fields to 0.
	ZeroMemory(&WinTrustData, sizeof(WinTrustData));
    WinTrustData.cbStruct = sizeof(WinTrustData);
    
    // Use default code signing EKU.
    WinTrustData.pPolicyCallbackData = NULL;

    // No data to pass to SIP.
    WinTrustData.pSIPClientData = NULL;

    // Disable WVT UI.
    WinTrustData.dwUIChoice = WTD_UI_NONE;

    // No revocation checking.
    WinTrustData.fdwRevocationChecks = WTD_REVOKE_NONE; 

    // Verify an embedded signature on a file.
    WinTrustData.dwUnionChoice = WTD_CHOICE_FILE;

    // Default verification.
    WinTrustData.dwStateAction = 0;

    // Not applicable for default verification of embedded signature.
    WinTrustData.hWVTStateData = NULL;

    // Not used.
    WinTrustData.pwszURLReference = NULL;

    // Default.
    WinTrustData.dwProvFlags = WTD_SAFER_FLAG;

    // This is not applicable if there is no UI because it changes 
    // the UI to accommodate running applications instead of 
    // installing applications.
    WinTrustData.dwUIContext = 0;

    // Set pFile.
    WinTrustData.pFile = &FileData;

    // WinVerifyTrust verifies signatures as specified by the GUID 
    // and Wintrust_Data.
    lStatus = WinVerifyTrust( NULL, &WVTPolicyGUID, &WinTrustData);

    switch (lStatus) {
        case ERROR_SUCCESS:
            /*
            Signed file:
                - Hash that represents the subject is trusted.

                - Trusted publisher without any verification errors.

                - UI was disabled in dwUIChoice. No publisher or 
                    time stamp chain errors.

                - UI was enabled in dwUIChoice and the user clicked 
                    "Yes" when asked to install and run the signed 
                    subject.
            */
            // wprintf_s(L"The file \"%s\" is signed and the signature was verified.\n", pwszSourceFile);
			return TRUE;
            break;
        
        case TRUST_E_NOSIGNATURE:
            // The file was not signed or had a signature 
            // that was not valid.

            // Get the reason for no signature.
            dwLastError = GetLastError();
            if (TRUST_E_NOSIGNATURE == dwLastError || TRUST_E_SUBJECT_FORM_UNKNOWN == dwLastError || TRUST_E_PROVIDER_UNKNOWN == dwLastError) {
                // The file was not signed.
                // wprintf_s(L"The file \"%s\" is not signed.\n pwszSourceFile);
            }  else {
                // The signature was not valid or there was an error 
                // opening the file.
               //  wprintf_s(L"An unknown error occurred trying to  verify the signature of the \"%s\" file.\n", pwszSourceFile);
            }

            break;

        case TRUST_E_EXPLICIT_DISTRUST:
            // The hash that represents the subject or the publisher 
            // is not allowed by the admin or user.
            // wprintf_s(L"The signature is present, but specifically disallowed.\n");
            break;

        case TRUST_E_SUBJECT_NOT_TRUSTED:
            // The user clicked "No" when asked to install and run.
            // wprintf_s(L"The signature is present, but not trusted.\n");
            break;

        case CRYPT_E_SECURITY_SETTINGS:
            /*
            The hash that represents the subject or the publisher 
            was not explicitly trusted by the admin and the 
            admin policy has disabled user trust. No signature, 
            publisher or time stamp errors.
            */
            // wprintf_s(L"CRYPT_E_SECURITY_SETTINGS - The hash representing the subject or the publisher wasn't explicitly trusted by the admin and admin policy has disabled user trust. No signature, publisher or timestamp errors.\n");
            break;

        default:
            // The UI was disabled in dwUIChoice or the admin policy 
            // has disabled user trust. lStatus contains the 
            // publisher or time stamp chain error.
            // wprintf_s(L"Error is: 0x%x.\n", lStatus);
            break;
    }

    return FALSE;
}
