
namespace CoApp.Toolkit.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Security.Cryptography.X509Certificates;
    public static class X509Certificate2Extensions
    {
        public static Dictionary<string, string> GetSubjectNameParts(this X509Certificate2 cert)
        {
            var subName = cert.SubjectName.Name;
            var list = subName.SplitToList(',').ToDictionary(i => i.Split('=')[0], i => i.Split('=')[1]);
                   
            return list;
        }
    }
}
