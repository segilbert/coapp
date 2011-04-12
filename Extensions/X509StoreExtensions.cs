

namespace CoApp.Toolkit.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Cryptography;
   
    public static class X509StoreExtensions
    {
        public const string CODE_SIGNING_OID = "1.3.6.1.5.5.7.3.3";
        
        public static X509Certificate2 FindCodeSigningCert(this X509Store pfx)
        {

            return (from c in pfx.Certificates.Cast<X509Certificate2>()
                    where c.HasPrivateKey
                    from ext in c.Extensions.Cast<X509Extension>().OfType<X509EnhancedKeyUsageExtension>()
                    from oid in ext.EnhancedKeyUsages.Cast<Oid>()
                    where oid.Value == CODE_SIGNING_OID
                    select c).FirstOrDefault();


        }
    }
}
