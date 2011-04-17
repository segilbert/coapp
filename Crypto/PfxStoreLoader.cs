

namespace CoApp.Toolkit.Crypto
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Runtime.InteropServices;
    using System.IO;

    public static class PfxStoreLoader
    {
        public static X509Store Load(string file, string password)
        {
            if (file == null || password == null)
                return null;

            if (!File.Exists(file))
                return null;

            var fileContents = File.ReadAllBytes(file);
            var ptr = Marshal.AllocHGlobal(fileContents.Length);
            Marshal.Copy(fileContents, 0, ptr, fileContents.Length);
            var cryptBlob = new CRYPT_DATA_BLOB() { cbData = (uint)fileContents.Length, pbData = ptr };
            if (!PFXIsPFXBlob(ref cryptBlob))
            {
                return null;
            }
            if (!PFXVerifyPassword(ref cryptBlob, password))
            {
                return null;
            }

            X509Store store = null;
            var storePtr = PFXImportCertStore(ref cryptBlob, password);
            store = new X509Store(storePtr);
            Marshal.FreeHGlobal(ptr);
            return store;

        }

        [DllImport("crypt32.dll", SetLastError = true)]
        private static extern IntPtr PFXImportCertStore(
            ref CRYPT_DATA_BLOB pPfx,
            [MarshalAs(UnmanagedType.LPWStr)] String szPassword,
            uint dwFlags = 0);

        [StructLayout(LayoutKind.Sequential)]
        internal struct CRYPT_DATA_BLOB
        {
            [MarshalAs(UnmanagedType.U4)]
            public uint cbData;
            public IntPtr pbData;
        }

       [DllImport("crypt32.dll")]
       private static extern bool PFXIsPFXBlob(
                    ref CRYPT_DATA_BLOB pPfx);
       
       [DllImport("crypt32.dll")]
       private static extern bool PFXVerifyPassword(
           ref CRYPT_DATA_BLOB pPfx,
           [MarshalAs(UnmanagedType.LPWStr)] String szPassword,
            uint dwFlags = 0);
    }
}
