//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Extensions;
    using Feeds.Atom;
    using Network;
    using PackageFormatHandlers;

    [Flags]
    internal enum ItemType {
        Unknown = 0x00000000,

        // Resource type 
        File = 0x00000001,
        Folder = 0x00000002,
        URL = 0x00000004,
        Reference = 0x00000008,

        // Packages
        PackageFile = 0x10000000,
        MSI = 0x00000010 & PackageFile,

        CoAppMSI = 0x00000020 & MSI,
        LegacyMSI = 0x00000040 & MSI,

        LegacyEXE = 0x00000080 & PackageFile,
        NuGetPackage = 0x00000100 & PackageFile,
        OpenWrapPackage = 0x00000200 & PackageFile,

        // Package collections
        // .ZIP, .CAB, etc... (file)
        Feed = 0x20000000,
        Archive = 0x00010000 & Feed,
        // can be a file or a URL
        AtomFeed = 0x00020000 & Feed, 

        // Directory           = 0x00040000,       // folder on disk

        CoAppODataService = 0x00100000 & Feed, // must be URL
        NuGetODataService = 0x00200000 & Feed, // must be URL
    }

    internal class Recognizer {
        internal class RecognitionInfo {
            // internal ItemType itemType = ItemType.Unknown;
            internal string fullPath;
            internal Uri fullUrl;
            internal bool IsWildcard;
            internal bool IsUnknown {
                get { return !(IsPackageFeed | IsPackageFile); }
            }

            internal bool IsInvalid {get; set; }

            internal bool IsPackageFile { get; set; }
            internal bool IsPackageFeed { get; set; }

            internal bool IsURL { get; set; }
            internal bool IsFile { get; set; }
            internal bool IsFolder { get; set; }
            internal bool IsReference { get; set; }

            internal bool IsMSI { get { return IsCoAppMSI | IsLegacyMSI; } }
            internal bool IsCoAppMSI { get; set; }
            internal bool IsLegacyMSI { get; set; }
            internal bool IsLegacyEXE { get; set; }

            internal bool IsNugetPackage { get; set; }
            internal bool IsOpenwrapPackage { get; set; }
            
            internal bool IsArchive { get; set; }
            internal bool IsAtom { get; set; }

            internal bool IsCoAppODataService { get; set; }
            internal bool IsNugetODataService { get; set; }

        }

        private static Dictionary<string, RecognitionInfo> cache = new Dictionary<string, RecognitionInfo>();

        internal static RecognitionInfo Recognize(string item, string baseDirectory = null, string baseUrl = null) {
            if (cache.ContainsKey(item)) {
                return cache[item];
            }

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
                result.IsOpenwrapPackage= true;
            }

            // Is this an URL of some kind?
            if (result.IsUnknown) {
                try {
                    if (item.IndexOf("://") > -1) {
                        // some sort of URL
                        result.IsURL = true;
                        result.fullUrl = new Uri(item);
                    }
                    else if (!string.IsNullOrEmpty(baseUrl)) {
                        // it's a relative URL?
                        result.IsURL = true;
                        result.fullUrl = new Uri(new Uri(baseUrl), item);
                    }
                    // for now, we've got a full URL and it looks like it point somewhere.
                    // until we attempt to retrieve the URL we really can't bank on knowing what it is.
                    Registrar.HttpClient.PreviewFile(result.fullUrl, (cachedFileInfo) => {
                        if (cachedFileInfo.PreviewState == CachingNetworkClient.ActionState.Failed)
                            result.IsInvalid = true;
                        else {
                            if( cachedFileInfo.HasLocalFile ) {
                                var localInfo = Recognize(cachedFileInfo.LocalFullPath);
                                // TODO: GS01

                            }
                        }
                    });
                }
                catch {
                }
            }

            // Is this a directory?
            if (result.IsUnknown) {
                try {
                    if (Directory.Exists(item)) {
                        // a valid directory
                        result.IsFolder = true;
                        result.fullPath = Path.GetFullPath(item).ToLower();
                    }
                    else if (!string.IsNullOrEmpty(baseDirectory)) {
                        var path = Path.Combine(baseDirectory, item).ToLower();
                        if (Directory.Exists(path)) {
                            result.IsFolder = true;
                            result.fullPath = path;
                        }
                    }

                    if( result.IsFolder ) {
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
                        result.fullPath = Path.GetFullPath(item).ToLower();
                    }
                    else if (!string.IsNullOrEmpty(baseDirectory)) {
                        var path = Path.Combine(baseDirectory, item);
                        if (File.Exists(path)) {
                            result.IsFile = true;
                            result.fullPath = Path.GetFullPath(path).ToLower();
                        }

                        if (item.IndexOf('?') > -1 || item.IndexOf('*') > -1) {
                            // this has a wildcard. Past that, we don't know what it is yet.
                            result.IsWildcard = true;
                        }
                    }

                    if (result.IsFile) {
                        // so, this is a file. 
                        // let's do a little bit of diggin'

                        var ext = Path.GetExtension(result.fullPath);

                        switch (ext) {
                            case ".msi":
                                result.IsCoAppMSI = CoAppMSI.IsCoAppPackageFile(result.fullPath);
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
                                if (result.fullPath.IsXmlFile()) {
                                    try {
                                        var feed = AtomFeed.Load(result.fullPath);
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

            cache.Add(item, result);
            return result;
        }
    }
}