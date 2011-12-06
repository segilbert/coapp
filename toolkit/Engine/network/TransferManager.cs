//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine.Network {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;

    public class TransferManager {
        public CancellationToken CancellationToken { get; set; } // GS01 compare to toolkit version

        public RemoteFile this[string uri] {
            get {
                throw new NotImplementedException();
            }
        }

        public RemoteFile this[Uri uri] {
            get {
                throw new NotImplementedException();
            }
        }

        protected TransferManager(string folder) {
            throw new NotImplementedException();
        }

        public static TransferManager GetTransferManager(string folder) {
            return null;
            //            throw new NotImplementedException();
        }
    }
}