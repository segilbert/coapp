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
    using System.Net.Configuration;
    using System.Reflection;
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

        public TriggeredProperty<long> DownloadProgress;

        private Uri _actualRemoteLocation;
        private CancellationToken _cancellationToken;
        private long _contentLength;
        private Task _currentTask;
        private TaskType _currentTaskType;
        private string _folder;
        private DateTime _lastModified;
        private FileStream _filestream;

        public string Folder {
            get { return _folder; }
            set {
                _folder = value;
                if (LocalFullPath != null) {
                    LocalFullPath = Path.Combine(value, Path.GetFileName(LocalFullPath));
                }
            }
        }

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

        static RemoteFile() {
            //Get the assembly that contains the internal class 
            Assembly aNetAssembly = Assembly.GetAssembly(typeof (SettingsSection));
            if (aNetAssembly != null) {
                //Use the assembly in order to get the internal type for the internal class 
                Type aSettingsType = aNetAssembly.GetType("System.Net.Configuration.SettingsSectionInternal");
                if (aSettingsType != null) {
                    //Use the internal static property to get an instance of the internal settings class. 
                    //If the static instance isn't created allready the property will create it for us. 
                    object anInstance = aSettingsType.InvokeMember("Section",
                        BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.NonPublic, null, null, new object[] {});
                    if (anInstance != null) {
                        //Locate the private bool field that tells the framework is unsafe header parsing should be allowed or not 
                        FieldInfo aUseUnsafeHeaderParsing = aSettingsType.GetField("useUnsafeHeaderParsing",
                            BindingFlags.NonPublic | BindingFlags.Instance);
                        if (aUseUnsafeHeaderParsing != null) {
                            aUseUnsafeHeaderParsing.SetValue(anInstance, true);
                        }
                    }
                }
            }
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
            return CoTask.Factory.StartNew(Cancel);
        }

        internal void Serialize(Stream outputStream) {
        }

        internal static RemoteFile Deserialize(Stream inputStream) {
            return null;
        }

        private void Complete() {
            if( _filestream != null ) {
                _filestream.Dispose();
                _filestream = null;
            }

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

            return CoTask.Factory.FromAsync<WebResponse>(webRequest.BeginGetResponse, webRequest.EndGetResponse, this).ContinueWithParent(
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
                        catch( AggregateException ae ) {
                            ae = ae.Flatten();
                            var e = ae.InnerExceptions[0] as WebException;

                            if( e != null ) {
                                try {
                                    LastStatus = ((HttpWebResponse)e.Response).StatusCode;
                                }
                                catch (Exception) {
                                    // if the fit hits the shan, just call it not found.
                                    LastStatus = HttpStatusCode.NotFound;
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
                    });
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
            throw new Exception("FTP/HTTP/HTTPS only");
        }

        private Task GetImplHttp(bool resumeExistingDownload = true) {
            var webRequest = (HttpWebRequest) WebRequest.Create(RemoteLocation);
            webRequest.AllowAutoRedirect = true;
            webRequest.Method = WebRequestMethods.Http.Get;

            if (IsCancelled) {
                return CancelledTask();
            }

            return CoTask.Factory.FromAsync<WebResponse>(webRequest.BeginGetResponse, webRequest.EndGetResponse, this).ContinueWithParent(
                    asyncResult => {
                        try {
                            var v = webRequest;

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
                                    // we should open the file here, so that it's ready when we start the async read cycle.
                                    if( _filestream != null ) {
                                        throw new Exception("THIS VERY BAD AND UNEXPECTED.");
                                    }
                                    _filestream = File.Open(LocalFullPath, FileMode.Create);

                                    if (IsCancelled) {
                                        Cancel();
                                    }

                                    var tcs = new TaskCompletionSource<HttpWebResponse>(TaskCreationOptions.AttachedToParent);
                                    ((Tasklet) tcs.Task).CancellationToken = Tasklet.CurrentCancellationToken;

                                    tcs.Iterate(AsyncReadImpl(tcs, httpWebResponse));
                                    return;
                                }
                                catch {
                                    // failed to actually create the file, or some other catastrophic failure.

                                    Complete();
                                    return;
                                }
                            }
                            // this is not good. 
                            throw new Exception("Status Code other than OK");
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
                        catch (AggregateException ae) {
                            ae = ae.Flatten();
                            var e = ae.InnerExceptions[0] as WebException;

                            if (e != null) {
                                try {
                                    LastStatus = ((HttpWebResponse)e.Response).StatusCode;
                                }
                                catch (Exception) {
                                    // if the fit hits the shan, just call it not found.
                                    LastStatus = HttpStatusCode.NotFound;
                                } 
                            }

                            Complete();
                            return;
                        }
                        catch (Exception e) {
                            Console.WriteLine("BAD ERROR: {0}\r\n{1}", e.Message, e.StackTrace);
                            LastStatus = HttpStatusCode.NotFound;
                            Complete();
                            return;
                        }
                        Console.WriteLine("Really? It gets here?");
                        Complete();
                    });
        }

        private IEnumerable<Task> AsyncReadImpl(TaskCompletionSource<HttpWebResponse> tcs, HttpWebResponse httpWebResponse) {
            using (var responseStream = httpWebResponse.GetResponseStream()) {
                var total = 0L;
                var buffer = new byte[BUFFER_SIZE];
                while (true) {
                    if (IsCancelled) {
                        Cancel();
                        tcs.SetResult(null);
                        break;
                    }

                    var read = CoTask<int>.Factory.FromAsync(responseStream.BeginRead, responseStream.EndRead, buffer, 0,
                        buffer.Length, this);

                    yield return read;

                    var bytesRead = read.Result;
                    if (bytesRead == 0) {
                        break;
                    }

                    total += bytesRead;
                    DownloadProgress.Value = _contentLength <= 0 ? total : (int) (total*100/_contentLength);

                    // write to output file.
                    _filestream.Write(buffer, 0, bytesRead);
                    _filestream.Flush();
                }
                // end of the file!
                _filestream.Close();

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
                fname = _encodedValueRegex.Replace(fname, "#").Replace('/', '-').Replace(':', '-') + "$DEFAULT";
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