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
#include "BootstrapGUI.h"
#include "BootstrapUtility.h"
#include <Strsafe.h>

#define BUFSIZE				8192

HWND StatusDialog = NULL;
BOOL Ready = FALSE;

void SetStatusMessage(  const wchar_t* format, ... ) {
	va_list args;
	wchar_t* text = NewString();

	va_start(args, format);
	StringCbVPrintf(text,BUFSIZE,format, args);

	// recipient must free the text buffer!
	PostMessage(StatusDialog, SETSTATUSMESSAGE, 0, (LPARAM)text );
	Sleep(20);
}

void SetLargeMessageText(const wchar_t* ps_text) {
	POINT point;
	RECT rectangle;
	HWND windowHandle = GetDlgItem(StatusDialog,IDC_STATICTEXT3);

	SetWindowText(windowHandle, ps_text);
	
	GetWindowRect(windowHandle, &rectangle);
	point.x=rectangle.left; 
	point.y=rectangle.top;

	ScreenToClient(StatusDialog, &point);
	rectangle.left = point.x; 
	rectangle.top = point.y;
	point.x=rectangle.right; 
	point.y=rectangle.bottom;

	ScreenToClient(StatusDialog, &point);
	rectangle.right = point.x; 
	rectangle.bottom = point.y;

	RedrawWindow(StatusDialog, &rectangle, NULL, RDW_INVALIDATE);
}

void SetProgressValue( int percentage ) {
	if( percentage > 100 ) {
		percentage = 100;
	}
	PostMessage(StatusDialog, SETPROGRESS, (WPARAM)(percentage),0 );
	Sleep(20);
}

INT_PTR CALLBACK DialogProc (HWND hwnd,  UINT message, WPARAM wParam,  LPARAM lParam) {
	HDC staticControl;
	
	switch (message) {

		case SETSTATUSMESSAGE: 
			SendMessage( GetDlgItem( hwnd, IDC_STATICTEXT1), WM_SETTEXT, wParam, lParam );
			free((void*)lParam); // caller allocated the message, we need to free.
		break;

		case SETPROGRESS: 
			SendMessage( GetDlgItem( hwnd, IDC_PROGRESS2), PBM_SETPOS,  wParam, lParam );
		break;
		
		case WM_CTLCOLORSTATIC: {

			staticControl = (HDC) wParam;
			// SetTextColor(staticControl, RGB(255,255,255));
			if( lParam == (LPARAM)GetDlgItem( hwnd, IDC_STATICTEXT1) ||  lParam == (LPARAM)GetDlgItem( hwnd, IDC_STATICTEXT2) ) {
				SetBkColor(staticControl, RGB(255,255,255));
				return (INT_PTR)CreateSolidBrush(RGB(255,255,255));
			}
			if( lParam == (LPARAM)GetDlgItem( hwnd, IDC_STATICTEXT3 )  ) {
				SetBkMode(staticControl , TRANSPARENT );
			}

			return (INT_PTR)GetStockObject(NULL_BRUSH);
		}

		case WM_DESTROY:
		case WM_COMMAND:
			PostQuitMessage(0);
			PostQuitMessage(0);
			return TRUE;

		case WM_CLOSE:
			DestroyWindow (hwnd);
		case WM_INITDIALOG:
			return TRUE;
			break;
	}
	return FALSE;
}


int ShowGUI( HINSTANCE hInstance ) {
	MSG  message;
	int status;
	int i;
	HWND image;
	HBITMAP gradientBitmap ;
	HANDLE bigTextFont;

	// the rest of this is just to keep the user busy looking at an awesome dialog while the real work goes on.
	bigTextFont =CreateFont (22, 0, 0, 0, FW_DONTCARE, FALSE, FALSE, FALSE, ANSI_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, DEFAULT_PITCH | FF_SWISS, L"Calibri");
  
	StatusDialog = CreateDialog(hInstance, MAKEINTRESOURCE(IDD_DIALOG1), NULL, DialogProc );

	// pretty gradient... oh so pretty.
	gradientBitmap = LoadBitmap(hInstance, MAKEINTRESOURCE(IDB_BITMAP_GRADIENT));
	for(i=0;i<40;i++) {
		image = CreateWindowEx(0, L"STATIC", L"", WS_CHILD | SS_BITMAP | WS_VISIBLE, 0,i,260,1,StatusDialog, (HMENU)100+i, hInstance , NULL);
		SendMessage(image, STM_SETIMAGE, (WPARAM)IMAGE_BITMAP,(LPARAM)gradientBitmap);
	}

	// Large Text String (on top of images)
	image = CreateWindowEx(0, L"STATIC", L"", WS_CHILD | WS_VISIBLE, 7,7,240,32,StatusDialog, (HMENU)IDC_STATICTEXT3, hInstance , NULL);

	// set progressbar to 0-100
	SendMessage( GetDlgItem( StatusDialog, IDC_PROGRESS2), PBM_SETRANGE, 0, MAKELPARAM(0,100) );
	SetProgressValue( 1 );

	// set Large Message Text Font
	SendMessage( GetDlgItem( StatusDialog, IDC_STATICTEXT3), WM_SETFONT, (WPARAM)bigTextFont ,TRUE);
	SetLargeMessageText(L"Installing CoApp...");

	Ready = TRUE;

	// main thread message pump.
	while ((status = GetMessage(& message, 0, 0, 0)) != 0){
		if (status == -1)
			return -1;
		if (!IsDialogMessage (StatusDialog, & message)){
			TranslateMessage ( &message );
			DispatchMessage ( &message );
		}
	}

	return 0;
}