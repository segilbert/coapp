//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CoApp.Toolkit.Win32
{
    using Microsoft.Win32.SafeHandles;

   

    /// <summary>
    /// Summary description for Win32.
    /// </summary>
    public class User32
    {
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd,int id,int fsModifiers,int vlc);
        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);
        [DllImport("user32.dll")]
        public static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Sends the specified message to a window or windows. The function 
        /// calls the window procedure for the specified window and does not 
        /// return until the window procedure has processed the message. 
        /// </summary>
        /// <param name="hWnd">
        /// Handle to the window whose window procedure will receive the 
        /// message.
        /// </param>
        /// <param name="Msg">Specifies the message to be sent.</param>
        /// <param name="wParam">
        /// Specifies additional message-specific information.
        /// </param>
        /// <param name="lParam">
        /// Specifies additional message-specific information.
        /// </param>
        /// <returns></returns>
        [DllImport("user32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int SendMessage(IntPtr hWnd, UInt32 Msg, int wParam, IntPtr lParam);

    }

    public class Kernel32
    {
        [DllImport("kernel32.dll")]
        public static extern int GlobalAddAtom(string name);
        [DllImport("kernel32.dll")]
        public static extern int GlobalDeleteAtom(int atom);
        [DllImport("kernel32.dll")]
        public static extern IntPtr GlobalLock(IntPtr hMem);
        [DllImport("kernel32.dll")]
        public static extern bool GlobalUnlock(IntPtr hMem);
    }

    public class Winmm {
        protected delegate void MidiCallback(int handle, int msg, int instance, int param1, int param2);

        [DllImport("winmm.dll")]
        private static extern int midiOutOpen(ref int handle, int deviceID, MidiCallback proc, int instance, int flags);

        [DllImport("winmm.dll")]
        protected static extern int midiOutShortMsg(int handle, int message);
              
    }

    public class Advapi32 {
        // Token Specific Access Rights

        public const UInt32 STANDARD_RIGHTS_REQUIRED = 0x000F0000;
        public const UInt32 STANDARD_RIGHTS_READ = 0x00020000;
        public const UInt32 TOKEN_ASSIGN_PRIMARY = 0x0001;
        public const UInt32 TOKEN_DUPLICATE = 0x0002;
        public const UInt32 TOKEN_IMPERSONATE = 0x0004;
        public const UInt32 TOKEN_QUERY = 0x0008;
        public const UInt32 TOKEN_QUERY_SOURCE = 0x0010;
        public const UInt32 TOKEN_ADJUST_PRIVILEGES = 0x0020;
        public const UInt32 TOKEN_ADJUST_GROUPS = 0x0040;
        public const UInt32 TOKEN_ADJUST_DEFAULT = 0x0080;
        public const UInt32 TOKEN_ADJUST_SESSIONID = 0x0100;
        public const UInt32 TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);
        public const UInt32 TOKEN_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED |
            TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_IMPERSONATE |
            TOKEN_QUERY | TOKEN_QUERY_SOURCE | TOKEN_ADJUST_PRIVILEGES |
            TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT | TOKEN_ADJUST_SESSIONID);


        public const Int32 ERROR_INSUFFICIENT_BUFFER = 122;


        // Integrity Levels

        public const Int32 SECURITY_MANDATORY_UNTRUSTED_RID = 0x00000000;
        public const Int32 SECURITY_MANDATORY_LOW_RID = 0x00001000;
        public const Int32 SECURITY_MANDATORY_MEDIUM_RID = 0x00002000;
        public const Int32 SECURITY_MANDATORY_HIGH_RID = 0x00003000;
        public const Int32 SECURITY_MANDATORY_SYSTEM_RID = 0x00004000;


        /// <summary>
        /// The function opens the access token associated with a process.
        /// </summary>
        /// <param name="hProcess">
        /// A handle to the process whose access token is opened.
        /// </param>
        /// <param name="desiredAccess">
        /// Specifies an access mask that specifies the requested types of 
        /// access to the access token. 
        /// </param>
        /// <param name="hToken">
        /// Outputs a handle that identifies the newly opened access token 
        /// when the function returns.
        /// </param>
        /// <returns></returns>
        [DllImport("advapi32", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool OpenProcessToken(IntPtr hProcess,
            UInt32 desiredAccess, out SafeTokenHandle hToken);


        /// <summary>
        /// The function creates a new access token that duplicates one 
        /// already in existence.
        /// </summary>
        /// <param name="ExistingTokenHandle">
        /// A handle to an access token opened with TOKEN_DUPLICATE access.
        /// </param>
        /// <param name="ImpersonationLevel">
        /// Specifies a SECURITY_IMPERSONATION_LEVEL enumerated type that 
        /// supplies the impersonation level of the new token.
        /// </param>
        /// <param name="DuplicateTokenHandle">
        /// Outputs a handle to the duplicate token. 
        /// </param>
        /// <returns></returns>
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public extern static bool DuplicateToken(
            SafeTokenHandle ExistingTokenHandle,
            SECURITY_IMPERSONATION_LEVEL ImpersonationLevel,
            out SafeTokenHandle DuplicateTokenHandle);


        /// <summary>
        /// The function retrieves a specified type of information about an 
        /// access token. The calling process must have appropriate access 
        /// rights to obtain the information.
        /// </summary>
        /// <param name="hToken">
        /// A handle to an access token from which information is retrieved.
        /// </param>
        /// <param name="tokenInfoClass">
        /// Specifies a value from the TOKEN_INFORMATION_CLASS enumerated 
        /// type to identify the type of information the function retrieves.
        /// </param>
        /// <param name="pTokenInfo">
        /// A pointer to a buffer the function fills with the requested 
        /// information.
        /// </param>
        /// <param name="tokenInfoLength">
        /// Specifies the size, in bytes, of the buffer pointed to by the 
        /// TokenInformation parameter. 
        /// </param>
        /// <param name="returnLength">
        /// A pointer to a variable that receives the number of bytes needed 
        /// for the buffer pointed to by the TokenInformation parameter. 
        /// </param>
        /// <returns></returns>
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetTokenInformation(
            SafeTokenHandle hToken,
            TOKEN_INFORMATION_CLASS tokenInfoClass,
            IntPtr pTokenInfo,
            Int32 tokenInfoLength,
            out Int32 returnLength);


        /// <summary>
        /// Sets the elevation required state for a specified button or 
        /// command link to display an elevated icon. 
        /// </summary>
        public const UInt32 BCM_SETSHIELD = 0x160C;

        /// <summary>
        /// The function returns a pointer to a specified subauthority in a 
        /// security identifier (SID). The subauthority value is a relative 
        /// identifier (RID).
        /// </summary>
        /// <param name="pSid">
        /// A pointer to the SID structure from which a pointer to a 
        /// subauthority is to be returned.
        /// </param>
        /// <param name="nSubAuthority">
        /// Specifies an index value identifying the subauthority array 
        /// element whose address the function will return.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is a pointer to the 
        /// specified SID subauthority. To get extended error information, 
        /// call GetLastError. If the function fails, the return value is 
        /// undefined. The function fails if the specified SID structure is 
        /// not valid or if the index value specified by the nSubAuthority 
        /// parameter is out of bounds.
        /// </returns>
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetSidSubAuthority(IntPtr pSid, UInt32 nSubAuthority);
    }


    /// <summary>
    /// The TOKEN_INFORMATION_CLASS enumeration type contains values that 
    /// specify the type of information being assigned to or retrieved from 
    /// an access token.
    /// </summary>
    public enum TOKEN_INFORMATION_CLASS {
        TokenUser = 1,
        TokenGroups,
        TokenPrivileges,
        TokenOwner,
        TokenPrimaryGroup,
        TokenDefaultDacl,
        TokenSource,
        TokenType,
        TokenImpersonationLevel,
        TokenStatistics,
        TokenRestrictedSids,
        TokenSessionId,
        TokenGroupsAndPrivileges,
        TokenSessionReference,
        TokenSandBoxInert,
        TokenAuditPolicy,
        TokenOrigin,
        TokenElevationType,
        TokenLinkedToken,
        TokenElevation,
        TokenHasRestrictions,
        TokenAccessInformation,
        TokenVirtualizationAllowed,
        TokenVirtualizationEnabled,
        TokenIntegrityLevel,
        TokenUIAccess,
        TokenMandatoryPolicy,
        TokenLogonSid,
        MaxTokenInfoClass
    }


    /// <summary>
    /// The WELL_KNOWN_SID_TYPE enumeration type is a list of commonly used 
    /// security identifiers (SIDs). Programs can pass these values to the 
    /// CreateWellKnownSid function to create a SID from this list.
    /// </summary>
    public enum WELL_KNOWN_SID_TYPE {
        WinNullSid = 0,
        WinWorldSid = 1,
        WinLocalSid = 2,
        WinCreatorOwnerSid = 3,
        WinCreatorGroupSid = 4,
        WinCreatorOwnerServerSid = 5,
        WinCreatorGroupServerSid = 6,
        WinNtAuthoritySid = 7,
        WinDialupSid = 8,
        WinNetworkSid = 9,
        WinBatchSid = 10,
        WinInteractiveSid = 11,
        WinServiceSid = 12,
        WinAnonymousSid = 13,
        WinProxySid = 14,
        WinEnterpriseControllersSid = 15,
        WinSelfSid = 16,
        WinAuthenticatedUserSid = 17,
        WinRestrictedCodeSid = 18,
        WinTerminalServerSid = 19,
        WinRemoteLogonIdSid = 20,
        WinLogonIdsSid = 21,
        WinLocalSystemSid = 22,
        WinLocalServiceSid = 23,
        WinNetworkServiceSid = 24,
        WinBuiltinDomainSid = 25,
        WinBuiltinAdministratorsSid = 26,
        WinBuiltinUsersSid = 27,
        WinBuiltinGuestsSid = 28,
        WinBuiltinPowerUsersSid = 29,
        WinBuiltinAccountOperatorsSid = 30,
        WinBuiltinSystemOperatorsSid = 31,
        WinBuiltinPrintOperatorsSid = 32,
        WinBuiltinBackupOperatorsSid = 33,
        WinBuiltinReplicatorSid = 34,
        WinBuiltinPreWindows2000CompatibleAccessSid = 35,
        WinBuiltinRemoteDesktopUsersSid = 36,
        WinBuiltinNetworkConfigurationOperatorsSid = 37,
        WinAccountAdministratorSid = 38,
        WinAccountGuestSid = 39,
        WinAccountKrbtgtSid = 40,
        WinAccountDomainAdminsSid = 41,
        WinAccountDomainUsersSid = 42,
        WinAccountDomainGuestsSid = 43,
        WinAccountComputersSid = 44,
        WinAccountControllersSid = 45,
        WinAccountCertAdminsSid = 46,
        WinAccountSchemaAdminsSid = 47,
        WinAccountEnterpriseAdminsSid = 48,
        WinAccountPolicyAdminsSid = 49,
        WinAccountRasAndIasServersSid = 50,
        WinNTLMAuthenticationSid = 51,
        WinDigestAuthenticationSid = 52,
        WinSChannelAuthenticationSid = 53,
        WinThisOrganizationSid = 54,
        WinOtherOrganizationSid = 55,
        WinBuiltinIncomingForestTrustBuildersSid = 56,
        WinBuiltinPerfMonitoringUsersSid = 57,
        WinBuiltinPerfLoggingUsersSid = 58,
        WinBuiltinAuthorizationAccessSid = 59,
        WinBuiltinTerminalServerLicenseServersSid = 60,
        WinBuiltinDCOMUsersSid = 61,
        WinBuiltinIUsersSid = 62,
        WinIUserSid = 63,
        WinBuiltinCryptoOperatorsSid = 64,
        WinUntrustedLabelSid = 65,
        WinLowLabelSid = 66,
        WinMediumLabelSid = 67,
        WinHighLabelSid = 68,
        WinSystemLabelSid = 69,
        WinWriteRestrictedCodeSid = 70,
        WinCreatorOwnerRightsSid = 71,
        WinCacheablePrincipalsGroupSid = 72,
        WinNonCacheablePrincipalsGroupSid = 73,
        WinEnterpriseReadonlyControllersSid = 74,
        WinAccountReadonlyControllersSid = 75,
        WinBuiltinEventLogReadersGroup = 76,
        WinNewEnterpriseReadonlyControllersSid = 77,
        WinBuiltinCertSvcDComAccessGroup = 78
    }

    /// <summary>
    /// The SECURITY_IMPERSONATION_LEVEL enumeration type contains values 
    /// that specify security impersonation levels. Security impersonation 
    /// levels govern the degree to which a server process can act on behalf 
    /// of a client process.
    /// </summary>
    public enum SECURITY_IMPERSONATION_LEVEL {
        SecurityAnonymous,
        SecurityIdentification,
        SecurityImpersonation,
        SecurityDelegation
    }

    /// <summary>
    /// The TOKEN_ELEVATION_TYPE enumeration indicates the elevation type of 
    /// token being queried by the GetTokenInformation function or set by 
    /// the SetTokenInformation function.
    /// </summary>
    public enum TOKEN_ELEVATION_TYPE {
        TokenElevationTypeDefault = 1,
        TokenElevationTypeFull,
        TokenElevationTypeLimited
    }

    /// <summary>
    /// The structure represents a security identifier (SID) and its 
    /// attributes. SIDs are used to uniquely identify users or groups.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SID_AND_ATTRIBUTES {
        public IntPtr Sid;
        public Int32 Attributes;
    }

    /// <summary>
    /// The structure indicates whether a token has elevated privileges.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TOKEN_ELEVATION {
        public Int32 TokenIsElevated;
    }

    /// <summary>
    /// The structure specifies the mandatory integrity level for a token.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TOKEN_MANDATORY_LABEL {
        public SID_AND_ATTRIBUTES Label;
    }

    /// <summary>
    /// Represents a wrapper class for a token handle.
    /// </summary>
    public class SafeTokenHandle : SafeHandleZeroOrMinusOneIsInvalid {
        private SafeTokenHandle()
            : base(true) {
        }

        public SafeTokenHandle(IntPtr handle)
            : base(true) {
            base.SetHandle(handle);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool CloseHandle(IntPtr handle);

        protected override bool ReleaseHandle() {
            return CloseHandle(base.handle);
        }
    }

    [Flags]
    public enum Modifiers {
        MOD_ALT=0x0001,
        MOD_CONTROL=0x0002,
        MOD_SHIFT=0x0004,
        MOD_WIN=0x0008
    }

    public enum Msgs{
        WM_NULL                   = 0x0000,
        WM_CREATE                 = 0x0001,
        WM_DESTROY                = 0x0002,
        WM_MOVE                   = 0x0003,
        WM_SIZE                   = 0x0005,
        WM_ACTIVATE               = 0x0006,
        WM_SETFOCUS               = 0x0007,
        WM_KILLFOCUS              = 0x0008,
        WM_ENABLE                 = 0x000A,
        WM_SETREDRAW              = 0x000B,
        WM_SETTEXT                = 0x000C,
        WM_GETTEXT                = 0x000D,
        WM_GETTEXTLENGTH          = 0x000E,
        WM_PAINT                  = 0x000F,
        WM_CLOSE                  = 0x0010,
        WM_QUERYENDSESSION        = 0x0011,
        WM_QUIT                   = 0x0012,
        WM_QUERYOPEN              = 0x0013,
        WM_ERASEBKGND             = 0x0014,
        WM_SYSCOLORCHANGE         = 0x0015,
        WM_ENDSESSION             = 0x0016,
        WM_SHOWWINDOW             = 0x0018,
        WM_WININICHANGE           = 0x001A,
        WM_SETTINGCHANGE          = 0x001A,
        WM_DEVMODECHANGE          = 0x001B,
        WM_ACTIVATEAPP            = 0x001C,
        WM_FONTCHANGE             = 0x001D,
        WM_TIMECHANGE             = 0x001E,
        WM_CANCELMODE             = 0x001F,
        WM_SETCURSOR              = 0x0020,
        WM_MOUSEACTIVATE          = 0x0021,
        WM_CHILDACTIVATE          = 0x0022,
        WM_QUEUESYNC              = 0x0023,
        WM_GETMINMAXINFO          = 0x0024,
        WM_PAINTICON              = 0x0026,
        WM_ICONERASEBKGND         = 0x0027,
        WM_NEXTDLGCTL             = 0x0028,
        WM_SPOOLERSTATUS          = 0x002A,
        WM_DRAWITEM               = 0x002B,
        WM_MEASUREITEM            = 0x002C,
        WM_DELETEITEM             = 0x002D,
        WM_VKEYTOITEM             = 0x002E,
        WM_CHARTOITEM             = 0x002F,
        WM_SETFONT                = 0x0030,
        WM_GETFONT                = 0x0031,
        WM_SETHOTKEY              = 0x0032,
        WM_GETHOTKEY              = 0x0033,
        WM_QUERYDRAGICON          = 0x0037,
        WM_COMPAREITEM            = 0x0039,
        WM_GETOBJECT              = 0x003D,
        WM_COMPACTING             = 0x0041,
        WM_COMMNOTIFY             = 0x0044,
        WM_WINDOWPOSCHANGING      = 0x0046,
        WM_WINDOWPOSCHANGED       = 0x0047,
        WM_POWER                  = 0x0048,
        WM_COPYDATA               = 0x004A,
        WM_CANCELJOURNAL          = 0x004B,
        WM_NOTIFY                 = 0x004E,
        WM_INPUTLANGCHANGEREQUEST = 0x0050,
        WM_INPUTLANGCHANGE        = 0x0051,
        WM_TCARD                  = 0x0052,
        WM_HELP                   = 0x0053,
        WM_USERCHANGED            = 0x0054,
        WM_NOTIFYFORMAT           = 0x0055,
        WM_CONTEXTMENU            = 0x007B,
        WM_STYLECHANGING          = 0x007C,
        WM_STYLECHANGED           = 0x007D,
        WM_DISPLAYCHANGE          = 0x007E,
        WM_GETICON                = 0x007F,
        WM_SETICON                = 0x0080,
        WM_NCCREATE               = 0x0081,
        WM_NCDESTROY              = 0x0082,
        WM_NCCALCSIZE             = 0x0083,
        WM_NCHITTEST              = 0x0084,
        WM_NCPAINT                = 0x0085,
        WM_NCACTIVATE             = 0x0086,
        WM_GETDLGCODE             = 0x0087,
        WM_SYNCPAINT              = 0x0088,
        WM_NCMOUSEMOVE            = 0x00A0,
        WM_NCLBUTTONDOWN          = 0x00A1,
        WM_NCLBUTTONUP            = 0x00A2,
        WM_NCLBUTTONDBLCLK        = 0x00A3,
        WM_NCRBUTTONDOWN          = 0x00A4,
        WM_NCRBUTTONUP            = 0x00A5,
        WM_NCRBUTTONDBLCLK        = 0x00A6,
        WM_NCMBUTTONDOWN          = 0x00A7,
        WM_NCMBUTTONUP            = 0x00A8,
        WM_NCMBUTTONDBLCLK        = 0x00A9,
        WM_KEYDOWN                = 0x0100,
        WM_KEYUP                  = 0x0101,
        WM_CHAR                   = 0x0102,
        WM_DEADCHAR               = 0x0103,
        WM_SYSKEYDOWN             = 0x0104,
        WM_SYSKEYUP               = 0x0105,
        WM_SYSCHAR                = 0x0106,
        WM_SYSDEADCHAR            = 0x0107,
        WM_KEYLAST                = 0x0108,
        WM_IME_STARTCOMPOSITION   = 0x010D,
        WM_IME_ENDCOMPOSITION     = 0x010E,
        WM_IME_COMPOSITION        = 0x010F,
        WM_IME_KEYLAST            = 0x010F,
        WM_INITDIALOG             = 0x0110,
        WM_COMMAND                = 0x0111,
        WM_SYSCOMMAND             = 0x0112,
        WM_TIMER                  = 0x0113,
        WM_HSCROLL                = 0x0114,
        WM_VSCROLL                = 0x0115,
        WM_INITMENU               = 0x0116,
        WM_INITMENUPOPUP          = 0x0117,
        WM_MENUSELECT             = 0x011F,
        WM_MENUCHAR               = 0x0120,
        WM_ENTERIDLE              = 0x0121,
        WM_MENURBUTTONUP          = 0x0122,
        WM_MENUDRAG               = 0x0123,
        WM_MENUGETOBJECT          = 0x0124,
        WM_UNINITMENUPOPUP        = 0x0125,
        WM_MENUCOMMAND            = 0x0126,
        WM_CTLCOLORMSGBOX         = 0x0132,
        WM_CTLCOLOREDIT           = 0x0133,
        WM_CTLCOLORLISTBOX        = 0x0134,
        WM_CTLCOLORBTN            = 0x0135,
        WM_CTLCOLORDLG            = 0x0136,
        WM_CTLCOLORSCROLLBAR      = 0x0137,
        WM_CTLCOLORSTATIC         = 0x0138,
        WM_MOUSEMOVE              = 0x0200,
        WM_LBUTTONDOWN            = 0x0201,
        WM_LBUTTONUP              = 0x0202,
        WM_LBUTTONDBLCLK          = 0x0203,
        WM_RBUTTONDOWN            = 0x0204,
        WM_RBUTTONUP              = 0x0205,
        WM_RBUTTONDBLCLK          = 0x0206,
        WM_MBUTTONDOWN            = 0x0207,
        WM_MBUTTONUP              = 0x0208,
        WM_MBUTTONDBLCLK          = 0x0209,
        WM_MOUSEWHEEL             = 0x020A,
        WM_PARENTNOTIFY           = 0x0210,
        WM_ENTERMENULOOP          = 0x0211,
        WM_EXITMENULOOP           = 0x0212,
        WM_NEXTMENU               = 0x0213,
        WM_SIZING                 = 0x0214,
        WM_CAPTURECHANGED         = 0x0215,
        WM_MOVING                 = 0x0216,
        WM_DEVICECHANGE           = 0x0219,
        WM_MDICREATE              = 0x0220,
        WM_MDIDESTROY             = 0x0221,
        WM_MDIACTIVATE            = 0x0222,
        WM_MDIRESTORE             = 0x0223,
        WM_MDINEXT                = 0x0224,
        WM_MDIMAXIMIZE            = 0x0225,
        WM_MDITILE                = 0x0226,
        WM_MDICASCADE             = 0x0227,
        WM_MDIICONARRANGE         = 0x0228,
        WM_MDIGETACTIVE           = 0x0229,
        WM_MDISETMENU             = 0x0230,
        WM_ENTERSIZEMOVE          = 0x0231,
        WM_EXITSIZEMOVE           = 0x0232,
        WM_DROPFILES              = 0x0233,
        WM_MDIREFRESHMENU         = 0x0234,
        WM_IME_SETCONTEXT         = 0x0281,
        WM_IME_NOTIFY             = 0x0282,
        WM_IME_CONTROL            = 0x0283,
        WM_IME_COMPOSITIONFULL    = 0x0284,
        WM_IME_SELECT             = 0x0285,
        WM_IME_CHAR               = 0x0286,
        WM_IME_REQUEST            = 0x0288,
        WM_IME_KEYDOWN            = 0x0290,
        WM_IME_KEYUP              = 0x0291,
        WM_MOUSEHOVER             = 0x02A1,
        WM_MOUSELEAVE             = 0x02A3,
        WM_CUT                    = 0x0300,
        WM_COPY                   = 0x0301,
        WM_PASTE                  = 0x0302,
        WM_CLEAR                  = 0x0303,
        WM_UNDO                   = 0x0304,
        WM_RENDERFORMAT           = 0x0305,
        WM_RENDERALLFORMATS       = 0x0306,
        WM_DESTROYCLIPBOARD       = 0x0307,
        WM_DRAWCLIPBOARD          = 0x0308,
        WM_PAINTCLIPBOARD         = 0x0309,
        WM_VSCROLLCLIPBOARD       = 0x030A,
        WM_SIZECLIPBOARD          = 0x030B,
        WM_ASKCBFORMATNAME        = 0x030C,
        WM_CHANGECBCHAIN          = 0x030D,
        WM_HSCROLLCLIPBOARD       = 0x030E,
        WM_QUERYNEWPALETTE        = 0x030F,
        WM_PALETTEISCHANGING      = 0x0310,
        WM_PALETTECHANGED         = 0x0311,
        WM_HOTKEY                 = 0x0312,
        WM_PRINT                  = 0x0317,
        WM_PRINTCLIENT            = 0x0318,
        WM_HANDHELDFIRST          = 0x0358,
        WM_HANDHELDLAST           = 0x035F,
        WM_AFXFIRST               = 0x0360,
        WM_AFXLAST                = 0x037F,
        WM_PENWINFIRST            = 0x0380,
        WM_PENWINLAST             = 0x038F,
        WM_APP                    = 0x8000,
        WM_USER                   = 0x0400,
        WM_DDE_INITIATE         = 0x03E0,
        WM_DDE_TERMINATE,
        WM_DDE_ADVISE,
        WM_DDE_UNADVISE,
        WM_DDE_ACK,
        WM_DDE_DATA,
        WM_DDE_REQUEST,
        WM_DDE_POKE,
        WM_DDE_EXECUTE
    }

    
    /// <summary>
    /// Defines a delegate for Message handling
    /// </summary>
    public delegate void MessageEventHandler(object sender, ref Message msg, ref bool handled);

    /// <summary>
    /// Inherits from System.Windows.Form.NativeWindow. Provides an Event for Message handling
    /// </summary>
    public class NativeWindowWithEvent: NativeWindow, IDisposable {
        public event MessageEventHandler ProcessMessage;

        private static NativeWindowWithEvent instance;
        private static Object instanceLock = new Object();

        protected override void WndProc(ref Message m) {
            bool handled=false;

            if (ProcessMessage!=null)
                ProcessMessage(this,ref m,ref handled);
                
                if (!handled) 
                    base.WndProc(ref m);
            }

        protected NativeWindowWithEvent() {
            var parms=new CreateParams();
            CreateHandle(parms);
        }

        public void Dispose() {
            if (Handle!=IntPtr.Zero)
                DestroyHandle();
        }

        public static NativeWindowWithEvent Instance {
            get {
                lock(instanceLock) {
                    return instance ?? (instance = new NativeWindowWithEvent());
                }
            }
        }
    }
}
