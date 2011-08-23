//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;
    using Extensions;
    
    using Network;
    using PackageFormatHandlers;
    using Tasks;

    /// <summary>
    /// NOTE: EXPLICITLY IGNORE, This is getting refactored 
    /// </summary>
    internal class Recognizer {
        internal class RecognizerState {
            internal string OriginalUrl;
            internal string LocalLocation;
        }

        private static Task<RecognitionInfo> CacheAndReturnTask( string itemPath , RecognitionInfo recognitionInfo) {
            SessionCache<RecognitionInfo>.Value[itemPath] = recognitionInfo;
            return recognitionInfo.AsResultTask();
        }

         private static RecognitionInfo Cache( string itemPath , RecognitionInfo recognitionInfo) {
            SessionCache<RecognitionInfo>.Value[itemPath] = recognitionInfo;
             return recognitionInfo;
         }

        internal static Task<RecognitionInfo> Recognize(string item) {
            var cachedResult = SessionCache<RecognitionInfo>.Value[item];
            if( cachedResult != null ) {
                return cachedResult.AsResultTask();
            }
            
            try {
                var location = new Uri(item);
                if( !location.IsFile ) {
                    // some sort of remote item.
                    // since we can't do anything with a remote item directly, 
                    // we have to issue a request to the client to get it for us

                    // first let's create a delegate to run when the file gets resolved.
                    var completion = new Task<RecognitionInfo>((recognizerState) => {
                        var state = recognizerState as RecognizerState;
                        if( state == null || string.IsNullOrEmpty(state.LocalLocation) ) {
                            // didn't fill in the local location? -- this happens when the client can't download.
                            // PackageManagerMessages.Invoke.FileNotRecognized() ?
                            return new RecognitionInfo {
                                FullPath = location.AbsoluteUri,
                                IsInvalid = true,
                            } ;
                        }
                        var newLocation = new Uri(state.LocalLocation);
                        if( newLocation.IsFile ) {
                            var continuedResult = Recognize(state.LocalLocation).Result;
                            
                            // create the result object 
                            var result = new RecognitionInfo {
                                FullUrl = location,
                            };

                            result.CopyDetailsFrom(continuedResult);
                            return Cache(item, result);
                        }
                        // so, the callback comes, but it's not a file. 
                        // 
                        return new RecognitionInfo {
                            FullPath = location.AbsoluteUri,
                            IsInvalid = true,
                        } ;
                    }, new RecognizerState { OriginalUrl = location.AbsoluteUri } , TaskCreationOptions.AttachedToParent);

                    // store the task until the client tells us that it has the file.
                    SessionCache<Task<RecognitionInfo>>.Value[item] = completion;

                    // GS01: Should we make a deeper path in the cache directory?
                    // perhaps that would let us use a cached version of the file we're looking for.
                    PackageManagerMessages.Invoke.RequireRemoteFile(item, location.AbsoluteUri.SingleItemAsEnumerable(), PackageManagerSettings.CoAppCacheDirectory,false);

                    // return the completion task, as whatever is waiting for this 
                    // needs to continue on that.
                    return completion;
                }

                //----------------------------------------------------------------
                // we've managed to find a file system path.
                // let's figure out what it is.
                var localPath = location.LocalPath;

                if( localPath.Contains("?") || localPath.Contains("*") ) {
                    // looks like a wildcard package feed.
                    // which is a directory feed with a filter.
                    var i = localPath.IndexOfAny(new [] {'*','?'} );

                    var lastSlash = localPath.LastIndexOf('\\', i);
                    var folder = localPath.Substring(0, lastSlash);
                    if (Directory.Exists(folder)) {
                        return CacheAndReturnTask(item, new RecognitionInfo {
                            FullPath = folder,
                            Filter = localPath.Substring(lastSlash + 1),
                            IsFolder = true,
                            IsPackageFeed = true
                        });
                    }
                }

                if (Directory.Exists(localPath)) {
                    // it's a directory.
                    // which means that it's a package feed.
                    return CacheAndReturnTask( item, new RecognitionInfo {
                        FullPath = localPath,
                        Filter = "*",
                        IsFolder = true,
                        IsPackageFeed = true,
                    });
                }

                if( File.Exists(localPath)) {
                    var ext = Path.GetExtension(localPath);
                    var result = new RecognitionInfo {
                        IsFile = true,
                    };

                    switch (ext) {
                        case ".msi":
                            result.IsCoAppMSI = CoAppMSI.IsCoAppPackageFile(localPath);
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
                                    // this could be an atom feed

                                        
                                    /*
                                    var feed = AtomFeed.Load(result.FullPath);
                                    if (feed != null) {
                                        result.IsPackageFeed = true;
                                        result.IsAtom = true;
                                    }
                                        * */

                                }
                                catch {
                                    // can't seem to figure out what this is. 
                                    result.IsInvalid = true;
                                }
                            }
                            break;
                    }
                    return CacheAndReturnTask(item, result);
                }
                
               

            } catch(UriFormatException) {
            }
            // item wasn't able to match any known URI, UNC or Local Path format.
            // or was file not found
                return new RecognitionInfo {
                FullPath = item,
                IsInvalid = true,
            }.AsResultTask();
        }

        #region Nested type: RecognitionInfo

        internal class RecognitionInfo {
            

            internal string Filter { get; set; }
            internal string FullPath { get; set; }

            internal Uri FullUrl {
                get; set; 
            }

            internal bool IsUnknown {
                get { return !(IsPackageFeed | IsPackageFile); }
            }

            internal bool IsInvalid { get; set; }

            internal bool IsPackageFile { get; set; }
            internal bool IsPackageFeed { get; set; }

            internal bool IsURL { get; set; }
            internal bool IsFile { get; set; }
            internal bool IsFolder { get; set; }

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