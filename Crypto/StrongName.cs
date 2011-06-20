//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2003 Motus Technologies Inc. (http://www.motus.com)
//     Copyright (c) 2004 Novell Inc. (http://www.novell.com)
//     Copyright (c) 2011 Eric Schultz
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Crypto
{
    using System;
    using System.Globalization;
    using System.Security.Cryptography;
    
    /// <summary>
    /// Originally from Mono project in Mono.Security assembly as Mono.Security.StrongName
    /// 
    /// TODO: rename?
    /// </summary>
    internal sealed class StrongName
    {

        internal class StrongNameSignature
        {
            private byte[] hash;
            private byte[] signature;
            private UInt32 signaturePosition;
            private UInt32 signatureLength;
            private UInt32 metadataPosition;
            private UInt32 metadataLength;
            private byte cliFlag;
            private UInt32 cliFlagPosition;

            public byte[] Hash
            {
                get { return hash; }
                set { hash = value; }
            }

            public byte[] Signature
            {
                get { return signature; }
                set { signature = value; }
            }

            public UInt32 MetadataPosition
            {
                get { return metadataPosition; }
                set { metadataPosition = value; }
            }

            public UInt32 MetadataLength
            {
                get { return metadataLength; }
                set { metadataLength = value; }
            }

            public UInt32 SignaturePosition
            {
                get { return signaturePosition; }
                set { signaturePosition = value; }
            }

            public UInt32 SignatureLength
            {
                get { return signatureLength; }
                set { signatureLength = value; }
            }

            // delay signed -> flag = 0x01
            // strongsigned -> flag = 0x09
            public byte CliFlag
            {
                get { return cliFlag; }
                set { cliFlag = value; }
            }

            public UInt32 CliFlagPosition
            {
                get { return cliFlagPosition; }
                set { cliFlagPosition = value; }
            }
        }

        internal enum StrongNameOptions
        {
            Metadata,
            Signature
        }

        private RSA rsa;
        private byte[] publicKey;
        private byte[] keyToken;
        private string tokenAlgorithm;

        public StrongName(RSA rsa)
        {
            if (rsa == null)
                throw new ArgumentNullException("rsa");

            RSA = rsa;
        }

        private void InvalidateCache()
        {
            publicKey = null;
            keyToken = null;
        }

        public bool CanSign
        {
            get
            {
                if (rsa == null)
                    return false;

                else
                {
                    // the hard way
                    try
                    {
                        RSAParameters p = rsa.ExportParameters(true);
                        return ((p.D != null) && (p.P != null) && (p.Q != null));
                    }
                    catch (CryptographicException)
                    {
                        return false;
                    }
                }
            }
        }

        public RSA RSA
        {
            get
            {
                // if none then we create a new keypair
                if (rsa == null)
                    rsa = (RSA)RSA.Create();
                return rsa;
            }
            set
            {
                rsa = value;
                InvalidateCache();
            }
        }

        public byte[] PublicKey
        {
            get
            {
                if (publicKey == null)
                {
                    byte[] keyPair = CryptoConvert.ToCapiKeyBlob(rsa, false);
                    // since 2.0 public keys can vary from 384 to 16384 bits
                    publicKey = new byte[32 + (rsa.KeySize >> 3)];

                    // The first 12 bytes are documented at:
                    // http://msdn.microsoft.com/library/en-us/cprefadd/html/grfungethashfromfile.asp
                    // ALG_ID - Signature
                    publicKey[0] = keyPair[4];
                    publicKey[1] = keyPair[5];
                    publicKey[2] = keyPair[6];
                    publicKey[3] = keyPair[7];
                    // ALG_ID - Hash (SHA1 == 0x8004)
                    publicKey[4] = 0x04;
                    publicKey[5] = 0x80;
                    publicKey[6] = 0x00;
                    publicKey[7] = 0x00;
                    // Length of Public Key (in bytes)
                    // TODO If Win on ARM is big-endian, this will not work and we'll need BitConverterLE from Mono.Security
                    byte[] lastPart = BitConverter.GetBytes(publicKey.Length - 12);
                    publicKey[8] = lastPart[0];
                    publicKey[9] = lastPart[1];
                    publicKey[10] = lastPart[2];
                    publicKey[11] = lastPart[3];
                    // Ok from here - Same structure as keypair - expect for public key
                    publicKey[12] = 0x06;		// PUBLICKEYBLOB
                    // we can copy this part
                    Buffer.BlockCopy(keyPair, 1, publicKey, 13, publicKey.Length - 13);
                    // and make a small adjustment 
                    publicKey[23] = 0x31;		// (RSA1 not RSA2)
                }
                return (byte[])publicKey.Clone();
            }
        }

        public byte[] PublicKeyToken
        {
            get
            {
                if (keyToken == null)
                {
                    byte[] publicKey = PublicKey;
                    if (publicKey == null)
                        return null;
                    HashAlgorithm ha = HashAlgorithm.Create(TokenAlgorithm);
                    byte[] hash = ha.ComputeHash(publicKey);
                    // we need the last 8 bytes in reverse order
                    keyToken = new byte[8];
                    Buffer.BlockCopy(hash, (hash.Length - 8), keyToken, 0, 8);
                    Array.Reverse(keyToken, 0, 8);
                }
                return (byte[])keyToken.Clone();
            }
        }

        public string TokenAlgorithm
        {
            get
            {
                if (tokenAlgorithm == null)
                    tokenAlgorithm = "SHA1";
                return tokenAlgorithm;
            }
            set
            {
                string algo = value.ToUpper(CultureInfo.InvariantCulture);
                if ((algo == "SHA1") || (algo == "MD5"))
                {
                    tokenAlgorithm = value;
                    InvalidateCache();
                }
                else
                    throw new ArgumentException("Unsupported hash algorithm for token");
            }
        }

        public byte[] GetBytes()
        {
            return CryptoConvert.ToCapiPrivateKeyBlob(RSA);
        }
    }
}
