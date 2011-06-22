//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Eric Schultz. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace CoApp.Toolkit.Extensions
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using CoApp.Toolkit.Crypto;
    using System.Security.Cryptography;
    public static class X509Certificate2Extensions
    {
        public static Dictionary<string, string> GetSubjectNameParts(this X509Certificate2 cert)
        {
            var result = new Dictionary<string, string>();

            foreach (var bits in cert.SubjectName.Name.SplitToList(',').Select(each => each.Split('='))) {
                var newKey= bits[0].Trim(' ');
                result.Add(result.ContainsKey(newKey) ? newKey + result.Keys.Where(key => key.StartsWith(newKey)).Count() : newKey, bits[1]);
            }

            return result;
        }

        public static byte[] GetPublicKey(this X509Certificate2 cert)
        {
            var sn = new StrongNameCertificate(cert.PublicKey.Key as RSA);
            return sn.PublicKey;
        }

        public static byte[] GetPublicKeyToken(this X509Certificate2 cert)
        {
            var sn = new StrongNameCertificate(cert.PublicKey.Key as RSA);
            return sn.PublicKeyToken;
        }

        public static string GetPktAsString(this X509Certificate2 cert)
        {
            return cert.GetPublicKeyToken().ToHexString();
        }

        public static bool FinishStrongNaming(this X509Certificate2 cert, string fileToDelaySign)
        {
            var sn = new StrongNameCertificate(cert.PrivateKey as RSA);
            return sn.Sign(fileToDelaySign);
        }
    }
}
