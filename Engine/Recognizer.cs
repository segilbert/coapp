//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;
    using Extensions;
    using Feeds.Atom;
    using Network;
    using PackageFormatHandlers;
    using Tasks;

    internal class Recognizer {
        private static readonly TransferManager _transferManager =
            TransferManager.GetTransferManager(PackageManagerSettings.CoAppCacheDirectory);

        private static readonly Dictionary<string, RecognitionInfo> _cache = new Dictionary<string, RecognitionInfo>();

        internal static RecognitionInfo Recognize(string item, string baseDirectory = null, string baseUrl = null, bool ensureLocal = false) {
            if (_cache.ContainsKey(item)) {
                return _cache[item];
            }

            baseDirectory = baseDirectory ?? Environment.CurrentDirectory;

            var result = new RecognitionInfo();

            if (item.StartsWith("pkg:", StringComparison.CurrentCultureIgnoreCase)) {
                // this is a coapp package reference
                result.IsReference = true;
                result.IsCoAppMSI = true;
            }

            if (item.StartsWith("nuget:", StringComparison.CurrentCultureIgnoreCase)) {
                // this is a nuget package reference
                result.IsReference = true;
                result.IsNugetPackage = true;
            }

            if (item.StartsWith("msi:", StringComparison.CurrentCultureIgnoreCase)) {
                // this is a msi package reference
                result.IsReference = true;
                result.IsLegacyMSI = true;
            }

            if (item.StartsWith("openwrap:", StringComparison.CurrentCultureIgnoreCase)) {
                // this is a msi package reference
                result.IsReference = true;
                result.IsOpenwrapPackage = true;
            }

            

            // Is this an URL of some kind?
            if (result.IsUnknown) {
                try {
                    if (item.IndexOf("://") > -1) {
                        // some sort of URL
                        result.IsURL = true;
                        result.FullUrl = new Uri(item);
                    }
                    else if (!string.IsNullOrEmpty(baseUrl)) {
                        // it's a relative URL?
                        result.IsURL = true;
                        result.FullUrl = new Uri(new Uri(baseUrl), item);
                    }

                    // for now, we've got a full URL and it looks like it point somewhere.
                    // until we attempt to retrieve the URL we really can't bank on knowing what it is.
                    if (result.IsURL) {
                        result.RemoteFile.Preview().ContinueWith(antecedent => {
                            if (result.RemoteFile.LastStatus != HttpStatusCode.OK) {
                                result.IsInvalid = true;
                                result.Recognized.Value = true;
                                return;
                            }

                            if (result.RemoteFile.IsRedirect) {
                                result.RemoteFile.Folder = _transferManager[result.RemoteFile.ActualRemoteLocation].Folder;
                            }

                            result.FullPath = result.RemoteFile.LocalFullPath;

                            if (ensureLocal) {
                                if (!result.IsLocal) {
                                    result.RemoteFile.Get().Wait();
                                }

                                if (!result.IsLocal) {
                                    result.IsInvalid = true;
                                    result.Recognized.Value = true;
                                    return;
                                }
                            }

                            if (result.IsLocal) {
                                var localFileInfo = Recognize(result.FullPath);
                                // at this point, we should actually know the real file results.
                                result.CopyDetailsFrom(localFileInfo);
                                result.Recognized.Value = true;
                                return;
                            }

                            // not local. all we can do is guess?
                            // TODO: Implement remote guess?
                        }, TaskContinuationOptions.AttachedToParent);
                    }
                }
                catch
                {
                }
            }

            if (result.IsUnknown) {
                if (item.IndexOf('?') > -1 || item.IndexOf('*') > -1) {
                    // this has a wildcard. Past that, we don't know what it is yet.
                    // assuming this is a local path expression:
                    //     c:\foo\*.msi
                    //     App*.msi          (matching in the current dir)
                    //     packages\*
                    var folder = baseDirectory;
                    var lastSlash = item.LastIndexOf("\\");
                    if (lastSlash > -1) {
                        folder = Path.GetFullPath(item.Substring(0, lastSlash));
                    }

                    if (folder.IndexOf('?') == -1 && folder.IndexOf('*') == -1) {
                        if (Directory.Exists(folder)) {
                            result.IsFolder = true;
                            result.FullPath = Path.GetFullPath(folder).ToLower();
                            result.Wildcard = item.Substring(lastSlash + 1);
                        }
                    }
                }
            }

            // Is this a directory?
            if (result.IsUnknown) {
                try {
                    if (Directory.Exists(item)) {
                        // a valid directory
                        result.IsFolder = true;
                        result.FullPath = Path.GetFullPath(item).ToLower();
                    }
                    else if (!string.IsNullOrEmpty(baseDirectory)) {
                        var path = Path.Combine(baseDirectory, item).ToLower();
                        if (Directory.Exists(path)) {
                            result.IsFolder = true;
                            result.FullPath = path;
                        }
                    }

                    if (result.IsFolder) {
                        result.IsPackageFeed = true;
                    }
                }
                catch {
                }
            }

            // maybe it's a file?
            if (result.IsUnknown) {
                try {
                    if (File.Exists(item)) {
                        // some type of file
                        result.IsFile = true;
                        result.FullPath = Path.GetFullPath(item).ToLower();
                    }
                    else if (!string.IsNullOrEmpty(baseDirectory)) {
                        var path = Path.Combine(baseDirectory, item);
                        if (File.Exists(path)) {
                            result.IsFile = true;
                            result.FullPath = Path.GetFullPath(path).ToLower();
                        }

                    }

                    if (result.IsFile) {
                        // so, this is a file. 
                        // let's do a little bit of diggin'

                        var ext = Path.GetExtension(result.FullPath);

                        switch (ext) {
                            case ".msi":
                                result.IsCoAppMSI = CoAppMSI.IsCoAppPackageFile(result.FullPath);
                                result.IsLegacyMSI = !result.IsCoAppMSI;
                                result.IsPackageFile = true;
                                break;

                            case ".nupkg":
                                result.IsNugetPackage = true;
                                result.IsPackageFile = true;
                                break;

                            case ".exe":
                                result.IsLegacyEXE = true;
                                result.IsPackageFile = true;
                                break;

                            case ".zip":
                            case ".cab":
                            case ".rar":
                            case ".7z":
                                result.IsArchive = true;
                                result.IsPackageFeed = true;
                                break;

                            default:
                                if (result.FullPath.IsXmlFile()) {
                                    try {
                                        var feed = AtomFeed.Load(result.FullPath);
                                        if (feed != null) {
                                            result.IsPackageFeed = true;
                                            result.IsAtom = true;
                                        }
                                    }
                                    catch {
                                    }
                                }
                                break;
                        }
                    }
                }
                catch {
                }
            }
            if (!result.IsUnknown) {
                result.Recognized.Value = true;
            }

            _cache.Add(item, result);
            return result;
        }

        #region Nested type: RecognitionInfo

        internal class RecognitionInfo {
            public TriggeredProperty<bool> Recognized = new TriggeredProperty<bool>(false, value => value);
            private Uri _fullUrl;
            internal bool IsWildcard { get { return Wildcard != null; } }
            internal string Wildcard { get; set; }
            internal string FullPath { get; set; }

            internal Uri FullUrl {
                get { return _fullUrl; }
                set {
                    if (value != null) {
                        RemoteFile = _transferManager[value];
                    }
                    _fullUrl = value;
                }
            }

            internal RemoteFile RemoteFile { get; set; }

            internal bool IsUnknown {
                get { return !(IsPackageFeed | IsPackageFile); }
            }

            internal bool IsInvalid { get; set; }

            internal bool IsPackageFile { get; set; }
            internal bool IsPackageFeed { get; set; }

            internal bool IsURL { get; set; }
            internal bool IsFile { get; set; }
            internal bool IsFolder { get; set; }
            internal bool IsReference { get; set; }

            internal bool IsMSI {
                get { return IsCoAppMSI | IsLegacyMSI; }
            }

            internal bool IsCoAppMSI { get; set; }
            internal bool IsLegacyMSI { get; set; }
            internal bool IsLegacyEXE { get; set; }

            internal bool IsNugetPackage { get; set; }
            internal bool IsOpenwrapPackage { get; set; }

            internal bool IsArchive { get; set; }
            internal bool IsAtom { get; set; }

            internal bool IsCoAppODataService { get; set; }
            internal bool IsNugetODataService { get; set; }

            internal bool IsLocal {
                get {
                    if (RemoteFile != null) {
                        return RemoteFile.IsLocal;
                    }

                    if (!string.IsNullOrEmpty(FullPath)) {
                        if (File.Exists(FullPath)) {
                            return true;
                        }
                    }

                    return false;
                }
            }

            internal void CopyDetailsFrom(RecognitionInfo fileInfo) {
                FullPath = fileInfo.FullPath;
                IsInvalid = fileInfo.IsInvalid;
                IsPackageFile = fileInfo.IsPackageFile;
                IsPackageFeed = fileInfo.IsPackageFeed;
                IsCoAppMSI = fileInfo.IsCoAppMSI;
                IsLegacyMSI = fileInfo.IsLegacyMSI;
                IsLegacyEXE = fileInfo.IsLegacyEXE;
                IsNugetPackage = fileInfo.IsNugetPackage;
                IsOpenwrapPackage = fileInfo.IsOpenwrapPackage;
                IsArchive = fileInfo.IsArchive;
                IsAtom = fileInfo.IsAtom;
            }
        }

        #endregion
    }
}