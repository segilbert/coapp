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
#include <Strsafe.h>


#define FINISH(text_msg) { MessageBox(NULL, Sprintf(L"Soft Download Error: %s \r\n [%s] \r\n code: [%x]\r\n line: %d",URL,text_msg , GetLastError(), __LINE__ ), L"not an error", MB_OK ); totalBytesDownloaded = -1; goto fin;}
void SetProgressValue( int percentage );
void SetStatusMessage(  const wchar_t* format, ... );

wchar_t* Sprintf(const wchar_t* format, ... ) {
	wchar_t* result = NewString();
	va_list args;
	
	ASSERT_STRING_OK(format);

	va_start(args, format);
	
	if( SUCCEEDED(StringCbVPrintf(result,BUFSIZE,format, args) ) ) {
		return result;
	}

	TerminateApplicationWithError(EXIT_STRING_PRINTF_ERROR, L"Internal Error: An unexpected error has ocurred in function:" __WFUNCTION__);

	return NULL;
}

wchar_t* NewString() {
	wchar_t* result = (wchar_t*) malloc(BUFSIZE*sizeof(wchar_t));
	ASSERT_NOT_NULL( result );

	ZeroMemory(result, BUFSIZE*sizeof(wchar_t));
	return result;
}

void DeleteString(wchar_t** stringPointer ) {
	if( stringPointer )  {
		if( *stringPointer ) {
			free( *stringPointer );
		}
		*stringPointer = NULL;
	}
}

wchar_t* DuplicateString( const wchar_t* text ) {
	size_t size;
	wchar_t* result = NULL;
	
	ASSERT_NOT_NULL( text );
	ASSERT_STRING_SIZE( text );

	size = SafeStringLengthInCharacters(text);
	
	result = NewString();
	wcsncpy_s(result , BUFSIZE, text, size );

	return result;
}

wchar_t* DuplicateAndTrimString(const wchar_t* text ) {
	wchar_t* result;
	size_t length;
	wchar_t* p;

	while( *text == L'\r' || *text == L'\n' || *text == L' ' || *text == L'\t' ) {
		text++;
	}

	result = DuplicateString(text);
	if( result ) {
		length = SafeStringLengthInCharacters(result);
		p = result + length -1;
		while( *p== L'\r' || *p== L'\n' || *p== L' ' || *p== L'\t' ) {
			*p = 0;
			p--;
		}
	}
	return result;	
}

size_t SafeStringLengthInBytes(const wchar_t* text) {
	size_t stringLength;

	if( SUCCEEDED( StringCbLengthW(text, BUFSIZE * sizeof(wchar_t), &stringLength )) ) {
		return stringLength;
	}
	return -1;
}

size_t SafeStringLengthInCharacters(const wchar_t* text ) {
	size_t stringLength;

	if( SUCCEEDED( StringCchLengthW(text, BUFSIZE, &stringLength )) ) {
		return stringLength;
	}
	return -1;
}

BOOL IsPathURL(const wchar_t* serverPath) {
	ASSERT_NOT_NULL(serverPath);
	return ( _wcsnicmp(serverPath, L"http://" , 7 ) == 0  || _wcsnicmp(serverPath, L"https://" , 7 ) == 0 );
}

BOOL IsNullOrEmpty(const wchar_t* text) {
	return !( text && *text );
}


///
/// <summary> 
///		combines a path and a filename
/// </summary>
 wchar_t* UrlOrPathCombine(const wchar_t* path, const wchar_t* name, wchar_t seperator) {
	ASSERT_STRING_OK( path );
	ASSERT_STRING_OK( name );

	if( *(path + SafeStringLengthInCharacters( path )-1) == seperator  ) {
		return Sprintf( L"%s%s" , path, name );
	}

	return Sprintf(L"%s%c%s" , path, seperator, name );
}


///
/// <summary> 
///		creates a temporary name for a file 
///		caller must free the memory for the string returned.
///		returns NULL on error.
/// </summary>
 wchar_t* TempFileName(const wchar_t* name) {
	DWORD returnValue = 0;
	wchar_t tempFolderPath[BUFSIZE];

	returnValue = GetTempPath(BUFSIZE,  tempFolderPath); 
	
	if (returnValue > BUFSIZE || (returnValue == 0)) {
		TerminateApplicationWithError(EXIT_UNABLE_TO_FIND_TEMPDIR, L"Internal Error: An unexpected error has ocurred in function:" __WFUNCTION__);
	}

	return UrlOrPathCombine( tempFolderPath, name , L'\\' );
}

///
/// <summary> 
///		creates a temporary name for a file 
///		caller must free the memory for the string returned.
///		returns NULL on error.
/// </summary>
wchar_t* UniqueTempFileName(const wchar_t* name,const wchar_t* extension) {
	DWORD returnValue = 0;
	wchar_t tempFolderPath[BUFSIZE];
	wchar_t* filename = NULL;
	wchar_t* result = NULL;

	returnValue = GetTempPath(BUFSIZE,  tempFolderPath); 
	
	if (returnValue > BUFSIZE || (returnValue == 0)) {
		free( filename );
		return NULL;
	}

	filename = Sprintf(L"%s[%d].%s", name , GetTickCount(), extension );

	result = UrlOrPathCombine(tempFolderPath, filename, L'\\' );
	DeleteString( &filename );
	return result;
}


// 
//   FUNCTION: IsRunAsAdmin()
//
//   PURPOSE: The function checks whether the current process is run as 
//   administrator. In other words, it dictates whether the primary access 
//   token of the process belongs to user account that is a member of the 
//   local Administrators group and it is elevated.
//
//   RETURN VALUE: Returns TRUE if the primary access token of the process 
//   belongs to user account that is a member of the local Administrators 
//   group and it is elevated. Returns FALSE if the token does not.
//
//   EXAMPLE CALL:
//         if (IsRunAsAdmin())
//             wprintf (L"Process is run as administrator\n");
//         else
//             wprintf (L"Process is not run as administrator\n");
BOOL IsRunAsAdmin() {
    BOOL fIsRunAsAdmin = FALSE;
    DWORD dwError = ERROR_SUCCESS;
    PSID pAdministratorsGroup = NULL;

    // Allocate and initialize a SID of the administrators group.
    SID_IDENTIFIER_AUTHORITY NtAuthority = SECURITY_NT_AUTHORITY;
    if (!AllocateAndInitializeSid(&NtAuthority,  2,  SECURITY_BUILTIN_DOMAIN_RID, DOMAIN_ALIAS_RID_ADMINS,  0, 0, 0, 0, 0, 0, &pAdministratorsGroup)) {
        dwError = GetLastError();
        goto Cleanup;
    }

    // Determine whether the SID of administrators group is enabled in 
    // the primary access token of the process.
    if (!CheckTokenMembership(NULL, pAdministratorsGroup, &fIsRunAsAdmin)) {
        dwError = GetLastError();
        goto Cleanup;
    }

Cleanup:
    // Centralized cleanup for all allocated resources.
    if (pAdministratorsGroup) {
        FreeSid(pAdministratorsGroup);
        pAdministratorsGroup = NULL;
    }

    // Throw the error if something failed in the function.
    if (ERROR_SUCCESS != dwError) {
        fIsRunAsAdmin = FALSE;
    }

    return fIsRunAsAdmin;
}

void SetRegistryValue(const wchar_t* keyname, const wchar_t* valueName, const wchar_t* value ) {
	LSTATUS status;
	HKEY key;
/*
    DWORD version;
    DWORD major;
    DWORD flags;

    version = GetVersion();
    major = LOBYTE(LOWORD(version));

    //
    // Windows XP and below don't support WOW64 flags, despite the MSDN documentation.
    // Will produce INVALID_PARAMETER HRESULT. - RR
    //
    if(major == 5)
        flags = KEY_WRITE;
    else
        flags = KEY_WRITE | KEY_WOW64_64KEY;

	status = RegCreateKeyEx( HKEY_LOCAL_MACHINE, keyname, 0,NULL, REG_OPTION_NON_VOLATILE,  flags, NULL , &key, NULL );
	*/

	status = RegCreateKeyEx( HKEY_LOCAL_MACHINE, keyname, 0,NULL, REG_OPTION_NON_VOLATILE, KEY_WRITE | KEY_WOW64_64KEY, NULL , &key, NULL );

	if( status != ERROR_SUCCESS ) {
		goto done;
	}

	if(IsNullOrEmpty(value)) {
		 RegDeleteValue(key, valueName);
	} else {
		RegSetValueEx( key, valueName, 0 , REG_SZ, (const BYTE*)(void*)value , SafeStringLengthInBytes(value) +2 );
	}

done:
	RegCloseKey(key);
}

void* GetRegistryValue(const wchar_t* keyname, const wchar_t* valueName,DWORD expectedDataType  ) {
	LSTATUS status;
	HKEY key;
	int index=0;
	wchar_t* name = NewString();
	wchar_t** value = (wchar_t**)(void*)NewString();
	DWORD nameSize = BUFSIZE;
	DWORD valueSize = BUFSIZE;
	DWORD dataType;
	/*
	DWORD version;
    DWORD major;
    DWORD flags;

    version = GetVersion();
    major = LOBYTE(LOWORD(version));

    //
    // Windows XP and below don't support WOW64 flags, despite the MSDN documentation.
    // Will produce INVALID_PARAMETER HRESULT. - RR
    //
    if(major == 5)
        flags = KEY_READ;
    else
        flags = KEY_READ | KEY_WOW64_64KEY;

	status = RegOpenKeyEx( HKEY_LOCAL_MACHINE, keyname, 0, flags , &key );
	*/

	status = RegOpenKeyEx( HKEY_LOCAL_MACHINE, keyname, 0, KEY_READ | KEY_WOW64_64KEY , &key );

	if( status != ERROR_SUCCESS ) {
		goto release_value;
	}

	do {
		ZeroMemory( name, BUFSIZE);
		ZeroMemory( value, BUFSIZE);
		nameSize = BUFSIZE;
		valueSize = BUFSIZE;

		status = RegEnumValue(key, index, name, &nameSize, NULL, &dataType,(LPBYTE)value, &valueSize);
		if( !(status == ERROR_SUCCESS || status == ERROR_MORE_DATA) )
			goto release_value;
		
		if( lstrcmpi(valueName, name) == 0 ) {
			if( expectedDataType == REG_NONE || expectedDataType == dataType ) {
				goto release_name;
			} else {
				goto release_value;
			}
		}
		index++;
	}while( status == ERROR_SUCCESS || status == ERROR_MORE_DATA  );

release_value:  // called when the keys don't exist.
		free(value);
		value = NULL;

release_name:
		free(name);
		name = NULL;
		
	RegCloseKey(key);
	return value;
}

BOOL RegistryKeyPresent(const wchar_t* regkey) {
	wchar_t* keyname = DuplicateString( regkey );
	wchar_t* valuename = keyname;
	void* value; 

	while( *valuename != 0 && *valuename != L'#') {
		valuename++;
	}

	if( *valuename == L'#' ) {
		*valuename = 0;
		valuename++;
	}

	value = GetRegistryValue( keyname, valuename, REG_NONE );
	if( value ) {
		free(value);
		return TRUE;
	}

	DeleteString(&keyname);
	return FALSE;
}

// given a path, returns the folder that contains it.
wchar_t* GetFolderFromPath( const wchar_t* path ) {
	wchar_t* result = DuplicateString(path);
	wchar_t* position = NULL;
	int length= wcslen(result);

	position = result+length;
	while( position >= result && position[0] != L'\\')
		position--;
	position[1] = 0;

	return result;
}

// returns the full path for a given module
wchar_t* GetModuleFullPath( HMODULE module ) {
	wchar_t* result = NewString();
	GetModuleFileName(module, result, BUFSIZE);
	return result;
}

// returns the folder containing a given module
wchar_t* GetModuleFolder( HMODULE module ) {
	wchar_t* modulePath = GetModuleFullPath(module);
	wchar_t* result = GetFolderFromPath(modulePath);
	free(modulePath);
	return result;
}

///
/// <summary> 
///		Downloads a file from a URL 
///		returns file size on success, -1 on error.
/// </summary>
int DownloadFile(const wchar_t* URL, const wchar_t* destinationFilename, const wchar_t* cosmeticName) {

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
	DWORD dwStatusCode = 0;
	DWORD contentLength;
	__int64 totalBytesDownloaded = 0;
	DWORD tmpValue= 0;
	HANDLE localFile = NULL;

	ZeroMemory(&urlComponents, sizeof(urlComponents));
	urlComponents.dwStructSize = sizeof(urlComponents);

	urlComponents.dwSchemeLength    = -1;
	urlComponents.dwHostNameLength  = -1;
	urlComponents.dwUrlPathLength   = -1;
	urlComponents.dwExtraInfoLength = -1;

	if(!WinHttpCrackUrl(URL, (DWORD)wcslen(URL), 0, &urlComponents)) {
		FINISH( L"URL not valid" );
	}

	wcsncpy_s( urlHost , BUFSIZE, URL+urlComponents.dwSchemeLength+3 ,urlComponents.dwHostNameLength );
	wcsncpy_s( urlPath , BUFSIZE, URL+urlComponents.dwSchemeLength+urlComponents.dwHostNameLength+3, urlComponents.dwUrlPathLength );

	//SetStatusMessage(L"Contacting Server [%s]", urlHost);

	// Use WinHttpOpen to obtain a session handle.
	if(!(session = WinHttpOpen( L"CoAppBootstrapper/1.0",  WINHTTP_ACCESS_TYPE_DEFAULT_PROXY,WINHTTP_NO_PROXY_NAME, WINHTTP_NO_PROXY_BYPASS, 0))) {
		FINISH( L"Unable to create session for download" );
	}

	WinHttpSetTimeouts( session, 12000, 12000, 12000, 12000);

	// Specify an HTTP server.
	if (!(connection = WinHttpConnect( session, urlHost, urlComponents.nPort, 0))) {
		FINISH( L"Unable to connect to URL for download" );
	}

	// Create an HTTP request handle.
	if (!(request = WinHttpOpenRequest( connection, L"GET",urlPath , NULL, WINHTTP_NO_REFERER,  WINHTTP_DEFAULT_ACCEPT_TYPES, 0))) {
		FINISH( L"Unable to open request for download" );
	}

	// Send a request.
	if(!(WinHttpSendRequest( request, WINHTTP_NO_ADDITIONAL_HEADERS, 0, WINHTTP_NO_REQUEST_DATA, 0, 0, 0))) {
		FINISH( L"Unable to send request for download" );
	}
 
	// End the request.
	if(!(WinHttpReceiveResponse( request, NULL))) {
		FINISH( L"Unable to receive response for download" );
	}

	tmpValue = sizeof(DWORD);
	WinHttpQueryHeaders( request, WINHTTP_QUERY_STATUS_CODE| WINHTTP_QUERY_FLAG_NUMBER, NULL, &dwStatusCode, &tmpValue, NULL );
	if( dwStatusCode != HTTP_STATUS_OK ) {
		//FINISH( L"Remote file not found" );
		totalBytesDownloaded = -1; goto fin;
	}

	tmpValue = sizeof(DWORD);
	WinHttpQueryHeaders( request, WINHTTP_QUERY_CONTENT_LENGTH | WINHTTP_QUERY_FLAG_NUMBER, NULL, &contentLength, &tmpValue , NULL);

	if( INVALID_HANDLE_VALUE == (localFile = CreateFile(destinationFilename, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS,  FILE_ATTRIBUTE_NORMAL,NULL))) {
		FINISH( L"Unable to create output file [%s]");
	}

	
	// Keep checking for data until there is nothing left.
	do  {
		SetStatusMessage(L"Downloading [%d%%]: %s ",(int)(totalBytesDownloaded*100/contentLength ),cosmeticName);

		// Check for available data.
		bytesAvailable = 0;

		if (!WinHttpQueryDataAvailable( request, &bytesAvailable)) {
			FINISH( L"No data available");
		}
		// No more available data.
		if (!bytesAvailable)
			break;

		// Allocate space for the buffer.
		pszOutBuffer = malloc(bytesAvailable+1);
		if (!pszOutBuffer)  {
			FINISH( L"Allocation Failure" );
		}
			
		// Read the Data.
		ZeroMemory(pszOutBuffer, bytesAvailable+1);

		if (!WinHttpReadData( request, (LPVOID)pszOutBuffer, bytesAvailable, &bytesDownloaded))  {
			FINISH( L"ReadData Failure" );
		}
		
		WriteFile( localFile, pszOutBuffer, bytesDownloaded, &bytesWritten, NULL ); 
		totalBytesDownloaded+=bytesDownloaded;
		SetProgressValue( (int)(totalBytesDownloaded*100/contentLength ));
		
		// Free the memory allocated to the buffer.
		free(pszOutBuffer);

		// This condition should never be reached since WinHttpQueryDataAvailable
		// reported that there are bits to read.
		if (!bytesDownloaded)
			break;
				
	} while (bytesAvailable > 0);

	SetStatusMessage(L"Downloading [100%%]: %s ",cosmeticName);
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

	return (int)totalBytesDownloaded; // bytes downloaded.
}

BOOL FileExists(const wchar_t* filePath) {
    WIN32_FILE_ATTRIBUTE_DATA fileData;

    if( IsNullOrEmpty(filePath) )
        return 0;

    return GetFileAttributesEx( filePath, GetFileExInfoStandard, &fileData);
}

__int64 GetFileVersion(const wchar_t* filename)  {
	DWORD dataSize, handle;
	void *data;
	UINT len;
	VS_FIXEDFILEINFO *pFileInfo;
	__int64 result = 0;
	
	if( FileExists( filename ) ) {
		dataSize = GetFileVersionInfoSize(filename, &handle);
		if( dataSize > 0 ) {
			data = malloc(dataSize);
			if(GetFileVersionInfo(filename, 0, dataSize, data) ) {
				if( VerQueryValue( data, L"\\", (void**)&pFileInfo, &len ) ) {
					result = (((__int64)pFileInfo->dwFileVersionMS)<<32) + ((__int64)pFileInfo->dwFileVersionLS);
				}
			}
			free(data);
		}
	}

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



void TerminateApplicationWithError(int errorLevel , const wchar_t* format, ... ) {
	va_list args;
	wchar_t caption[BUFSIZE];
	wchar_t message[BUFSIZE];
	wchar_t fullMessage[BUFSIZE];

	StringCbPrintf( caption, BUFSIZE,L"A problem has occured [%d]", errorLevel ) ;

	va_start(args, format);
	StringCbVPrintf(message,BUFSIZE,format, args);
	StringCbPrintf( fullMessage, BUFSIZE,L"%s \r\n\r\nFor troubleshooting on this error please visit http://coapp.org/help/%d \r\n\r\nDebug Info:\r\n\r\n   MsiFile:[%s]\r\n   MsiDirectory:[%s]\r\n   ManifestFilename:[%s]\r\n", message, errorLevel ,MsiFile, 	MsiDirectory, ManifestFilename);

	MessageBox(NULL,fullMessage,caption, MB_ICONERROR );

	ExitProcess(errorLevel);
}
