//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace CoApp.Toolkit.Network {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    public class CachingNetworkClient {
        private const int FILE_CACHE_VERSION = 1;

        protected readonly string CachePath;
        private readonly string _cacheFile;

        protected Dictionary<Uri, CachedFileInfo> _cache = new Dictionary<Uri, CachedFileInfo>();

        public enum ActionState {
            None,
            InProgress,
            Completed,
            Failed
        }

        public class CachedFileInfo {
            // Persistable Info
            public Uri RemoteLocation { get; internal set; }
            public Uri ActualRemoteLocation { get; internal set; }

            public DateTime LastModified { get; internal set; }
            public long ContentLength { get; internal set; }
            public string LocalFullPath { get; internal set; }

            // Transient Info
            public HttpStatusCode StatusCode { get; internal set; }
            public bool HasLocalFile { get {
                return !string.IsNullOrEmpty(LocalFullPath) && File.Exists(LocalFullPath);
            } }

            public bool HasLocalFileYoungerThan(TimeSpan timeSpan) {
                return HasLocalFile && File.GetLastWriteTime(LocalFullPath).Add(timeSpan).CompareTo(DateTime.Now) > 0;
            }

            public ActionState DownloadState { get; internal set; }
            public ActionState PreviewState { get; internal set; }
            public int DownloadProgress { get; internal set; }
            
            internal void GenerateLocalPath(string cachePath, string pathHint) {
                LocalFullPath = Path.Combine(cachePath, pathHint);
            }

            internal void GenerateLocalPath(string cachePath, Uri pathHint) {
                var fname = pathHint.LocalPath.Substring(pathHint.LocalPath.LastIndexOf('/') + 1);
                if( string.IsNullOrEmpty(fname )) {
                    fname = new Regex(".*://").Replace(pathHint.AbsoluteUri, "");
                    fname = new Regex("%..").Replace(fname, "#").Replace('/', '-')+"$DEFAULT";
                }
                LocalFullPath = Path.Combine(cachePath, fname);
            }
        }

        public CachingNetworkClient(string cachePath) {
            CachePath = cachePath;
            if (!Directory.Exists(CachePath)) {
                Directory.CreateDirectory(CachePath);
            } else {
                _cacheFile = Path.Combine(CachePath, "network.cache");
                LoadCache();
            }
        }

        protected void LoadCache() {
            if (File.Exists(_cacheFile)) {
                var buffer = File.ReadAllBytes(_cacheFile);
                using (var ms = new MemoryStream(buffer)) {
                    var binaryReader = new BinaryReader(ms);
                    
                    var version = binaryReader.ReadInt32();
                    var count = binaryReader.ReadInt32();

                    for(var i=0;i<count;i++) {
                        try {
                            
                            var remoteLocation = binaryReader.ReadString();
                            var actualRemoteLocation = binaryReader.ReadString();
                            var ticks = binaryReader.ReadInt64();
                            var length = binaryReader.ReadInt64();
                            var localFullPath = binaryReader.ReadString();
                            
                            var item = new CachedFileInfo {
                                RemoteLocation = new Uri(remoteLocation),
                                ActualRemoteLocation = string.IsNullOrEmpty(actualRemoteLocation) ? null : new Uri(actualRemoteLocation),
                                LastModified = new DateTime(ticks),
                                ContentLength = length,
                                LocalFullPath = localFullPath
                            };
                            _cache.Add(item.RemoteLocation, item);
                        } catch {
                            // skip any broken ones...
                        }
                    }
                }
            }
        }

        protected void SaveCache() {
            try {
                using (var ms = new MemoryStream()) {
                    var binaryWriter = new BinaryWriter(ms);

                    // order of the following is very important.
                    binaryWriter.Write(FILE_CACHE_VERSION);
                    binaryWriter.Write(_cache.Count);

                    foreach (var val in _cache.Values.ToList()) {
                        binaryWriter.Write(val.RemoteLocation != null ? val.RemoteLocation.AbsoluteUri : "");
                        binaryWriter.Write(val.ActualRemoteLocation != null ? val.ActualRemoteLocation.AbsoluteUri : "");
                        binaryWriter.Write(val.LastModified.Ticks);
                        binaryWriter.Write(val.ContentLength);
                        binaryWriter.Write(val.LocalFullPath ?? "");
                    }
                    File.WriteAllBytes(_cacheFile, ms.GetBuffer());
                }
            } catch {
                // if it breaks, don't worry.
            }
        }

        public IEnumerable<CachedFileInfo> FilesInProgress {get {
            return from file in _cache.Values where
                file.PreviewState == ActionState.InProgress || file.DownloadState == ActionState.InProgress select file;
        }}

        public CachedFileInfo GetFileInfo(Uri remotePath) {
            if (!_cache.ContainsKey(remotePath)) {
                _cache.Add(remotePath, new CachedFileInfo() { RemoteLocation = remotePath });
            }
            return _cache[remotePath];
        }

        public void DownloadFile(Uri remotePath, TimeSpan acceptableCacheTime, Action<CachedFileInfo> operationCompleted, Action<int> downloadProgress) {
            if (!_cache.ContainsKey(remotePath)) {
                _cache.Add(remotePath, new CachedFileInfo() { RemoteLocation = remotePath });
            }
            var cachedFileInfo = _cache[remotePath];
            if( cachedFileInfo.HasLocalFileYoungerThan(acceptableCacheTime) ) {
                operationCompleted(cachedFileInfo);
                return;
            }

            DownloadFile(remotePath,operationCompleted, downloadProgress);
        }

        public void DownloadFile(Uri remotePath, Action<CachedFileInfo> operationCompleted, Action<int> downloadProgress) {
            if (!_cache.ContainsKey(remotePath)) {
                _cache.Add(remotePath, new CachedFileInfo() { RemoteLocation = remotePath });
            }
            var cachedFileInfo = _cache[remotePath];

            Action<CachedFileInfo> afterPreview = cfi => {
                if( cfi.PreviewState == ActionState.Failed ) {
                    cfi.DownloadState = ActionState.Failed;
                    operationCompleted(cachedFileInfo = _cache[remotePath]);
                    return;
                }

                if( File.Exists(cfi.LocalFullPath) ) {
                    var fi = new FileInfo(cfi.LocalFullPath);

                    if( File.GetCreationTime(cfi.LocalFullPath).Ticks == cfi.LastModified.Ticks && fi.Length == cfi.ContentLength ) {
                        // file is the same as the one at the server, don't reget.
                        operationCompleted(cachedFileInfo);
                        return;
                    }
                    // otherwise, just remove it.
                    fi.Delete();
                }
                DownloadFileImpl(remotePath, operationCompleted, downloadProgress);
            };

            if( cachedFileInfo.PreviewState == ActionState.Completed ) {
                afterPreview.Invoke(cachedFileInfo);
            } else {
                // lets preview first.
                PreviewFile(remotePath, afterPreview);
            }
        }

        public void PreviewFile(Uri remotePath, Action<CachedFileInfo> operationCompleted) {
            if(!_cache.ContainsKey(remotePath)) {
                _cache.Add(remotePath, new CachedFileInfo() {RemoteLocation=remotePath} );
            }
            var cachedFileInfo = _cache[remotePath];

            // if it's already in progress, let's piggyback.
            if (cachedFileInfo.PreviewState == ActionState.InProgress ) {
                Task.Factory.StartNew(() => {
                    while (cachedFileInfo.PreviewState == ActionState.InProgress ) {
                        Thread.Sleep(50);
                    }
                    operationCompleted(cachedFileInfo);
                });
                return;
            }
         
            PreviewFileImpl(remotePath, operationCompleted);
        }

        protected virtual void PreviewFileImpl(Uri remotePath, Action<CachedFileInfo> operationCompleted) {
            throw new NotImplementedException();
        }

        protected virtual void DownloadFileImpl(Uri remotePath, Action<CachedFileInfo> operationCompleted, Action<int> downloadProgress) {
            throw new NotImplementedException();
        }
    }
}