//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------
#include <Windows.h>
#include <malloc.h>
#include <process.h>

BOOL Done;
BOOL Stopped = TRUE;
HANDLE WorkerThread = NULL;
unsigned WorkerThreadId = 0;
BOOL IsExplorer = FALSE;

void ForceEnvironmentReload() {
	int i = 0;
    LSTATUS status = 0;
	DWORD longestName = 0, longestValue = 0;
	wchar_t* name = NULL;
	wchar_t* data = NULL;
	HKEY hklm = NULL;

	do {
		//
		// Let's size out our required buffer lengths.
		//
		RegQueryInfoKey(hklm, NULL, NULL, NULL, NULL, NULL, NULL, NULL, &longestName, &longestValue, NULL, NULL);
		
		name = (wchar_t*)malloc((++longestName) * sizeof(wchar_t) );
		ZeroMemory(name, longestName);

		data = (wchar_t*)malloc((++longestValue)* sizeof(wchar_t) );
		ZeroMemory(data, longestValue);
            
		//
		// Grab the values.
		//
		if(RegEnumValueW(hklm, i, name, &longestName, NULL, NULL, (LPBYTE)data, &longestValue) != ERROR_SUCCESS) {            
			goto fin;
		}

		if(SetEnvironmentVariableW(name, data) == FALSE) {
			goto fin;
		}

	} while(++i);

	fin:
	if(hklm != NULL) {
		RegCloseKey(hklm);
		free(name);
		free(data);
		name = NULL;
		data = NULL;
		hklm = NULL;
	}
}

unsigned __stdcall ListenForEvent(void* parameter) {
	HANDLE handle;
	handle = CreateEvent( NULL, TRUE,FALSE, L"Global\\CoApp.Reload.Environment" ); 
	
	while( !Stopped  ) {
		OutputDebugString(L"Rehash.DLL is waiting on global event");
		switch( WaitForSingleObject( handle , INFINITE ) ) {
			case WAIT_OBJECT_0:
				OutputDebugString(L"Rehash.DLL is trying to force environment reload");
				ForceEnvironmentReload();
				if( IsExplorer ) { //GetShellWindow()
					OutputDebugString(L"Rehash.DLL is trying send HWND_BROADCAST WM_SETTINGCHANGE");
					SendMessageTimeoutA(HWND_BROADCAST, WM_SETTINGCHANGE, 0, (LPARAM)"Environment", SMTO_ABORTIFHUNG, 1000, NULL);
				}
				Sleep(3000); // ensure there is plent of time between rehashes.
			break;

			default:
				OutputDebugString(L"Rehash.DLL is ceasing to wait for events");
				Stopped = TRUE;
				break;
		}
	}
	return 0;
}

BOOL APIENTRY DllMain( HINSTANCE hModule, DWORD ul_reason_for_call, LPVOID lpReserved ) {
	DWORD processId;
	if((ul_reason_for_call == DLL_PROCESS_ATTACH || ul_reason_for_call == DLL_THREAD_ATTACH ) && Stopped ) {
		Stopped = FALSE;
		// are we inside the explorer shell?
		GetWindowThreadProcessId(GetShellWindow(),&processId);
		IsExplorer = (processId == GetCurrentProcessId());
		
		// check Stopped again...
		WorkerThread = (HANDLE)_beginthreadex(NULL, 0, &ListenForEvent, NULL, 0, &WorkerThreadId);
    }
    return TRUE;
}