#pragma once
#include "chainer.h"

HANDLE sectionHandle = NULL;
HANDLE eventHandle = NULL;
struct MmioDataStructure* mmioData = NULL;
wchar_t* eventName = L"CoAppEvent";
wchar_t* sectionName = L"CoAppBootstrapper";

// This is called by the chainer to force the chained setup to be cancelled
void AbortChain() {
    //Don't do anything if it is invalid.
    if (NULL == mmioData) {
        return;
    }

    // set cancel flags
    mmioData->m_downloadAbort= TRUE;
    mmioData->m_installAbort = TRUE;
}

// Called by the chainer to start the chained setup - this blocks untils the setup is complete
HRESULT MonitorChainer( HANDLE process, void (*OnProgress)(wchar_t* step,unsigned int progress)) {
    HANDLE handles[2] = { process, eventHandle };
	int totalProgress = 0;
	HRESULT result;

	sectionHandle = CreateFileMapping(INVALID_HANDLE_VALUE, NULL, PAGE_READWRITE, 0, sizeof( struct MmioDataStructure), sectionName);
    eventHandle = CreateEvent(NULL, FALSE, FALSE, eventName);
	mmioData = (struct MmioDataStructure*)(MapViewOfFile(sectionHandle, FILE_MAP_WRITE, 0, 0, sizeof(struct MmioDataStructure)));

    // Common items for download and install
    wcscpy_s(mmioData->m_szEventName, MAX_PATH, eventName);

    // Download specific data
    mmioData->m_downloadFinished = FALSE;
    mmioData->m_downloadProgressSoFar = 0;
    mmioData->m_hrDownloadFinished = E_PENDING;
    mmioData->m_downloadAbort = FALSE;

    // Install specific data
    mmioData->m_installFinished = FALSE;
    mmioData->m_installProgressSoFar = 0;
    mmioData->m_hrInstallFinished = E_PENDING;
    mmioData->m_installAbort = FALSE;
    mmioData->m_hrInternalError = S_OK;

    while(!(mmioData->m_downloadFinished && mmioData->m_installFinished)) {
        DWORD ret = WaitForMultipleObjects(2, handles, FALSE, 100); // INFINITE ??
        switch(ret) {
        case WAIT_OBJECT_0: { // process handle closed.  Maybe it blew up, maybe it's just really fast.  Let's find out.
            if ((mmioData->m_downloadFinished && mmioData->m_installFinished) == FALSE) { 
				goto fin; // huh, not a good sign
            }
            break;
        }

        case WAIT_OBJECT_0 + 1:
			totalProgress = (mmioData->m_downloadProgressSoFar + mmioData->m_installProgressSoFar)/6; // (gives a number between 0-85%)
			if( totalProgress > 100 ) 
				totalProgress = 100;

            OnProgress(mmioData->m_szCurrentItemStep, totalProgress);
            break;

        default:
            break;
        }		
    }

fin:
	if (mmioData) {
        UnmapViewOfFile(mmioData);
    }
	
	if (mmioData->m_hrInstallFinished != S_OK) {
        result = mmioData->m_hrInstallFinished;
    }
    else {
        result = mmioData->m_hrDownloadFinished;
    }

	mmioData = NULL;

	return result;
}