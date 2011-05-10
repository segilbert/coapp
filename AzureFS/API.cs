//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.AzureFS {
    public class Api {
        [DllExport("Connect")]
        public static int Connect(string name, string password) {
            return (name + password).GetHashCode();
        }
    }
}