//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
#pragma once

#define _WIN32_WINNT _WIN32_WINNT_WS03 
#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers

#include <SDKDDKVer.h>
#include <windows.h>

extern LPWSTR CommandLine;
extern LPWSTR MsiFile;
extern LPWSTR MsiDirectory;
extern LPWSTR ManifestFilename;

#define BUFSIZE 8192

#define WIDEN2(x) L ## x
#define WIDEN(x) WIDEN2(x)
#define __WFUNCTION__ WIDEN(__FUNCTION__)

#define EXIT_UNABLE_TO_DOWNLOAD_REQUIRED_PACKAGE 1
#define EXIT_PACKAGE_FAILED_SIGNATURE_VALIDATION 2
#define EXIT_BOOTSTRAP_MANIFEST_PARSE_FAILURE    3
#define EXIT_NULL_POINTER						 4
#define EXIT_ADMIN_RIGHTS_REQUIRED				 5
#define EXIT_MEMORY_ALLOCATION_FAILURE			 6
#define EXIT_INVALID_STRING						 7
#define EXIT_STRING_PRINTF_ERROR				 8
#define EXIT_UNABLE_TO_FIND_TEMPDIR			     9
#define EXIT_UNKNOWN_COMPONENT_TYPE			     10
#define EXIT_NO_MSI_COMMANDLINE					 11
#define EXIT_BOOTSTRAP_DIDNT_INSTALL_COAPP		 12

#define ASSERT_NOT_NULL( pointer ) { if(!pointer) { TerminateApplicationWithError(EXIT_NULL_POINTER,L"Internal Error: An unexpected error has ocurred in function:" __WFUNCTION__); } }
#define ASSERT_STRING_SIZE( stringpointer ) { if(SafeStringLengthInCharacters(stringpointer)< 0) { TerminateApplicationWithError(EXIT_INVALID_STRING, L"Internal Error: An unexpected error has ocurred in function:" __WFUNCTION__); } }
#define ASSERT_STRING_OK( stringpointer) ASSERT_NOT_NULL( stringpointer ); ASSERT_STRING_SIZE( stringpointer );

wchar_t* NewString();
void DeleteString(wchar_t* string);
wchar_t* DuplicateString( const wchar_t* text );
wchar_t* DuplicateAndTrimString(const wchar_t* text );
size_t SafeStringLengthInCharacters(const wchar_t* text);
size_t SafeStringLengthInBytes(const wchar_t* text);
BOOL IsPathURL(const wchar_t* serverPath);
BOOL IsNullOrEmpty(const wchar_t* text);
wchar_t* UrlOrPathCombine(const wchar_t* path, const wchar_t* name, wchar_t seperator);
wchar_t* GetModuleFolder( HMODULE module );
wchar_t* GetModuleFullPath( HMODULE module );
wchar_t* TempFileName(const wchar_t* name);
wchar_t* UniqueTempFileName(const wchar_t* name,const wchar_t* extension);
BOOL IsRunAsAdmin();
void* GetRegistryValue(const wchar_t* keyname, const wchar_t* valueName, DWORD expectedDataType );
BOOL RegistryKeyPresent(const wchar_t* regkey);
wchar_t* GetPathFromRegistry();
wchar_t* GetFolderFromPath( const wchar_t* path );
wchar_t* GetModuleFullPath( HMODULE module );
wchar_t* GetModuleFolder( HMODULE module );
int DownloadFile(const wchar_t* URL, const wchar_t* destinationFilename, const wchar_t* cosmeticName);
wchar_t* GetWinSxSResourcePathViaManifest(HMODULE module, int resourceIdForManifest, wchar_t* itemInAssembly );
BOOL IsEmbeddedSignatureValid(LPCWSTR pwszSourceFile);
void TerminateApplicationWithError(int errorLevel , const wchar_t* errorMessage, ... ); 



