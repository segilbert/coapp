//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Network {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;

    public class TransferManager {
        protected static Dictionary<string, TransferManager> _managedFolders = new Dictionary<string, TransferManager>();

        protected readonly string _folder;
        protected readonly Dictionary<string, RemoteFile> _items = new Dictionary<string, RemoteFile>();
        private CancellationToken _cancellationToken;

        public CancellationToken CancellationToken {
            get { return _cancellationToken; }
            set {
                _cancellationToken = value;
                foreach (var remotefile in _items.Values) {
                    remotefile.CancellationToken = value;
                }
            }
        }

        public RemoteFile this[string uri] {
            get {
                return !_items.ContainsKey(uri) ? this[new Uri(uri)] : _items[uri];
            }
        }

        public RemoteFile this[Uri uri] {
            get {
                if (!_items.ContainsKey(uri.AbsoluteUri)) {
                    
                    _items.Add(uri.AbsoluteUri, new RemoteFile(uri, Path.Combine( _folder, uri.DnsSafeHost), CancellationToken));
                }
                return _items[uri.AbsoluteUri];
            }
        }

        protected TransferManager(string folder) {
            _folder = folder;
        }

        public static TransferManager GetTransferManager(string folder) {
            folder = Path.GetFullPath(folder).ToLower();
            if (! Directory.Exists(folder)) {
                Directory.CreateDirectory(folder);
            }

            if (!_managedFolders.ContainsKey(folder)) {
                _managedFolders.Add(folder, new TransferManager(folder));
            }

            return _managedFolders[folder];
        }
    }
}