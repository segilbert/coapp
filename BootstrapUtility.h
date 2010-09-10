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

wchar_t* DuplicateString( const wchar_t* text );
wchar_t* GetModulePath( HMODULE module );
wchar_t* TempFileName(wchar_t* name,wchar_t* extension);
void DownloadFile(wchar_t* URL, wchar_t* destinationFilename);