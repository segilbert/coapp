//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine {
    using System.Collections.Generic;
    using System.Text;
    using System;

    public class PackageManager {
        public bool Pretend {get;set;}

        public PackageManager() {
        }

        public void Install(List<string> packages) {

        }

        public void Remove(List<string> packages) {

        }

        public string RepostoryDirectoryUrl {
            get {
                var stringBuilder = new StringBuilder(1024);
                NativeMethods.coapp_GetRepostitoryDirectoryURL(1024, stringBuilder);
                return stringBuilder.ToString();
            }
            set {
                var uri = new Uri(value);
                if(uri.IsFile || uri.IsUnc || uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                    NativeMethods.coapp_SetRepositoryDirectoryURL(value.Length, value);
                else
                    throw new Exception("Repository Directory URL must be file, https, or http URI");
            }
        }
    }
}
