//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Changes Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
// Original Source: http://www.pinvoke.net/default.aspx/wintrust.winverifytrust
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Crypto {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography.X509Certificates;
    using Win32;

    public enum WinTrustDataUIChoice : uint {
        All = 1,
        None = 2,
        NoBad = 3,
        NoGood = 4
    }

    public enum WinTrustDataRevocationChecks : uint {
        None = 0x00000000,
        WholeChain = 0x00000001
    }

    public enum WinTrustDataChoice : uint {
        File = 1,
        Catalog = 2,
        Blob = 3,
        Signer = 4,
        Certificate = 5
    }

    public enum WinTrustDataStateAction : uint {
        Ignore = 0x00000000,
        Verify = 0x00000001,
        Close = 0x00000002,
        AutoCache = 0x00000003,
        AutoCacheFlush = 0x00000004
    }

    [FlagsAttribute]
    public enum WinTrustDataProvFlags : uint {
        UseIe4TrustFlag = 0x00000001,
        NoIe4ChainFlag = 0x00000002,
        NoPolicyUsageFlag = 0x00000004,
        RevocationCheckNone = 0x00000010,
        RevocationCheckEndCert = 0x00000020,
        RevocationCheckChain = 0x00000040,
        RevocationCheckChainExcludeRoot = 0x00000080,
        SaferFlag = 0x00000100,
        HashOnlyFlag = 0x00000200,
        UseDefaultOsverCheck = 0x00000400,
        LifetimeSigningFlag = 0x00000800,
        CacheOnlyUrlRetrieval = 0x00001000      // affects CRL retrieval and AIA retrieval
    }

    public enum WinTrustDataUIContext : uint {
        Execute = 0,
        Install = 1
    }

    public enum WinVerifyTrustResult : uint {
        Success = 0,
        ProviderUnknown = 0x800b0001,           // The trust provider is not recognized on this system
        ActionUnknown = 0x800b0002,             // The trust provider does not support the specified action
        SubjectFormUnknown = 0x800b0003,        // The trust provider does not support the form specified for the subject
        SubjectNotTrusted = 0x800b0004          // The subject failed the specified verification action
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    class WinTrustFileInfo {
        UInt32 StructSize = (UInt32)Marshal.SizeOf(typeof(WinTrustFileInfo));
        IntPtr FilePath;                        // required, file name to be verified
        IntPtr hFile = IntPtr.Zero;             // optional, open handle to FilePath
        IntPtr pgKnownSubject = IntPtr.Zero;    // optional, subject type if it is known

        public WinTrustFileInfo(String _filePath) {
            FilePath = Marshal.StringToCoTaskMemAuto(_filePath);
        }
        ~WinTrustFileInfo() {
            Marshal.FreeCoTaskMem(FilePath);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public class WinTrustData {
        UInt32 StructSize = (UInt32)Marshal.SizeOf(typeof(WinTrustData));
        IntPtr PolicyCallbackData = IntPtr.Zero;
        IntPtr SIPClientData = IntPtr.Zero;
        // required: UI choice
        WinTrustDataUIChoice UIChoice = WinTrustDataUIChoice.None;
        // required: certificate revocation check options
        WinTrustDataRevocationChecks RevocationChecks = WinTrustDataRevocationChecks.None;
        // required: which structure is being passed in?
        WinTrustDataChoice UnionChoice = WinTrustDataChoice.File;
        // individual file
        IntPtr FileInfoPtr;
        WinTrustDataStateAction StateAction = WinTrustDataStateAction.Ignore;
        IntPtr StateData = IntPtr.Zero;
        String URLReference = null;
        WinTrustDataProvFlags ProvFlags = WinTrustDataProvFlags.SaferFlag;
        WinTrustDataUIContext UIContext = WinTrustDataUIContext.Execute;

        // constructor for silent WinTrustDataChoice.File check
        public WinTrustData(String filename) {
            var wtfiData = new WinTrustFileInfo(filename);
            FileInfoPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(WinTrustFileInfo)));
            Marshal.StructureToPtr(wtfiData, FileInfoPtr, false);
        }
        ~WinTrustData() {
            Marshal.FreeCoTaskMem(FileInfoPtr);
        }
    }

    public class Verifier {
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        // GUID of the action to perform
        private const string WINTRUST_ACTION_GENERIC_VERIFY_V2 = "{00AAC56B-CD44-11d0-8CC2-00C04FC295EE}";

        public static bool HasValidSignature(string fileName) {
            WinTrustData wtd = new WinTrustData(fileName);
            Guid guidAction = new Guid(WINTRUST_ACTION_GENERIC_VERIFY_V2);
            WinVerifyTrustResult result = WinTrust.WinVerifyTrust(INVALID_HANDLE_VALUE, guidAction, wtd);
            bool ret = (result == WinVerifyTrustResult.Success);
            return ret;
        }

        public static Dictionary<string, string> GetPublisherInformation(string filename) {
            var cert = new X509Certificate2(filename);
            var fields= cert.Subject.Split(new []{','},StringSplitOptions.RemoveEmptyEntries);
            var result = fields.Select(f => f.Split('=')).Where(s => s.Length > 1).ToDictionary(s => s[0], s => s[1]);
            result.Add("PublisherName", fields[0].Split('=')[1]);
            return result;
        }

       public static void GetSignatureInformation(string filename) {
           if (HasValidSignature(filename)) {
               X509Certificate2 cert = new X509Certificate2(filename);
               Console.WriteLine("Cert: {0}", cert.Subject);

               
               X509Chain ch = new X509Chain();

               ch.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;

               ch.ChainPolicy.RevocationMode = X509RevocationMode.Online;

               ch.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);

               ch.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;

               ch.Build(cert);


               ch.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
               ch.ChainPolicy.RevocationMode = X509RevocationMode.Online;


               Console.WriteLine("Chain Information");
               
               Console.WriteLine("Chain revocation flag: {0}", ch.ChainPolicy.RevocationFlag);
               Console.WriteLine("Chain revocation mode: {0}", ch.ChainPolicy.RevocationMode);
               Console.WriteLine("Chain verification flag: {0}", ch.ChainPolicy.VerificationFlags);
               Console.WriteLine("Chain verification time: {0}", ch.ChainPolicy.VerificationTime);
               Console.WriteLine("Chain status length: {0}", ch.ChainStatus.Length);
               Console.WriteLine("Chain application policy count: {0}", ch.ChainPolicy.ApplicationPolicy.Count);
               Console.WriteLine("Chain certificate policy count: {0} {1}", ch.ChainPolicy.CertificatePolicy.Count, Environment.NewLine);
               //Output chain element information.
               Console.WriteLine("Chain Element Information");
               Console.WriteLine("Number of chain elements: {0}", ch.ChainElements.Count);

               foreach (X509ChainElement element in ch.ChainElements) {
                   
                   Console.WriteLine("Element issuer name: {0}", element.Certificate.Issuer);
                   Console.WriteLine("Element certificate valid until: {0}", element.Certificate.NotAfter);
                   Console.WriteLine("Element certificate is valid: {0}", element.Certificate.Verify());
                   Console.WriteLine("Element error status length: {0}", element.ChainElementStatus.Length);
                   Console.WriteLine("Element information: {0}", element.Information);
                   Console.WriteLine("Number of element extensions: {0}{1}", element.Certificate.Extensions.Count, Environment.NewLine);

               }

               if (ch.ChainStatus.Length > 0) {
                   for (int index = 0; index < ch.ChainStatus.Length; index++) {
                       Console.WriteLine(ch.ChainStatus[index].Status);
                       Console.WriteLine(ch.ChainStatus[index].StatusInformation);
                   }
               }

               Console.WriteLine("Cert Valid?: {0}", cert.Verify());

           }
       }

    }
}