#pragma once
#include <windows.h>

// MMIO data structure for .NET installer IPC
typedef struct MmioDataStructure {
    BOOL m_downloadFinished;        // Is download done yet?
    BOOL m_installFinished;         // Is installer operation done yet?
    BOOL m_downloadAbort;           // Set to cause downloader to abort.
    BOOL m_installAbort;            // Set to cause installer operation to abort.
    HRESULT m_hrDownloadFinished;   // HRESULT for download.
    HRESULT m_hrInstallFinished;    // HRESULT for installer operation.
    HRESULT m_hrInternalError;      // Internal error from MSI if applicable.
    WCHAR m_szCurrentItemStep[MAX_PATH];   // This identifies the windows installer step being executed if an error occurs while processing an MSI, for example, "Rollback".
    unsigned char m_downloadProgressSoFar; // Download progress 0 - 255 (0 to 100% done). 
    unsigned char m_installProgressSoFar;  // Install progress 0 - 255 (0 to 100% done).
    WCHAR m_szEventName[MAX_PATH];         // Event that chainer creates and chainee opens to sync communications.
};