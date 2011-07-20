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

#define REASONABLE_MAXIMUM_ENTRIES 32

HANDLE ApplicationInstance = 0;
HANDLE WorkerThread = NULL;
unsigned WorkerThreadId = 0;
BOOL IsShuttingDown = FALSE;
LPWSTR CommandLine = NULL;
LPWSTR MsiFile = NULL;
LPWSTR MsiDirectory = NULL;
LPWSTR ManifestFilename = NULL;

wchar_t* CoAppInstallerPath = NULL;

typedef struct ManifestEntry { 
	wchar_t* filename;
	wchar_t* location;
	wchar_t* registryKeyCheck;
	wchar_t* cosmeticName;
	wchar_t* parameters;

	wchar_t* localPath;

	BOOL IsInstalled;
};


int ManfiestEntriesCount = 0;
struct ManifestEntry* ManifestEntries[REASONABLE_MAXIMUM_ENTRIES];

struct ManifestEntry* NewManifestEntry() {
	struct ManifestEntry* result;

	result = (struct ManifestEntry*)malloc( sizeof(struct ManifestEntry) );
	ZeroMemory(result, sizeof(struct ManifestEntry));

	return result;
}

#define NULLIFY_MEMBER( member ) if( member ) { free( member); member = NULL; }
void DeleteManifestEntry(struct ManifestEntry* entry) {
	if( entry ) {
		NULLIFY_MEMBER( entry->filename );
		NULLIFY_MEMBER( entry->location );
		NULLIFY_MEMBER( entry->registryKeyCheck );
		NULLIFY_MEMBER( entry->cosmeticName );
		NULLIFY_MEMBER( entry->parameters );
		NULLIFY_MEMBER( entry->localPath );

		free( entry );
	}
}

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

void TryUpdate() {
	wchar_t* updateCommand;
	
	// check if there is a registry setting 
	updateCommand = (wchar_t*)GetRegistryValue(L"Software\\CoApp", L"UpdateCommand", REG_SZ);

		
	DeleteString(updateCommand);
	CoAppInstallerPath = NULL;
}

int IsCoAppInstalled( ) {
	
	if( RegistryKeyPresent(L"Software\\CoApp#Reinstall") ) 
		return FALSE;

    CoAppInstallerPath = GetWinSxSResourcePathViaManifest((HMODULE)ApplicationInstance, INSTALLER_MANFIEST_ID, L"coapp.installer.exe");

	
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
        
				if( index > 3 || index < 0 )
					break;

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

int Launch() {
    wchar_t commandLine[32768];

    STARTUPINFO StartupInfo;
    PROCESS_INFORMATION ProcInfo;

    ZeroMemory(&StartupInfo, sizeof(STARTUPINFO) );
    StartupInfo.cb = sizeof( STARTUPINFO );

	if( !SUCCEEDED( StringCbPrintf( commandLine, 32768,  L"\"%s\" %s", CoAppInstallerPath, CommandLine) ) ) {
		TerminateApplicationWithError(EXIT_STRING_PRINTF_ERROR, L"Internal Error: An unexpected error has ocurred in function:" __WFUNCTION__);
	}

    CreateProcess( CoAppInstallerPath, commandLine, NULL, NULL, TRUE, 0, NULL, NULL, &StartupInfo, &ProcInfo );
    
    ExitProcess(0);
    return 0;
}

wchar_t* GetBootstrapServerPaths() {
	wchar_t* result = NULL;
	wchar_t* entry = NULL;
	size_t accum = 0;
	size_t stringLength = 0;
	wchar_t* coapp_org = L"http://coapp.org/repository/";

	result = (wchar_t*)GetRegistryValue(L"Software\\CoApp", L"BootstrapServers", REG_MULTI_SZ);

	if( result == NULL ) {
		result = NewString();
	}

	entry = result;
	while( *entry ) {
		if( SUCCEEDED( StringCbLengthW(entry, BUFSIZE - accum, &stringLength )) ){
			entry += stringLength+1;
			accum+=stringLength+1;

		} else {
			// this is a severe error condition. 
			// we should bail as a fatal error at this point. 
			TerminateApplicationWithError(EXIT_BOOTSTRAP_MANIFEST_PARSE_FAILURE, L"Internal Error:An error occured reading the bootstrap server paths." );
			break;
		}
	}
	// entry now points to the last 
	if( BUFSIZE - accum < 512 )  {
		// there isn't enough space to safely add our search locations to the list.
		TerminateApplicationWithError(EXIT_BOOTSTRAP_MANIFEST_PARSE_FAILURE, L"Internal Error:An error occured reading the bootstrap server paths.");	
	}
	if( !IsNullOrEmpty( MsiDirectory ) ) { 
		// append the directory that the MSI is in.
		stringLength = SafeStringLengthInCharacters(MsiDirectory);
		wcsncpy_s(entry , BUFSIZE - accum , MsiDirectory, stringLength);
		accum += stringLength +1;
		entry += stringLength +1;
	}

	// append the coapp.org canonical location
	stringLength  = SafeStringLengthInCharacters(coapp_org);
	wcsncpy_s(entry , BUFSIZE - accum , coapp_org, stringLength   );
	accum += stringLength + 1;
	entry += stringLength + 1;

	return result;
}

HRESULT ReadTextFile( const wchar_t* filename ,wchar_t** textRead, ULONG *sizeRead) 	
{
    HRESULT result = S_OK;
    HANDLE fileHandle = INVALID_HANDLE_VALUE;
    DWORD fileSize;
    BOOL isUnicodeFile = FALSE;
    USHORT uTemp;
    wchar_t* buffer = 0;
    ULONG charactersRead = 0;
    DWORD bytesRead;

	ManifestFilename = DuplicateString(filename);

    if (SUCCEEDED(result)) {
        fileHandle = CreateFileW(filename, GENERIC_READ, 0, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
		if( fileHandle == INVALID_HANDLE_VALUE ) {
			TerminateApplicationWithError(EXIT_BOOTSTRAP_MANIFEST_PARSE_FAILURE, L"Failed to open manifest file: \r\n\r\n   %s\r\n", filename );
		}
    }

    if (SUCCEEDED(result)) {
        fileSize = GetFileSize(fileHandle, NULL); // 64K
        result = (fileSize != -1) ? S_OK : HRESULT_FROM_WIN32(GetLastError());
    }

    if (SUCCEEDED(result)) {
        result = ReadFile(fileHandle, &uTemp, 2, &bytesRead, NULL) ? S_OK : HRESULT_FROM_WIN32(GetLastError());
    }

    if (SUCCEEDED(result)) {
        isUnicodeFile = (uTemp == 0xfeff);

        if (isUnicodeFile) {
            fileSize -= 2;

            buffer = (wchar_t*) malloc(fileSize);

            if (buffer) {
                result = ReadFile(fileHandle, buffer, fileSize, &bytesRead, NULL) ? S_OK : HRESULT_FROM_WIN32(GetLastError());
                charactersRead = fileSize / sizeof(wchar_t);
            }
            else {
                result = E_OUTOFMEMORY;
            }
        }
        else {
            result = S_OK;
            if (SUCCEEDED(result)) {
                char * pszBuffer = (char *)malloc(fileSize);

                result = pszBuffer ? S_OK : E_OUTOFMEMORY;

                if (SUCCEEDED(result)) {
                    SetFilePointer(fileHandle, 0, NULL, FILE_BEGIN); 
        
                    result = ReadFile(fileHandle, pszBuffer, fileSize, &bytesRead, NULL) ? S_OK : HRESULT_FROM_WIN32(GetLastError());
                }

                if (SUCCEEDED(result)) {
                    charactersRead = MultiByteToWideChar(CP_ACP, 0, pszBuffer, fileSize, NULL, 0);

                    if (charactersRead) {
                        buffer = (wchar_t *)malloc(sizeof(wchar_t) * charactersRead);
                    }
                    else {
                        result = E_FAIL;
                    }
                }

                if (SUCCEEDED(result)) {
                    MultiByteToWideChar(CP_ACP, 0, pszBuffer, fileSize, buffer, charactersRead);
                }

                if (pszBuffer) {
                    free(pszBuffer);
                }
            }
        }
    }

    if (INVALID_HANDLE_VALUE != fileHandle) {
        CloseHandle(fileHandle);
    }

	*textRead = buffer;
    *sizeRead= charactersRead;
    
    return result;
}

BOOL isAlphaNumeric( wchar_t character ) {
	return  (character >= L'a' && character <= L'z') || 
			(character >= L'0' && character <= L'9') || 
			(character >= L'A' && character <= L'Z');
}

int ParseBootstrappingManifest( wchar_t* text, ULONG charCount) {
	int index =0;
	wchar_t* p;
	wchar_t* line;
	wchar_t* q;
	BOOL endOfLine;
	
	p = text;
	while( p < text+charCount && *p != 0 ) {
		line = p;

		// go until we hit the end of the line or file.
		while( *p != L'\r' && *p != L'\n' && *p != 0 && p < text+charCount ) {
			p++;
		}

		// terminate this line
		*p = 0;

		// forward past the null 
		p++;

		// forward to the beginning of the next line (skip eol/whitespace)
		while( (*p == L'\r' || *p == L'\n'|| *p == L' ') && p < text+charCount ) {
			p++;
		}

		if( isAlphaNumeric(*line)) { // line now points to filename
			// only take lines that don't start with a # character (comment!)
			ManifestEntries[index] = NewManifestEntry();

			q = line;
			endOfLine = FALSE;

			// advance to next field
			while( *q!= 0 && *q!= L',' ) { q++; }  endOfLine = ( *q == 0 );  *q = 0; q++;

			ManifestEntries[index]->filename = DuplicateString(line);
			if( !endOfLine ) {
				line = q; // now points to regkeycheck

				// advance to next field
				while( *q!= 0 && *q!= L',' ) { q++; }  endOfLine = ( *q == 0 );  *q = 0; q++;
				ManifestEntries[index]->registryKeyCheck= DuplicateAndTrimString(line);
			}

			if( !endOfLine ) {
				line = q; // now points to location

				// advance to next field
				while( *q!= 0 && *q!= L',' ) { q++; }  endOfLine = ( *q == 0 );  *q = 0; q++;
				ManifestEntries[index]->location= DuplicateAndTrimString(line);
			}

			if( !endOfLine ) {
				line = q; // now points to cosmeticname

				// advance to next field
				while( *q!= 0 && *q!= L',' ) { q++; }  endOfLine = ( *q == 0 );  *q = 0; q++;
				ManifestEntries[index]->cosmeticName= DuplicateAndTrimString(line);
			}

			if( !endOfLine ) {
				line = q; // now points to parameters
				ManifestEntries[index]->parameters= DuplicateAndTrimString(line);
			}

			index++;
		}

		if (index == REASONABLE_MAXIMUM_ENTRIES ) {
			break;
		}
	}

	DeleteString(text);
	return index;
}

int LoadBootstrappingManifest() {
	wchar_t* serverPaths = GetBootstrapServerPaths();
	wchar_t* serverPath = serverPaths;
	wchar_t* manifestLocation;
	wchar_t* localManifestPath = UniqueTempFileName(L"bootstrapmanifest", L"txt");
	DWORD bufferSize;
	wchar_t* buffer;
	wchar_t* text;
	ULONG charCount;
	MSIHANDLE packageHandle;
	int result = 0;

	ASSERT_NOT_NULL(serverPath);
	SetStatusMessage(L"Reading bootstrap instructions");
	SetProgressNextTask();

	while( *serverPath ) {
		if( IsPathURL(serverPath) ) {
			manifestLocation = UrlOrPathCombine(serverPath, L"bootstrapmanifest.txt", L'/' );
			if ( DownloadFile(manifestLocation, localManifestPath, L"Bootstrap Manifest"  ) >0 ) {

				if( !SUCCEEDED( ReadTextFile( localManifestPath, &text, &charCount) ) ) {
					TerminateApplicationWithError(EXIT_BOOTSTRAP_MANIFEST_PARSE_FAILURE , L"Failed to read manifest file \r\n\r\n   %s\r\n", localManifestPath );
				}

				result = ParseBootstrappingManifest( text, charCount );
				goto fin;
			}
		}
		else  {
			manifestLocation = UrlOrPathCombine(serverPath, L"bootstrapmanifest.txt", L'\\' );
			if( FileExists(manifestLocation) ) {
				if( !SUCCEEDED( ReadTextFile( manifestLocation, &text, &charCount) ) ) {
					TerminateApplicationWithError(EXIT_BOOTSTRAP_MANIFEST_PARSE_FAILURE , L"Failed to read manifest file \r\n\r\n   %s\r\n", manifestLocation );
				}

				result = ParseBootstrappingManifest( text, charCount );
				goto fin;
			}
		}

		DeleteString(manifestLocation);
		manifestLocation = NULL;
		serverPath+= SafeStringLengthInCharacters(serverPath)+1;
	}

	if( !IsNullOrEmpty(MsiFile) ) {
		// if we get here, we haven't found a manifest value yet.
		// lets look in the msi we're trying to install to see if there is a manifest in there.
		if( ERROR_SUCCESS == MsiOpenPackageEx(MsiFile, 0,  &packageHandle) ) {
			buffer = NewString();
			bufferSize = BUFSIZE;
	
			if( ERROR_SUCCESS == MsiGetProperty(packageHandle,  L"BOOTSTRAPMANIFEST", buffer,  &bufferSize)  && bufferSize > 0 ) {
				MsiCloseHandle(packageHandle);
				// we've got the property! 
				result = ParseBootstrappingManifest( buffer, bufferSize );
			}
			else { 
				MsiCloseHandle(packageHandle);
			}
			DeleteString(buffer);
		}
	}
	fin:
	DeleteString(serverPaths);
	DeleteString(manifestLocation);
	DeleteString(localManifestPath);

	return result;
}

void PerformInstall() {
	int i;
	wchar_t* fullPath;
	int offset;
	STARTUPINFO StartupInfo;
    PROCESS_INFORMATION ProcInfo;
	wchar_t* commandLine;
	SetProgressNextTask();

	if( ManfiestEntriesCount = LoadBootstrappingManifest() ) {
		// set number of actual tasks to perform
		TaskCount = ManfiestEntriesCount  + 3; 

		// we've loaded the bootstrap manifest. 
		// Try running through the list of entries to download and install.

		// download everything first
		SetStatusMessage(L"Checking for installed components");
		for(i=0;i< ManfiestEntriesCount; i++ ) {
			SetProgressNextTask();
			if( IsShuttingDown )
				return;

			if( ManifestEntries[i] ) {
				
				if(!IsNullOrEmpty( ManifestEntries[i]->registryKeyCheck  )) {
					if( ManifestEntries[i]->IsInstalled = RegistryKeyPresent(ManifestEntries[i]->registryKeyCheck) ) {
						continue; // regkey is present, skip this entry in the manifest.
					}
				}

				ManifestEntries[i]->localPath = TempFileName(ManifestEntries[i]->filename);

				if( !(FileExists(ManifestEntries[i]->localPath) && IsEmbeddedSignatureValid(ManifestEntries[i]->localPath ) ) ) {

					if( IsPathURL(ManifestEntries[i]->location) ) {
						// download the URL 
						if( DownloadFile( ManifestEntries[i]->location , ManifestEntries[i]->localPath, ManifestEntries[i]->cosmeticName? ManifestEntries[i]->cosmeticName : ManifestEntries[i]->filename   ) <= 0 ) {
							// didn't find it there. Try smashing the location + filename
							fullPath = UrlOrPathCombine( ManifestEntries[i]->location, ManifestEntries[i]->filename , L'/');
							if( DownloadFile( fullPath , ManifestEntries[i]->localPath,ManifestEntries[i]->cosmeticName? ManifestEntries[i]->cosmeticName : ManifestEntries[i]->filename  ) <= 0 ) {
								// didn't find it there either?
								// try local dir as a last ditch effort.
								if( !IsNullOrEmpty( MsiDirectory ) ) {
									DeleteString(fullPath);
									fullPath = UrlOrPathCombine(MsiDirectory, ManifestEntries[i]->filename , L'\\');
									if( (FileExists(fullPath) ) ) {
										// try copying the file from the location
										CopyFileW(fullPath, ManifestEntries[i]->localPath, FALSE );
									}
								}
							}
							DeleteString(fullPath);
							fullPath=NULL;
						}

						if( !(FileExists(ManifestEntries[i]->localPath) ) ) {
							TerminateApplicationWithError(EXIT_UNABLE_TO_DOWNLOAD_REQUIRED_PACKAGE ,L"Failed to download required CoApp component \r\n\r\n   %s\r\n\r\nfrom \r\n\r\n   %s\r\n", ManifestEntries[i]->cosmeticName? ManifestEntries[i]->cosmeticName : ManifestEntries[i]->filename , ManifestEntries[i]->location);
						}

						if( !IsEmbeddedSignatureValid(ManifestEntries[i]->localPath) ) {
							TerminateApplicationWithError(EXIT_PACKAGE_FAILED_SIGNATURE_VALIDATION, L"The CoApp component \r\n\r\n   %s\r\n\r\ndoes not have valid signature for file \r\n\r\n   %s\r\n", ManifestEntries[i]->cosmeticName? ManifestEntries[i]->cosmeticName : ManifestEntries[i]->filename,   ManifestEntries[i]->localPath );
						}
						continue;
					}

					// otherwise, it's a locally accessible path.
					if( (FileExists(ManifestEntries[i]->location) ) ) {
						// try copying the file from the location
						CopyFileW(ManifestEntries[i]->location, ManifestEntries[i]->localPath, FALSE );
					}

					// if the file isn't here, or isn't valid, try smashing location+filename
					if( !(FileExists(ManifestEntries[i]->localPath) && IsEmbeddedSignatureValid(ManifestEntries[i]->localPath ) ) ) {
						fullPath = UrlOrPathCombine( ManifestEntries[i]->location, ManifestEntries[i]->filename , L'\\');

						if( (FileExists(fullPath) ) ) {
							// try copying the file from the location
							CopyFileW(fullPath, ManifestEntries[i]->localPath, FALSE );
						} else if( !IsNullOrEmpty( MsiDirectory ) ) {
							DeleteString(fullPath);
							fullPath = UrlOrPathCombine(MsiDirectory, ManifestEntries[i]->filename , L'\\');
							if( (FileExists(fullPath) ) ) {
								// try copying the file from the location
								CopyFileW(fullPath, ManifestEntries[i]->localPath, FALSE );
							}
						}

						DeleteString(fullPath);
						fullPath=NULL;

						if( !(FileExists(ManifestEntries[i]->localPath) ) ) {
							TerminateApplicationWithError(EXIT_UNABLE_TO_DOWNLOAD_REQUIRED_PACKAGE ,L"Failed to download required CoApp component \r\n\r\n   %s\r\n\r\nfrom \r\n\r\n   %s\r\n", ManifestEntries[i]->cosmeticName? ManifestEntries[i]->cosmeticName : ManifestEntries[i]->filename , ManifestEntries[i]->location);
						}

						if( !IsEmbeddedSignatureValid(ManifestEntries[i]->localPath) ) {
							TerminateApplicationWithError(EXIT_PACKAGE_FAILED_SIGNATURE_VALIDATION, L"The CoApp component \r\n\r\n   %s\r\n\r\ndoes not have valid signature for file \r\n\r\n   %s\r\n", ManifestEntries[i]->cosmeticName? ManifestEntries[i]->cosmeticName : ManifestEntries[i]->filename,   ManifestEntries[i]->localPath );
						}
					}
					continue;
				}
			}
		}

		SetStatusMessage(L"Installing required components");
		// now, loop thru and install each one.
		for(i=0;i< ManfiestEntriesCount; i++ ) {
			if( IsShuttingDown )
				return;

			if( ManifestEntries[i] ) {
				SetProgressNextTask();

				if( !ManifestEntries[i]->IsInstalled ) {
					offset = SafeStringLengthInCharacters( ManifestEntries[i]->localPath )-4;
					if( _wcsnicmp((ManifestEntries[i]->localPath)+offset , L".exe" , 4 ) == 0 ) {
						// it's an EXE, lets' execute it.
						commandLine = ManifestEntries[i]->parameters ? DuplicateString(ManifestEntries[i]->parameters) : DuplicateString( L"/q /passive /norestart" );
						ZeroMemory(&StartupInfo, sizeof(STARTUPINFO) );
						StartupInfo.cb = sizeof( STARTUPINFO );
						SetStatusMessage(L"Installing %s",ManifestEntries[i]->cosmeticName? ManifestEntries[i]->cosmeticName : ManifestEntries[i]->filename);
						CreateProcess( ManifestEntries[i]->localPath , commandLine, NULL, NULL, TRUE, 0, NULL, NULL, &StartupInfo, &ProcInfo );
						WaitForSingleObject( ProcInfo.hProcess, INFINITE );
						DeleteString(commandLine);
					}
					else if( _wcsnicmp((ManifestEntries[i]->localPath)+offset , L".msi" , 4 ) == 0 ) {
						SetStatusMessage(L"Installing %s",ManifestEntries[i]->cosmeticName? ManifestEntries[i]->cosmeticName : ManifestEntries[i]->filename);

						// it's an MSI, lets' install it.
						MsiSetInternalUI( INSTALLUILEVEL_NONE , 0); 
						MsiSetExternalUI( BasicUIHandler, INSTALLLOGMODE_PROGRESS, L"COAPP");
						MsiInstallProduct( ManifestEntries[i]->localPath,  ManifestEntries[i]->parameters ?  ManifestEntries[i]->parameters : L"TARGETDIR=\"C:\\apps\\.installed\" COAPP_INSTALLED=1 REBOOT=REALLYSUPPRESS");
					} else {
						TerminateApplicationWithError(EXIT_PACKAGE_FAILED_SIGNATURE_VALIDATION, L"CoApp component \r\n\r\n   %s\r\n\r\nis not an MSI or EXE file. \r\n\r\n   %s\r\n",ManifestEntries[i]->cosmeticName? ManifestEntries[i]->cosmeticName : ManifestEntries[i]->filename,   ManifestEntries[i]->localPath  );
					}
				}
			}
		}
	} else {
		// we were not succesful in finding the bootstrap manfest. 
		// notify the user that CoApp is not able to install
		// and that they could go to a help URL to find out what
		// they can do if they are an advanced user.
		TerminateApplicationWithError(EXIT_BOOTSTRAP_MANIFEST_PARSE_FAILURE, L"Unable to get install manifest anywhere!");
	}
}

unsigned __stdcall InstallCoApp( void* pArguments ){
	while(!Ready)
        Sleep(300);

    SetStatusMessage(L"");
    SetLargeMessageText(L"Installing CoApp...");
    SetOverallProgressValue( 1 );

    if( IsShuttingDown )
        goto fin;

	TryUpdate();

	if( IsShuttingDown )
        goto fin;

	if( IsCoAppInstalled() ) {
		SetStatusMessage(L"Launching package installer.");
        Launch();
		goto fin;
    }

	PerformInstall();

    if( IsShuttingDown )
        goto fin;

	SetStatusMessage(L"Required CoApp Components Installed.");
	SetOverallProgressValue( 100 );

    if( IsCoAppInstalled() ) {
		SetStatusMessage(L"Launching package installer.");
        Launch();
    }
	else {
		TerminateApplicationWithError(EXIT_BOOTSTRAP_DIDNT_INSTALL_COAPP, L"No Installation errors occurred, yet the CoApp Engine isn't installed.");
	}

fin:
    ExitProcess(0);
    _endthreadex( 0 );
    WorkerThread = NULL;
    
    return 0;
}

int WINAPI wWinMain( HINSTANCE hInstance, HINSTANCE hPrevInstance, LPWSTR pszCmdLine, int nCmdShow) {
    SHELLEXECUTEINFO sei;
	DWORD dwError;
	wchar_t szPath[MAX_PATH];
	wchar_t *p;

    INITCOMMONCONTROLSEX iccs;

    CommandLine = pszCmdLine;
    ApplicationInstance = hInstance;

	 // Elevate the process if it is not run as administrator.
	if (!IsRunAsAdmin()){
		if (GetModuleFileName(NULL, szPath, ARRAYSIZE(szPath))) {
			// Launch itself as administrator.
			sei.lpVerb = L"runas";
			sei.lpFile = szPath;
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
	}

    // load comctl32 v6, in particular the progress bar class
    iccs.dwSize = sizeof(INITCOMMONCONTROLSEX); // Naughty! :)
    iccs.dwICC  = ICC_PROGRESS_CLASS;
    InitCommonControlsEx(&iccs);
	
    // check for CoApp 
    if( IsCoAppInstalled() ) 
        return Launch();

	ZeroMemory(ManifestEntries, REASONABLE_MAXIMUM_ENTRIES * sizeof(struct ManifestEntry*) );
	
	// we're gonna leak this. :p
	MsiFile = DuplicateString(CommandLine);
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
			p = MsiFile;
			while( *p != 0 && *p != L' ' ) {
				p++;
			}
			*p = 0; 
		}

		// and we're gonna leak this. :p
		MsiDirectory = GetFolderFromPath(MsiFile);
	}
	
    // not there? install it.--- start worker thread
    WorkerThread = (HANDLE)_beginthreadex(NULL, 0, &InstallCoApp, NULL, 0, &WorkerThreadId);
	
    // And, show the GUI
    return ShowGUI(hInstance);
}