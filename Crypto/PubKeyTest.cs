

namespace CoApp.Toolkit.Crypto
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using CoApp.Toolkit.Extensions;
    public class Win32
    {

        [DllImport("crypt32.dll")]
        public static extern bool CryptDecodeObject(
        uint CertEncodingType,
        uint lpszStructType,
        byte[] pbEncoded,
        uint cbEncoded,
        uint flags,
        [In, Out] byte[] pvStructInfo,
        ref uint cbStructInfo);


        [DllImport("crypt32.dll")]
        public static extern bool CryptDecodeObject(
        uint CertEncodingType,
        uint lpszStructType,
        byte[] pbEncoded,
        uint cbEncoded,
        uint flags,
        IntPtr pvStructInfo,
        ref uint cbStructInfo);


        public class PublicKeyBlob
        {
            uint SigAlgId;
            uint HashAlgId;
            ulong cbPublicKey;
            byte[] PublicKey;
        }

        [DllImport(@"advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptExportKey(IntPtr hKey, IntPtr hExpKey, uint dwBlobType, uint dwFlags, [In, Out] byte[] pbData, ref uint dwDataLen);
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct PUBKEYBLOBHEADERS
    {
        public byte bType;	//BLOBHEADER
        public byte bVersion;	//BLOBHEADER
        public short reserved;	//BLOBHEADER
        public uint aiKeyAlg;	//BLOBHEADER
        public uint magic;	//RSAPUBKEY
        public uint bitlen;	//RSAPUBKEY
        public uint pubexp;	//RSAPUBKEY
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct CERT_PUBLIC_KEY_INFO
    {
        public IntPtr SubjPKIAlgpszObjId;
        public int SubjPKIAlgParameterscbData;
        public IntPtr SubjPKIAlgParameterspbData;
        public int PublicKeycbData;
        public IntPtr PublicKeypbData;
        public int PublicKeycUnusedBits;
    }


    public class RSAPubKeyData
    {

        const uint X509_ASN_ENCODING = 0x00000001;
        const uint PKCS_7_ASN_ENCODING = 0x00010000;

        const uint RSA_CSP_PUBLICKEYBLOB = 19;
        const uint X509_PUBLIC_KEY_INFO = 8;

        const int AT_KEYEXCHANGE = 1;  //keyspec values
        const int AT_SIGNATURE = 2;
        static uint ENCODING_TYPE = PKCS_7_ASN_ENCODING | X509_ASN_ENCODING;

        const byte PUBLICKEYBLOB = 0x06;
        const byte CUR_BLOB_VERSION = 0x02;
        const ushort reserved = 0x0000;
        const uint CALG_RSA_KEYX = 0x0000a400;
        const uint CALG_RSA_SIGN = 0x00002400;


        private byte[] keyModulus;	// big-Endian 
        private byte[] keyExponent;	// big-Endian
        private byte[] publicKeyBlob;	//Microsoft PUBLICKEYBLOB format
        private uint keySize;		//modulus size in bits
        private bool verbose = false;


        public uint keysize
        {
            get { return keySize; }
        }

        public byte[] keymodulus
        {
            get { return keyModulus; }
        }

        public byte[] keyexponent
        {
            get { return keyExponent; }
        }

        public byte[] MSpublickeyblob
        {
            get { return publicKeyBlob; }
        }
        //----  RSAPublicKey,  PKCS #1 format  -----
        public bool DecodeRSAPublicKey(String RSAPublicKeyfile)
        {
            if (!File.Exists(RSAPublicKeyfile))
                return false;
            byte[] encodeddata = File.ReadAllBytes(RSAPublicKeyfile);
            return DecodeRSAPublicKey(encodeddata);
        }




        //----  SubjectPublicKeyInfo,  X.509 standard format; e.g. Java getEncoded(); OpenSSL exported etc.
        // ---  decode first to RSAPublicKey encoded format ----
        public bool DecodeSubjectPublicKeyInfo(String SubjectPublicKeyInfoFile)
        {
            if (!File.Exists(SubjectPublicKeyInfoFile))
                return false;
            byte[] subjectpublickeydata = File.ReadAllBytes(SubjectPublicKeyInfoFile);

            IntPtr pcertpublickeyinfo = IntPtr.Zero;
            uint cbytes = 0;
            if (Win32.CryptDecodeObject(ENCODING_TYPE, X509_PUBLIC_KEY_INFO, subjectpublickeydata, (uint)subjectpublickeydata.Length, 0, IntPtr.Zero, ref cbytes))
            {
                pcertpublickeyinfo = Marshal.AllocHGlobal((int)cbytes);
                Win32.CryptDecodeObject(ENCODING_TYPE, X509_PUBLIC_KEY_INFO, subjectpublickeydata, (uint)subjectpublickeydata.Length, 0, pcertpublickeyinfo, ref cbytes);
                CERT_PUBLIC_KEY_INFO pkinfo = (CERT_PUBLIC_KEY_INFO)Marshal.PtrToStructure(pcertpublickeyinfo, typeof(CERT_PUBLIC_KEY_INFO));
                IntPtr pencodeddata = pkinfo.PublicKeypbData;
                int cblob = pkinfo.PublicKeycbData;
                byte[] encodeddata = new byte[cblob];
                Marshal.Copy(pencodeddata, encodeddata, 0, cblob);  //copy bytes from IntPtr to byte[]
                Marshal.FreeHGlobal(pcertpublickeyinfo);
                return DecodeRSAPublicKey(encodeddata);
            }
            else
            {
                return false;
            }
        }

        public static RSAPubKeyData Blah(byte[] keydata)
        {
           

            RSAPubKeyData orsakey = new RSAPubKeyData();
            /*
            if (args.Length < 1)
            {
                RSAPubKeyData.usage();
                return;
            }
            */
            /*
            if (!File.Exists(keyfile))
            {
                Console.WriteLine("File '{0}' not found.", keyfile);
                return;
            }

            byte[] keydata = File.ReadAllBytes(keyfile);
            */
            
            Console.WriteLine("-------- Trying to decode keyfile as  PKCS #1 RSAPublicKey format --------");
            if (orsakey.DecodeRSAPublicKey(keydata))
                Console.WriteLine("Decoded successfully as PKCS #1 RSAPublicKey");
            else
                Console.WriteLine("FAILED to decode as PKCS #1 RSAPublicKey");
            
            
            Console.WriteLine("\n\n-------- Trying to decode keyfile as  X.509 SubjectPublicKeyInfo format --------");
            if (orsakey.DecodeSubjectPublicKeyInfo(keydata))
                Console.WriteLine("Decoded successfully as X.509 SubjectPublicKeyInfo");
            else
                Console.WriteLine("FAILED to decode as X.509 SubjectPublicKeyInfo");
            

            Console.WriteLine("\n\n--------- Trying to decode keyfile as  Microsoft PUBLICKEYBLOB  --------");
            if (orsakey.DecodeMSPublicKeyBlob(keydata))
                Console.WriteLine("Decoded successfully as Microsoft PUBLICKEYBLOB");
            else
                Console.WriteLine("FAILED to decode as Microsoft PUBLICKEYBLOB");
            
            return orsakey;

        }


        public bool DecodeSubjectPublicKeyInfo(byte[] subjectpublickeydata)
        {
           
            

            IntPtr pcertpublickeyinfo = IntPtr.Zero;
            uint cbytes = 0;
            if (Win32.CryptDecodeObject(ENCODING_TYPE, X509_PUBLIC_KEY_INFO, subjectpublickeydata, (uint)subjectpublickeydata.Length, 0, IntPtr.Zero, ref cbytes))
            {
                pcertpublickeyinfo = Marshal.AllocHGlobal((int)cbytes);
                Win32.CryptDecodeObject(ENCODING_TYPE, X509_PUBLIC_KEY_INFO, subjectpublickeydata, (uint)subjectpublickeydata.Length, 0, pcertpublickeyinfo, ref cbytes);
                CERT_PUBLIC_KEY_INFO pkinfo = (CERT_PUBLIC_KEY_INFO)Marshal.PtrToStructure(pcertpublickeyinfo, typeof(CERT_PUBLIC_KEY_INFO));
                IntPtr pencodeddata = pkinfo.PublicKeypbData;
                int cblob = pkinfo.PublicKeycbData;
                byte[] encodeddata = new byte[cblob];
                Marshal.Copy(pencodeddata, encodeddata, 0, cblob);  //copy bytes from IntPtr to byte[]
                Marshal.FreeHGlobal(pcertpublickeyinfo);
                return DecodeRSAPublicKey(encodeddata);
            }
            else
            {
                return false;
            }
        }



        //----- decode public key and extract modulus and exponent from RSAPublicKey,  PKCS #1 format byte[] ----
        public bool DecodeRSAPublicKey(byte[] encodedpubkey)
        {
            byte[] publickeyblob;

            uint blobbytes = 0;
            if (Win32.CryptDecodeObject(ENCODING_TYPE, RSA_CSP_PUBLICKEYBLOB, encodedpubkey, (uint)encodedpubkey.Length, 0, null, ref blobbytes))
            {
                publickeyblob = new byte[blobbytes];
                if (Win32.CryptDecodeObject(ENCODING_TYPE, RSA_CSP_PUBLICKEYBLOB, encodedpubkey, (uint)encodedpubkey.Length, 0, publickeyblob, ref blobbytes))
                    if (verbose)
                        showBytes("CryptoAPI publickeyblob", publickeyblob);
            }
            else
            {
                return false;
            }
            this.publicKeyBlob = publickeyblob;
            return DecodeMSPublicKeyBlob(publickeyblob);
        }


        /*
        //----  Microsoft PUBLICKEYBLOB format  -----
        public bool DecodeMSPublicKeyBlob(String publickeyblobfile)
        {
            if (!File.Exists(publickeyblobfile))
                return false;
            byte[] publickeyblobdata = File.ReadAllBytes(publickeyblobfile);
            return DecodeMSPublicKeyBlob(publickeyblobdata);
        }


        */
        //-----  Microsoft PUBLICKEYBLOB format  ----
        public bool DecodeMSPublicKeyBlob(byte[] publickeyblob)
        {
            PUBKEYBLOBHEADERS pkheaders = new PUBKEYBLOBHEADERS();
            int headerslength = Marshal.SizeOf(pkheaders);
            IntPtr buffer = Marshal.AllocHGlobal(headerslength);
            Marshal.Copy(publickeyblob, 0, buffer, headerslength);
            pkheaders = (PUBKEYBLOBHEADERS)Marshal.PtrToStructure(buffer, typeof(PUBKEYBLOBHEADERS));
            Marshal.FreeHGlobal(buffer);

            //-----  basic sanity check of PUBLICKEYBLOB fields ------------
            if (pkheaders.bType != PUBLICKEYBLOB)
                return false;
            if (pkheaders.bVersion != CUR_BLOB_VERSION)
                return false;
            if (pkheaders.aiKeyAlg != CALG_RSA_KEYX && pkheaders.aiKeyAlg != CALG_RSA_SIGN)
                return false;

            if (verbose)
            {
                Console.WriteLine("\n ---- PUBLICKEYBLOB headers ------");
                Console.WriteLine("  btype     {0}", pkheaders.bType);
                Console.WriteLine("  bversion  {0}", pkheaders.bVersion);
                Console.WriteLine("  reserved  {0}", pkheaders.reserved);
                Console.WriteLine("  aiKeyAlg  0x{0:x8}", pkheaders.aiKeyAlg);
                String magicstring = (new ASCIIEncoding()).GetString(BitConverter.GetBytes(pkheaders.magic));
                Console.WriteLine("  magic     0x{0:x8}     '{1}'", pkheaders.magic, magicstring);
                Console.WriteLine("  bitlen    {0}", pkheaders.bitlen);
                Console.WriteLine("  pubexp    {0}", pkheaders.pubexp);
                Console.WriteLine(" --------------------------------");
            }
            //-----  Get public key size in bits -------------
            this.keySize = pkheaders.bitlen;

            //-----  Get public exponent -------------
            byte[] exponent = BitConverter.GetBytes(pkheaders.pubexp); //little-endian ordered
            Array.Reverse(exponent);    //convert to big-endian order
            this.keyExponent = exponent;
            if (verbose)
                showBytes("\nPublic key exponent (big-endian order):", exponent);

            //-----  Get modulus  -------------
            int modulusbytes = (int)pkheaders.bitlen / 8;
            byte[] modulus = new byte[modulusbytes];
            try
            {
                Array.Copy(publickeyblob, headerslength, modulus, 0, modulusbytes);
                Array.Reverse(modulus);   //convert from little to big-endian ordering.
                this.keyModulus = modulus;
                if (verbose)
                    showBytes("\nPublic key modulus  (big-endian order):", modulus);
            }
            catch (Exception)
            {
                Console.WriteLine("Problem getting modulus from publickeyblob");
                return false;
            }
            return true;
        }
        private static void showBytes(String info, byte[] data)
        {
            Console.WriteLine("{0}  [{1} bytes]", info, data.Length);
            for (int i = 1; i <= data.Length; i++)
            {
                Console.Write("{0:X2}  ", data[i - 1]);
                if (i % 16 == 0)
                    Console.WriteLine();
            }
            Console.WriteLine();
        }
    }
}
