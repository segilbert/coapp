//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Network {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensions;
    using Tasks;

    public class RemoteFile {
        private const string SchemeHttp = "http";
        private const string SchemeHttps = "https";
        private const string SchemeFtp = "ftp";
        private const int BUFFER_SIZE = 32768;
        private static readonly Regex _uriRegex = new Regex(".*://");
        private static readonly Regex _encodedValueRegex = new Regex("%..");

        public static IEnumerable<string> ServerSideExtensions = new[] {"asp", "aspx", "php", "jsp", "cfm"};
        private string _folder;
        public string Folder { get { return _folder; } set { _folder = value;
        if (LocalFullPath != null)
            LocalFullPath = Path.Combine(value, Path.GetFileName(LocalFullPath));
        } }

        public TriggeredProperty<long> DownloadProgress;

        private Uri _actualRemoteLocation;
        private CancellationToken _cancellationToken;
        private long _contentLength;
        private Task _currentTask;
        private TaskType _currentTaskType;
        private DateTime _lastModified;

        public Uri RemoteLocation { get; internal set; }

        public bool IsLocal {
            get { return File.Exists(LocalFullPath) && !IsPartial; }
        }

        public string LocalFullPath { get; internal set; }

        public bool HasPreviewed {
            get { return _lastModified != DateTime.MinValue; }
        }

        public HttpStatusCode LastStatus { get; internal set; }

        public CancellationToken CancellationToken {
            get { return _cancellationToken; }
            set {
                if (_cancellationToken != value) {
                    if (_currentTaskType != TaskType.None) {
                        throw new Exception("Cancellation Token Should not be changed while tasks are in progress");
                    }

                    _cancellationToken = value;
                }
            }
        }

        private bool IsCancelled {
            get { return CancellationToken.IsCancellationRequested; }
        }

        public Uri ActualRemoteLocation {
            get { return _actualRemoteLocation ?? RemoteLocation; }
            internal set { _actualRemoteLocation = value; }
        }

        public bool IsRedirect {
            get { return _actualRemoteLocation != null; }
        }

        public DateTime LastModified {
            get { return _lastModified; }
        }

        public long ContentLength {
            get { return _contentLength; }
        }

        public long CurrentLength {
            get { return File.Exists(LocalFullPath) ? new FileInfo(LocalFullPath).Length : -1; }
        }

        public bool IsPartial {
            get { return ContentLength != CurrentLength; }
        }

        private RemoteFile() {
            _folder = Environment.CurrentDirectory;
            _lastModified = DateTime.MinValue;
            LastStatus = HttpStatusCode.Unused;
            DownloadProgress = new TriggeredProperty<long>(-1, value => value != DownloadProgress.Value) {TripOnce = false};
            _currentTaskType = TaskType.None;
        }

        public RemoteFile(Uri remoteLocation) : this() {
            RemoteLocation = remoteLocation;
        }

        public RemoteFile(Uri remoteLocation, string localFolder) : this() {
            RemoteLocation = remoteLocation;
            _folder = localFolder;
            if (!Directory.Exists(_folder)) {
                Directory.CreateDirectory(_folder);
            }
        }

        public RemoteFile(Uri remoteLocation, CancellationToken cancellationToken) : this(remoteLocation) {
            CancellationToken = cancellationToken;
        }

        public RemoteFile(Uri remoteLocation, string localFolder, CancellationToken cancellationToken) : this(remoteLocation, localFolder) {
            CancellationToken = cancellationToken;
        }

        private void Cancel() {
            if (_currentTaskType != TaskType.None) {
                Complete();
            }
        }

        private Task CancelledTask() {
            return Task.Factory.StartNew(Cancel);
        }

        internal void Serialize(Stream outputStream) {
        }

        internal static RemoteFile Deserialize(Stream inputStream) {
            return null;
        }

        private void Complete() {
            if (_currentTaskType == TaskType.Get) {
                DownloadProgress = new TriggeredProperty<long>(0, value => value != DownloadProgress.Value) {TripOnce = false};
            }

            lock (this) {
                _currentTaskType = TaskType.None;
                _currentTask = null;
            }
        }

        private Task RunOperation(TaskType type, Operation publicOperation, Operation privateOperation) {
            if (IsCancelled) {
                return CancelledTask();
            }

            lock (this) {
                if (_currentTaskType == type) {
                    return _currentTask;
                }

                if (_currentTaskType != TaskType.None) {
                    return _currentTask.ContinueWithParent(antecedent => publicOperation().Wait());
                }
                _currentTaskType = type;
                return _currentTask = privateOperation();
            }
        }

        public Task Preview() {
            return RunOperation(TaskType.Preview, Preview, PreviewImpl);
        }

        private Task PreviewImpl() {
            switch (RemoteLocation.Scheme) {
                case SchemeHttp:
                case SchemeHttps:
                    return PreviewImplHttp();
                case SchemeFtp:
                    return PreviewImplFtp();
            }
            throw new ProtocolViolationException();
        }

        private Task PreviewImplHttp() {
            var webRequest = (HttpWebRequest) WebRequest.Create(RemoteLocation);
            webRequest.AllowAutoRedirect = false;
            webRequest.Method = WebRequestMethods.Http.Head;

            if (IsCancelled) {
                return CancelledTask();
            }

            return
                Task.Factory.FromAsync<WebResponse>(webRequest.BeginGetResponse, webRequest.EndGetResponse, this).ContinueWith(
                    asyncResult => {
                        try {
                            if (IsCancelled) {
                                Cancel();
                            }

                            var httpWebResponse = asyncResult.Result as HttpWebResponse;
                            LastStatus = httpWebResponse.StatusCode;

                            if (httpWebResponse.StatusCode == HttpStatusCode.Moved ||
                                httpWebResponse.StatusCode == HttpStatusCode.TemporaryRedirect) {
                                try {
                                    var rf = new RemoteFile(new Uri(httpWebResponse.Headers[HttpResponseHeader.Location]), CancellationToken);
                                    if (IsCancelled) {
                                        Cancel();
                                    }

                                    rf.Preview().Wait();
                                    LastStatus = rf.LastStatus;

                                    if (rf.HasPreviewed) {
                                        ActualRemoteLocation = rf.ActualRemoteLocation;
                                        _contentLength = rf.ContentLength;
                                        _lastModified = rf.LastModified;
                                        LocalFullPath = rf.LocalFullPath;
                                    }
                                }
                                catch {
                                    // not really sure if we should do something here. 
                                }
                                Complete();
                                return;
                            }

                            if (httpWebResponse.StatusCode == HttpStatusCode.OK) {
                                _lastModified = httpWebResponse.LastModified;
                                _contentLength = httpWebResponse.ContentLength;
                                if (IsCancelled) {
                                    Cancel();
                                }

                                GenerateLocalFilename();

                                var filename = httpWebResponse.ContentDispositionFilename();
                                if (!string.IsNullOrEmpty(filename)) {
                                    GenerateLocalFilename(filename);
                                }
                            }
                        }
                        catch (WebException e) {
                            try {
                                LastStatus = ((HttpWebResponse) e.Response).StatusCode;
                            }
                            catch (Exception) {
                                // if the fit hits the shan, just call it not found.
                                LastStatus = HttpStatusCode.NotFound;
                            }
                        }
                        Complete();
                    }, TaskContinuationOptions.AttachedToParent);
        }

        private Task PreviewImplFtp() {
            throw new NotImplementedException();
        }

        public Task Get(bool resumeExistingDownload = true) {
            return RunOperation(TaskType.Get, () => Get(resumeExistingDownload), () => GetImpl(resumeExistingDownload));
        }

        private Task GetImpl(bool resumeExistingDownload = true) {
            switch (RemoteLocation.Scheme) {
                case SchemeHttp:
                case SchemeHttps:
                    return GetImplHttp(resumeExistingDownload);
                case SchemeFtp:
                    return GetImplFtp(resumeExistingDownload);
            }
            throw new ProtocolViolationException();
        }

        private Task GetImplHttp(bool resumeExistingDownload = true) {
            var webRequest = (HttpWebRequest) WebRequest.Create(RemoteLocation);
            webRequest.AllowAutoRedirect = true;
            webRequest.Method = WebRequestMethods.Http.Get;

            if (IsCancelled) {
                return CancelledTask();
            }

            return
                Task.Factory.FromAsync<WebResponse>(webRequest.BeginGetResponse, webRequest.EndGetResponse, this).ContinueWith(
                    asyncResult => {
                        try {
                            if (IsCancelled) {
                                Cancel();
                            }

                            var httpWebResponse = asyncResult.Result as HttpWebResponse;
                            LastStatus = httpWebResponse.StatusCode;

                            if (httpWebResponse.StatusCode == HttpStatusCode.OK) {
                                _lastModified = httpWebResponse.LastModified;
                                _contentLength = httpWebResponse.ContentLength;
                                ActualRemoteLocation = httpWebResponse.ResponseUri;

                                if (IsCancelled) {
                                    Cancel();
                                }

                                GenerateLocalFilename();

                                var filename = httpWebResponse.ContentDispositionFilename();
                                if (!string.IsNullOrEmpty(filename)) {
                                    GenerateLocalFilename(filename);
                                }

                                try {
                                    if (IsCancelled) {
                                        Cancel();
                                    }

                                    var tcs = new TaskCompletionSource<HttpWebResponse>(TaskCreationOptions.AttachedToParent);
                                    tcs.Iterate(AsyncReadImpl(tcs, httpWebResponse));
                                    return;
                                }
                                catch {
                                    // not sure what to do here either.
                                    Complete();
                                    return;
                                }
                            }
                            // this is not good. 
                            // throw new Exception("Status Code other than OK");
                            Complete();
                        }
                        catch (WebException e) {
                            try {
                                LastStatus = ((HttpWebResponse) e.Response).StatusCode;
                            }
                            catch (Exception) {
                                // if the fit hits the shan, just call it not found.
                                LastStatus = HttpStatusCode.NotFound;
                            }
                            Complete();
                        }
                        Console.WriteLine("Really? It gets here?");
                        Complete();
                    }, TaskContinuationOptions.AttachedToParent);
        }

        private IEnumerable<Task> AsyncReadImpl(TaskCompletionSource<HttpWebResponse> tcs, HttpWebResponse httpWebResponse) {
            using (var responseStream = httpWebResponse.GetResponseStream()) {
                using (var fileStream = File.Open(LocalFullPath, FileMode.Create)) {
                    var total = 0L;
                    var buffer = new byte[BUFFER_SIZE];
                    while (true) {
                        if (IsCancelled) {
                            Cancel();
                            tcs.SetResult(null);
                            break;
                        }

                        var read = Task<int>.Factory.FromAsync(responseStream.BeginRead, responseStream.EndRead, buffer, 0,
                            buffer.Length, this, TaskCreationOptions.AttachedToParent);

                        yield return read;

                        var bytesRead = read.Result;
                        if (bytesRead == 0) {
                            break;
                        }

                        total += bytesRead;
                        DownloadProgress.Value = _contentLength <= 0 ? total : (int) (total*100/_contentLength);

                        // write to output file.
                        fileStream.Write(buffer, 0, bytesRead);
                        fileStream.Flush();
                    }
                    // end of the file!
                }
                try {
                    if (IsCancelled) {
                        Cancel();
                        tcs.SetResult(null);
                    }

                    var fi = new FileInfo(LocalFullPath);
                    File.SetCreationTime(LocalFullPath, LastModified);
                    File.SetLastWriteTime(LocalFullPath, LastModified);
                    if (_contentLength == 0) {
                        _contentLength = fi.Length;
                    }

                    Complete();
                    tcs.SetResult(null);
                }
                catch (Exception e) {
                    Console.WriteLine(e.Message);
                    Complete();
                    tcs.SetException(e);
                }
            }
        }

        private Task GetImplFtp(bool resumeExistingDownload = true) {
            throw new NotImplementedException();
        }

        public Task Put(string localFilename = null) {
            localFilename = localFilename ?? LocalFullPath;
            return RunOperation(TaskType.Put, () => Put(localFilename), () => PutImpl(localFilename));
        }

        private Task PutImpl(string localFilename = null) {
            throw new NotImplementedException();
        }

        public Task Stop(bool deletePartialDownload = false) {
            return RunOperation(TaskType.Stop, () => Stop(deletePartialDownload), () => StopImpl(deletePartialDownload));
        }

        private Task StopImpl(bool deletePartialDownload = false) {
            throw new NotImplementedException();
        }

        public Task Delete() {
            return RunOperation(TaskType.Delete, Delete, DeleteImpl);
        }

        private Task DeleteImpl() {
            throw new NotImplementedException();
        }

        public Task Move(Uri newRemoteLocation) {
            return RunOperation(TaskType.Move, () => Move(newRemoteLocation), () => MoveImpl(newRemoteLocation));
        }

        private Task MoveImpl(Uri newRemoteLocation) {
            throw new NotImplementedException();
        }

        public void DeleteLocalFile() {
            throw new NotImplementedException();
        }

        private void GenerateLocalFilename(string filename) {
            LocalFullPath = Path.Combine(_folder, filename);
        }

        private void GenerateLocalFilename() {
            var fname = ActualRemoteLocation.LocalPath.Substring(ActualRemoteLocation.LocalPath.LastIndexOf('/') + 1);

            if (string.IsNullOrEmpty(fname)) {
                fname = _uriRegex.Replace(ActualRemoteLocation.AbsoluteUri, "");
                fname = _encodedValueRegex.Replace(fname, "#").Replace('/', '-') + "$DEFAULT";
            }
            GenerateLocalFilename(fname);
        }

        #region Nested type: Operation

        private delegate Task Operation();

        #endregion

        #region Nested type: TaskType

        private enum TaskType {
            None,
            Preview,
            Get,
            Put,
            Stop,
            Delete,
            Move
        }

        #endregion
    }
}