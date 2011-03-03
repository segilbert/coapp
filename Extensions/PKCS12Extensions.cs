

namespace CoApp.Toolkit.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Mono.Security.X509;
    using Mono.Security.X509.Extensions;
    using ssc = System.Security.Cryptography;
    using sscx = System.Security.Cryptography.X509Certificates;
   
    public static class PKCS12Extensions
    {
        public const string CODE_SIGNING_OID = "1.3.6.1.5.5.7.3.3";
        public static X509Certificate FindCodeSigningCert(this PKCS12 pfx)
        {

            return (from msCert in
                        (from X509Certificate c in pfx.Certificates
                         select new sscx.X509Certificate2(c.RawData))
                    from ext in msCert.Extensions.Cast<sscx.X509Extension>().OfType<sscx.X509EnhancedKeyUsageExtension>()
                    from oid in ext.EnhancedKeyUsages.Cast<ssc.Oid>()
                    where oid.Value == CODE_SIGNING_OID
                    select new X509Certificate(msCert.RawData)).FirstOrDefault();
        }
    }
}
