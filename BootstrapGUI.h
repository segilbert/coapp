//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
#pragma once

#define SETSTATUSMESSAGE	WM_USER+1
#define SETPROGRESS			WM_USER+2


extern HWND StatusDialog;
extern BOOL Ready;

void SetStatusMessage(  const wchar_t* format, ... );
INT_PTR CALLBACK DialogProc (HWND hwnd,  UINT message, WPARAM wParam,  LPARAM lParam);
void SetLargeMessageText(const wchar_t* ps_text);
void SetProgressValue( int percentage );
int ShowGUI( HINSTANCE hInstance );