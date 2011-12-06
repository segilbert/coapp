//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Toolkit.AzureFS {
    using System.IO;
    using Extensions;

    public class CachingFileSystem {
        private static readonly string StorageCachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AzureStorageCache");

        private static string GenerateStoragePath(string name) {
            return Path.Combine(StorageCachePath, name);
        }

        private static string GenerateStoragePath(string one, string two) {
            return GenerateStoragePath((one + two).MD5Hash());
        }

        private string name;
        private string password;
        private string cachePath;

        public CachingFileSystem(string storageName, string storagePassword, string localCachePath ) {
            name = storageName;
            password = storagePassword;
            cachePath = localCachePath;
            if( !Directory.Exists(cachePath)) {
                Directory.CreateDirectory(cachePath);
            }
        }

        public CachingFileSystem(string storageName, string storagePassword): this(storageName, storagePassword, GenerateStoragePath(storageName, storagePassword)) { 
        }

        public void ClearCache() {
            
        }
    }
}
