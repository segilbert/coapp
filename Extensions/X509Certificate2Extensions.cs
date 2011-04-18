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
