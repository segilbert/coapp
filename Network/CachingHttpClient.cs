////-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Network {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using Extensions;
    using Text;

    public class CachingHttpClient : CachingNetworkClient {
        private const int BUFFER_SIZE = 32768;

        public CachingHttpClient(string cachePath) : base(cachePath) {
        }

        protected override void PreviewFileImpl(Uri remotePath, Action<CachedFileInfo> operationCompleted) {
            var webRequest = (HttpWebRequest) WebRequest.Create(remotePath);
            webRequest.AllowAutoRedirect = false;
            webRequest.Method = "HEAD";
            var cachedFileInfo = _cache[remotePath];
            cachedFileInfo.PreviewState = ActionState.InProgress;
            webRequest.BeginGetResponse(asyncResult => {
                try {
                    var httpWebResponse = (HttpWebResponse) webRequest.EndGetResponse(asyncResult);
                    cachedFileInfo.StatusCode = httpWebResponse.StatusCode;
                    cachedFileInfo.PreviewState = ActionState.InProgress;

                    if (httpWebResponse.StatusCode == HttpStatusCode.Moved || httpWebResponse.StatusCode == HttpStatusCode.TemporaryRedirect) {
                        try {
                            PreviewFile(new Uri(httpWebResponse.Headers[HttpResponseHeader.Location]), result => {
                                cachedFileInfo.LocalFullPath = result.LocalFullPath;
                                cachedFileInfo.ContentLength = result.ContentLength;
                                cachedFileInfo.LastModified = result.LastModified;
                                cachedFileInfo.PreviewState = result.PreviewState;
                                cachedFileInfo.ActualRemoteLocation = result.ActualRemoteLocation;
                                SaveCache();
                                operationCompleted(cachedFileInfo);
                            });
                            return;
                        } catch (Exception e) { 
                            cachedFileInfo.PreviewState = ActionState.Failed;
                            SaveCache();
                            operationCompleted(cachedFileInfo);
                        }
                        return;
                    }

                    cachedFileInfo.ActualRemoteLocation = cachedFileInfo.RemoteLocation;

                    if (httpWebResponse.StatusCode == HttpStatusCode.OK) {
                        cachedFileInfo.LastModified = httpWebResponse.LastModified;
                        cachedFileInfo.ContentLength = httpWebResponse.ContentLength;
                        cachedFileInfo.GenerateLocalPath(CachePath, remotePath);

                        if (httpWebResponse.Headers.AllKeys.Contains("Content-Disposition")) {
                            var disp = httpWebResponse.Headers["Content-Disposition"];
                            var p = disp.IndexOf("filename=");
                            if (p > -1) {
                                cachedFileInfo.GenerateLocalPath(CachePath, HttpUtility.UrlDecode(disp.Substring(p + 1).Trim()));
                            }
                        }
                        cachedFileInfo.PreviewState = ActionState.Completed;
                        SaveCache();
                        operationCompleted(cachedFileInfo);
                        return;
                    }
                }
                catch (WebException e) {
                    try {
                        cachedFileInfo.StatusCode = ((HttpWebResponse) e.Response).StatusCode;
                    }
                    catch (Exception exa) {
                        // if the fit hits the shan, just call it not found.
                        cachedFileInfo.StatusCode = HttpStatusCode.NotFound;
                    }
                }
                // not ok.. something not "OK" happened.
                cachedFileInfo.PreviewState = ActionState.Failed;
                SaveCache();
                operationCompleted(cachedFileInfo);
            }, null);
        }

        protected override void DownloadFileImpl(Uri remotePath, Action<CachedFileInfo> operationCompleted, Action<int> downloadProgress) {
            var webRequest = (HttpWebRequest) WebRequest.Create(remotePath);
            webRequest.AllowAutoRedirect = true;
            webRequest.Method = "GET";
            var cachedFileInfo = _cache[remotePath];

            cachedFileInfo.DownloadState = ActionState.InProgress;
            webRequest.BeginGetResponse(asyncResult => {
                try {
                    var httpWebResponse = (HttpWebResponse) webRequest.EndGetResponse(asyncResult);
                    cachedFileInfo.StatusCode = httpWebResponse.StatusCode;

                    if (httpWebResponse.StatusCode == HttpStatusCode.OK) {
                        /*
                        var d = new Dictionary<string, List<string>>();
                        d.Add("header", httpWebResponse.Headers.AllKeys.ToList());
                        d.Add("value", httpWebResponse.Headers.AllKeys.Select(key => httpWebResponse.Headers[key]).ToList());
                        d.Dump(new[] { "Header", "Values" });
                        */
                        cachedFileInfo.LastModified = httpWebResponse.LastModified;
                        cachedFileInfo.ContentLength = httpWebResponse.ContentLength;
                        cachedFileInfo.GenerateLocalPath(CachePath, httpWebResponse.ResponseUri);
                            

                        if (httpWebResponse.Headers.AllKeys.Contains("Content-Disposition")) {
                            var disp = httpWebResponse.Headers["Content-Disposition"];
                            var p = disp.IndexOf("filename=");
                            if (p > -1) {
                                cachedFileInfo.GenerateLocalPath(CachePath, HttpUtility.UrlDecode(disp.Substring(p + 1).Trim()));
                            }
                        }

                        try {
                            var buffer = new byte[BUFFER_SIZE];
                            var responseStream = httpWebResponse.GetResponseStream();
                            if (File.Exists(cachedFileInfo.LocalFullPath)) {
                                try {
                                    File.Delete(cachedFileInfo.LocalFullPath);
                                }
                                catch {
                                    if (File.Exists(cachedFileInfo.LocalFullPath)) {
                                        // pick some other path if we can't seem to use the one we have.
                                        cachedFileInfo.LocalFullPath = cachedFileInfo.LocalFullPath + (DateTime.Now.Ticks%10000);
                                    }
                                }
                            }

                            var fileStream = File.Open(cachedFileInfo.LocalFullPath, FileMode.Create);
                            int total = 0;
                            AsyncCallback rc = null;
                            rc = asyncResult2 => {
                                cachedFileInfo.DownloadState = ActionState.InProgress;
                                try {
                                    var bytesRead = responseStream.EndRead(asyncResult2);
                                    if (bytesRead > 0) {
                                        total += bytesRead;
                                        cachedFileInfo.DownloadProgress = cachedFileInfo.ContentLength <= 0
                                            ? total
                                            : (int) (total*100/cachedFileInfo.ContentLength);
                
                                        downloadProgress(cachedFileInfo.DownloadProgress);
                                        // write to output file.
                                        fileStream.Write(buffer, 0, bytesRead);
                                        fileStream.Flush();

                                        responseStream.BeginRead(buffer, 0, BUFFER_SIZE, rc, null);
                                        return;
                                    }
                                    
                                    // end of the file!
                                    fileStream.Dispose();
                                    var fi = new FileInfo(cachedFileInfo.LocalFullPath);
                                    File.SetCreationTime(cachedFileInfo.LocalFullPath, cachedFileInfo.LastModified);
                                    File.SetLastWriteTime(cachedFileInfo.LocalFullPath, cachedFileInfo.LastModified);
                                    cachedFileInfo.ContentLength = fi.Length;
                                    cachedFileInfo.DownloadState = ActionState.Completed;
                                    SaveCache();
                                    operationCompleted(cachedFileInfo);
                                    return;
                                }
                                catch (Exception e) {
                                    cachedFileInfo.DownloadState = ActionState.Failed;
                                    SaveCache();
                                    operationCompleted(cachedFileInfo);
                                }
                            };

                            responseStream.BeginRead(buffer, 0, BUFFER_SIZE, rc, null);
                            return;
                        }
                        catch {
                            cachedFileInfo.DownloadState = ActionState.Failed;
                            SaveCache();
                            operationCompleted(cachedFileInfo);
                            return;
                        }
                    }
                    // if it gets here, we're destined for failure.
                }
                catch (WebException e) {
                    try {
                        cachedFileInfo.StatusCode = ((HttpWebResponse) e.Response).StatusCode;
                    }
                    catch (Exception exa) {
                        cachedFileInfo.StatusCode = HttpStatusCode.NotFound;
                    }
                }
                cachedFileInfo.DownloadState = ActionState.Failed;
                SaveCache();
                operationCompleted(cachedFileInfo);
                return;
            }, null);
        }
    }
}