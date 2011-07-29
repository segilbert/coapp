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
#define HACK_FORCE_LOCALSYSTEM

const wchar_t* REG_CoAppKey = L"SOFTWARE\\CoApp";
const wchar_t* REG_CoAppReinstallKey = L"SOFTWARE\\CoApp#Reinstall";
const wchar_t* REG_CoAppInstallerValue= L"Installer";
const wchar_t* REG_CoAppPreferredInstallerValue= L"PreferredInstaller";
const wchar_t* REG_CoAppRootValue = L"Root";
const wchar_t* REG_CoAppBootstrapServers = L"BootstrapServers";
const wchar_t* coapp_org = L"http://coapp.org/repository/";
const wchar_t* coapp_exe = L"coapp.exe";
const wchar_t* msi_parameters = L"TARGETDIR=\"%s\\.installed\" COAPP_INSTALLED=1 REBOOT=REALLYSUPPRESS";
//const wchar_t* CoAppEnsureOk = L"c:\\windows\\system32\\cmd.exe /k \"%s\" set-active";
const wchar_t* CoAppEnsureOk = L"\"%s\" set-active";

__int64 BootstrapVersion = 0;
HANDLE ApplicationInstance = 0;
HANDLE WorkerThread = NULL;
unsigned WorkerThreadId = 0;
BOOL IsShuttingDown = FALSE;

wchar_t* MsiFile = NULL;
wchar_t* MsiDirectory = NULL;
wchar_t* ManifestFilename = NULL;
wchar_t* CoAppInstallerPath = NULL;
wchar_t* CoAppRootPath = NULL;
wchar_t* OutercurvePath = NULL;
int maxTicks;
int tickValue;

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
	struct ManifestEntry* result = NULL;

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
__int64 highestSoFar = 0;

void SearchForHighestVersionCoApp( const wchar_t* folder, __int64 minimumVersion ){
	wchar_t* folderWildcard = UrlOrPathCombine(folder, L"*" , L'\\');
	wchar_t* childFolder = NULL;
	__int64 fileVersion;

    WIN32_FIND_DATA file_data = {0};
    HANDLE hFile = FindFirstFile( folderWildcard, &file_data );

    if( hFile == INVALID_HANDLE_VALUE ) {
         return;
    }

    do {
        if( file_data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY ) {
            if( (wcscmp(file_data.cFileName, L".") != 0) && (wcscmp(file_data.cFileName, L"..") != 0) ) {
				childFolder = UrlOrPathCombine(folder, file_data.cFileName  , L'\\');
                SearchForHighestVersionCoApp( childFolder,minimumVersion );
				DeleteString(&childFolder);
            }
        } else {
            if( (file_data.dwFileAttributes & (FILE_ATTRIBUTE_HIDDEN | FILE_ATTRIBUTE_SYSTEM)) == 0 ) {
				if( lstrcmpi(coapp_exe, file_data.cFileName) == 0 ) {
					wchar_t* fullPath = UrlOrPathCombine(folder, file_data.cFileName, L'\\');
					fileVersion = GetFileVersion( fullPath );
					if( fileVersion > highestSoFar && fileVersion >= minimumVersion ) {
						DeleteString(&CoAppInstallerPath);
						CoAppInstallerPath = fullPath;
					}
					else {
						DeleteString(&fullPath);
					}
				}
            }
        }
    }
    while( FindNextFile( hFile, &file_data ) );

    FindClose( hFile );
}

BOOL IsCoAppInstalled( ) {
	__int64 fileVersion;

	if( RegistryKeyPresent(REG_CoAppReinstallKey) ) 
		return FALSE;

	CoAppInstallerPath = (wchar_t*)GetRegistryValue(REG_CoAppKey, REG_CoAppInstallerValue, REG_SZ);
	if( CoAppInstallerPath != NULL ) {
		fileVersion = GetFileVersion( CoAppInstallerPath );
		if( fileVersion >= BootstrapVersion ) {
			return TRUE;
		}
		// current version isn't high enough version
		DeleteString(&CoAppInstallerPath);
		SetRegistryValue(REG_CoAppKey, REG_CoAppInstallerValue, NULL);

	}	
	SearchForHighestVersionCoApp(OutercurvePath,BootstrapVersion);
	if( !IsNullOrEmpty(CoAppInstallerPath))  {
		// found one high enough version to work!
		SetRegistryValue(REG_CoAppKey, REG_CoAppInstallerValue, CoAppInstallerPath);
		return TRUE;
	}

    return FALSE;
}

int _stdcall BasicUIHandler(LPVOID pvContext, UINT iMessageType, LPCWSTR szMessage) {
    INSTALLMESSAGE mt;
    UINT uiFlags;
    int value[4];
    int index;
    const wchar_t* pChar = NULL;

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
						if ( maxTicks <= tickValue ) {
							maxTicks = tickValue;
						}
                        SetProgressValue(tickValue*100/maxTicks);
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
    wchar_t* commandLine = NULL;
	wchar_t* preferredInstaller = NULL;
    STARTUPINFO StartupInfo;
    PROCESS_INFORMATION ProcInfo;

    ZeroMemory(&StartupInfo, sizeof(STARTUPINFO) );
    StartupInfo.cb = sizeof( STARTUPINFO );
	
	preferredInstaller = (wchar_t*)GetRegistryValue(REG_CoAppKey, REG_CoAppPreferredInstallerValue, REG_SZ);
	if( !IsNullOrEmpty(preferredInstaller ) ) {
		if( FileExists( preferredInstaller ) ) {
			DeleteString(&CoAppInstallerPath);
			CoAppInstallerPath = preferredInstaller;
		}
	}

	commandLine = Sprintf(L"\"%s\" \"%s\"", CoAppInstallerPath, MsiFile);

    CreateProcess( CoAppInstallerPath, commandLine, NULL, NULL, TRUE, 0, NULL, NULL, &StartupInfo, &ProcInfo );

	DeleteString(&commandLine);

    ExitProcess(0);
    return 0;
}

wchar_t* GetBootstrapServerPaths() {
	wchar_t* result = NULL;
	wchar_t* entry = NULL;
	size_t accum = 0;
	size_t stringLength = 0;

	result = (wchar_t*)GetRegistryValue(REG_CoAppKey, REG_CoAppBootstrapServers, REG_MULTI_SZ);

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

HRESULT ReadTextFile( const wchar_t* filename ,wchar_t** textRead, ULONG *sizeRead) {
    HRESULT result = S_OK;
    HANDLE fileHandle = INVALID_HANDLE_VALUE;
    DWORD fileSize;
    BOOL isUnicodeFile = FALSE;
    USHORT uTemp;
    wchar_t* buffer = NULL;
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

	DeleteString(&text);
	return index;
}

int LoadBootstrappingManifest() {
	wchar_t* serverPaths = GetBootstrapServerPaths();
	wchar_t* serverPath = serverPaths;
	wchar_t* manifestLocation = NULL;
	wchar_t* localManifestPath = UniqueTempFileName(L"bootstrapmanifest", L"txt");
	wchar_t* buffer = NULL;
	wchar_t* text = NULL;

	DWORD bufferSize =0;
	ULONG charCount = 0;
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

		DeleteString(&manifestLocation);
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
			DeleteString(&buffer);
		}
	}
	fin:
	DeleteString(&serverPaths);
	DeleteString(&manifestLocation);
	DeleteString(&localManifestPath);

	return result;
}

void PerformInstall() {
	int i;
	wchar_t* fullPath = NULL;
	int offset;
	STARTUPINFO StartupInfo;
    PROCESS_INFORMATION ProcInfo;
	wchar_t* commandLine = NULL;
	wchar_t* msiParameters;
	SetProgressNextTask();

	if( ManfiestEntriesCount = LoadBootstrappingManifest() ) {
		// set number of actual tasks to perform
		TaskCount = (ManfiestEntriesCount*2)  + 4; 

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


					if( IsPathURL(ManifestEntries[i]->location) ) {
						// download the URL 
						if( DownloadFile( ManifestEntries[i]->location , ManifestEntries[i]->localPath, ManifestEntries[i]->cosmeticName? ManifestEntries[i]->cosmeticName : ManifestEntries[i]->filename   ) <= 0 ) {
							// didn't find it there. Try smashing the location + filename
							fullPath = UrlOrPathCombine( ManifestEntries[i]->location, ManifestEntries[i]->filename , L'/');
							if( DownloadFile( fullPath , ManifestEntries[i]->localPath,ManifestEntries[i]->cosmeticName? ManifestEntries[i]->cosmeticName : ManifestEntries[i]->filename  ) <= 0 ) {
								// didn't find it there either?
								// try local dir as a last ditch effort.
								if( !IsNullOrEmpty( MsiDirectory ) ) {
									DeleteString(&fullPath);
									fullPath = UrlOrPathCombine(MsiDirectory, ManifestEntries[i]->filename , L'\\');
									if( (FileExists(fullPath) ) ) {
										// try copying the file from the location
										CopyFileW(fullPath, ManifestEntries[i]->localPath, FALSE );
									}
								}
							}
							DeleteString(&fullPath);
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
							DeleteString(&fullPath);
							fullPath = UrlOrPathCombine(MsiDirectory, ManifestEntries[i]->filename , L'\\');
							if( (FileExists(fullPath) ) ) {
								// try copying the file from the location
								CopyFileW(fullPath, ManifestEntries[i]->localPath, FALSE );
							}
						}

						DeleteString(&fullPath);
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
						DeleteString(&commandLine);
					}
					else if( _wcsnicmp((ManifestEntries[i]->localPath)+offset , L".msi" , 4 ) == 0 ) {
						SetStatusMessage(L"Installing %s",ManifestEntries[i]->cosmeticName? ManifestEntries[i]->cosmeticName : ManifestEntries[i]->filename);

						// it's an MSI, lets' install it.
						MsiSetInternalUI( INSTALLUILEVEL_NONE , 0); 
						MsiSetExternalUI( BasicUIHandler, INSTALLLOGMODE_PROGRESS, L"COAPP");
						msiParameters = Sprintf(IsNullOrEmpty(ManifestEntries[i]->parameters) ?  msi_parameters : ManifestEntries[i]->parameters, CoAppRootPath );
						MsiInstallProduct( ManifestEntries[i]->localPath,  msiParameters );
						DeleteString(&msiParameters);
					} else {
						TerminateApplicationWithError(EXIT_PACKAGE_FAILED_SIGNATURE_VALIDATION, L"CoApp component \r\n\r\n   %s\r\n\r\nis not an MSI or EXE file. \r\n\r\n   %s\r\n",ManifestEntries[i]->cosmeticName? ManifestEntries[i]->cosmeticName : ManifestEntries[i]->filename,   ManifestEntries[i]->localPath  );
					}
				}
			}
		}

		SetStatusMessage(L"Updating Current Version");

		SearchForHighestVersionCoApp(OutercurvePath,BootstrapVersion);
		if( !IsNullOrEmpty(CoAppInstallerPath) ) {
			// we should run coapp set-active
			commandLine = Sprintf(CoAppEnsureOk, CoAppInstallerPath);
			ZeroMemory(&StartupInfo, sizeof(STARTUPINFO) );
			StartupInfo.dwFlags = STARTF_USESHOWWINDOW;
			StartupInfo.wShowWindow = SW_MINIMIZE;
			StartupInfo.cb = sizeof( STARTUPINFO );
			// CreateProcess( L"c:\\windows\\system32\\cmd.exe", commandLine, NULL, NULL, TRUE, 0, NULL, NULL, &StartupInfo, &ProcInfo );
			CreateProcess( CoAppInstallerPath, commandLine, NULL, NULL, TRUE, 0, NULL, NULL, &StartupInfo, &ProcInfo );
			WaitForSingleObject( ProcInfo.hProcess, INFINITE );
			DeleteString(&commandLine);
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

void CreateDirectoryFull( const wchar_t* path ) {
	wchar_t* parent = GetFolderFromPath( path );
	DWORD attributes = GetFileAttributes( path );

	if( attributes != 0xFFFFFFFF && (attributes & FILE_ATTRIBUTE_DIRECTORY) ) {
		return; // directory exists
	}

	if( attributes != 0xFFFFFFFF ) {
		TerminateApplicationWithError(EXIT_UNABLE_TO_CREATE_DIRECTORY, L"Blocked trying to create directory \r\n   %s", path );
	}

	attributes = GetFileAttributes( parent );
	if( attributes == 0xFFFFFFFF ) {
		CreateDirectoryFull( parent );
		attributes = GetFileAttributes( parent );
	}

	if( attributes == 0xFFFFFFFF ) {
		TerminateApplicationWithError(EXIT_UNABLE_TO_CREATE_DIRECTORY, L"Failed to create parent directory \r\n   %s \r\nfor \r\n   %s", parent, path );
	}

	if(!(attributes & FILE_ATTRIBUTE_DIRECTORY) ) {
		TerminateApplicationWithError(EXIT_UNABLE_TO_CREATE_DIRECTORY, L"Parent path \r\n   %s \r\nfor \r\n   %s is not a directory", parent, path );
	}

	CreateDirectory( path , NULL );
	attributes = GetFileAttributes( path );

	if( attributes == 0xFFFFFFFF ) {
		TerminateApplicationWithError(EXIT_UNABLE_TO_CREATE_DIRECTORY, L"Failed to create directory\r\n   %s", path );
	}
}

#ifdef HACK_FORCE_LOCALSYSTEM
BOOL IsUnrestrictedLocalSystem() {
	HANDLE hToken;
	LUID luidDebugPrivilege;
	PRIVILEGE_SET privs; 
	BOOL bResult = FALSE;

	// Get the calling thread's access token.
	if (!OpenThreadToken(GetCurrentThread(), TOKEN_QUERY, TRUE, &hToken)) {
		if (GetLastError() != ERROR_NO_TOKEN) {
			printf("CAN'T GET THREAD TOKEN!!!\n");
			return FALSE;
		}

		// Retry against process token if no thread token exists.
		if (!OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, &hToken)) {
			printf("CAN'T GET PROCESS TOKEN!!!\n");
			return FALSE;
		}
	}

	//Find the LUID for the debug privilege token
	if ( !LookupPrivilegeValue(  NULL, L"SeDebugPrivilege",  &luidDebugPrivilege ) ) {
		printf("LookupPrivilegeValue error: %u\n", GetLastError() ); 
		return FALSE; 
	}

	privs.PrivilegeCount = 1;
	privs.Control = PRIVILEGE_SET_ALL_NECESSARY;
	privs.Privilege[0].Luid = luidDebugPrivilege;
	privs.Privilege[0].Attributes = SE_PRIVILEGE_ENABLED; 
	PrivilegeCheck(hToken, &privs, &bResult);

	return bResult;
}

#endif

int WINAPI wWinMain( HINSTANCE hInstance, HINSTANCE hPrevInstance, wchar_t* pszCmdLine, int nCmdShow) {
    SHELLEXECUTEINFO sei;
	DWORD dwError;
	wchar_t BootstrapPath[MAX_PATH];
	wchar_t *p;
    INITCOMMONCONTROLSEX iccs;

#ifdef HACK_FORCE_LOCALSYSTEM
	wchar_t* PsExecPath = TempFileName(L"_PSEXEC.EXE");
	if(!FileExists(PsExecPath) ) {
		// First find and load the required resource
		HRSRC hResource = FindResource(hInstance, MAKEINTRESOURCE(EVIL_PSEXEC_BINARY), L"BINARY" );
		HGLOBAL hFileResource = LoadResource(hInstance, hResource);

		// Now open and map this to a disk file
		LPVOID lpFile = LockResource(hFileResource);
		DWORD dwSize = SizeofResource(hInstance, hResource);            

		// Open the file and filemap
		HANDLE hFile = CreateFile(PsExecPath, GENERIC_READ | GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
		HANDLE hFilemap = CreateFileMapping(hFile, NULL, PAGE_READWRITE, 0, dwSize, NULL);           
		LPVOID lpBaseAddress = MapViewOfFile(hFilemap, FILE_MAP_WRITE, 0, 0, 0);            

		// Write the file
		CopyMemory(lpBaseAddress, lpFile, dwSize);            

		// Unmap the file and close the handles
		UnmapViewOfFile(lpBaseAddress);
		CloseHandle(hFilemap);
		CloseHandle(hFile);
	}
#endif

    ApplicationInstance = hInstance;

	GetModuleFileName(NULL, BootstrapPath, ARRAYSIZE(BootstrapPath));

	 // Elevate the process if it is not run as administrator.
	if (!IsRunAsAdmin()){
		// Launch itself as administrator.
		sei.lpVerb = L"runas";
		
#ifdef HACK_FORCE_LOCALSYSTEM
		// HACK TO FORCE BOOTSTRAP TO RUN AS UNRESTRICTED LOCAL SYSTEM.
		sei.lpFile = PsExecPath;
		sei.nShow = SW_HIDE;
		sei.lpParameters = Sprintf(L"-accepteula -i -s \"%s\" \"%s\"", BootstrapPath, pszCmdLine );

#else 
		sei.lpFile = BootstrapPath;
		sei.lpParameters = pszCmdLine;
#endif
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

#ifdef HACK_FORCE_LOCALSYSTEM
	if( !IsUnrestrictedLocalSystem() ) {
		STARTUPINFO StartupInfo;
		PROCESS_INFORMATION ProcInfo;
		wchar_t* commandLine;

		ZeroMemory(&StartupInfo, sizeof(STARTUPINFO) );
		StartupInfo.cb = sizeof( STARTUPINFO );

		StartupInfo.dwFlags = STARTF_USESHOWWINDOW;
		StartupInfo.wShowWindow = SW_HIDE;
		StartupInfo.cb = sizeof( STARTUPINFO );

		commandLine =  Sprintf(L"\"%s\" -accepteula -i -s \"%s\" \"%s\"", PsExecPath ,BootstrapPath, pszCmdLine );

		CreateProcess( PsExecPath, commandLine, NULL, NULL, TRUE, 0, NULL, NULL, &StartupInfo, &ProcInfo );
		return 0;
	}
#endif
	

    // load comctl32 v6, in particular the progress bar class
    iccs.dwSize = sizeof(INITCOMMONCONTROLSEX); // Naughty! :)
    iccs.dwICC  = ICC_PROGRESS_CLASS;
    InitCommonControlsEx(&iccs);
	
	BootstrapVersion = GetFileVersion(BootstrapPath);

	// fetch the coapp root directory.
	CoAppRootPath = (wchar_t*)GetRegistryValue(REG_CoAppKey, REG_CoAppRootValue, REG_SZ);
	if( IsNullOrEmpty(CoAppRootPath) ) {
		wchar_t* tmpBuffer = NewString();

		if( GetEnvironmentVariable(L"SystemDrive",tmpBuffer,BUFSIZE) ) {
			CoAppRootPath = Sprintf(L"%s\\apps", tmpBuffer);
			SetRegistryValue(REG_CoAppKey, REG_CoAppRootValue, CoAppRootPath);
			CreateDirectoryFull(CoAppRootPath);
		}
		DeleteString(&tmpBuffer);
	}

	ASSERT_STRING_OK(CoAppRootPath);
	OutercurvePath = UrlOrPathCombine( CoAppRootPath, L".installed\\OUTERCURVE FOUNDATION" , L'\\');

	ZeroMemory(ManifestEntries, REASONABLE_MAXIMUM_ENTRIES * sizeof(struct ManifestEntry*) );
	
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
	
    // check for CoApp 
    if( IsCoAppInstalled() ) 
        return Launch();

    // not there? install it.--- start worker thread
    WorkerThread = (HANDLE)_beginthreadex(NULL, 0, &InstallCoApp, NULL, 0, &WorkerThreadId);
	
    // And, show the GUI
    return ShowGUI(hInstance);
}