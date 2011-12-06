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
    using System.Net;
    using System.Net.Configuration;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensions;
    using Tasks;

    public class RemoteFile {
        public static IEnumerable<string> ServerSideExtensions = new[] {"asp", "aspx", "php", "jsp", "cfm"};
        public TriggeredProperty<long> DownloadProgress;

        public string Folder { get; set; }

        public Uri RemoteLocation { get; internal set; }

        public bool IsLocal {
            get { return File.Exists(LocalFullPath) && !IsPartial; }
        }

        public string LocalFullPath { get; internal set; }

        public bool HasPreviewed {
            get; set; }

        public HttpStatusCode LastStatus { get; internal set; }

        public CancellationToken CancellationToken {
            get;
            set;
        }

        private bool IsCancelled {
            get { return CancellationToken.IsCancellationRequested; }
        }

        public Uri ActualRemoteLocation {
            get;
            set;
        }

        public bool IsRedirect {
            get;
            set;
        }

        public DateTime LastModified {
            get;
            set;
        }

        public long ContentLength {
            get;
            set;
        }

        public long CurrentLength {
            get { throw new NotImplementedException(); }
        }

        public bool IsPartial {
            get { throw new NotImplementedException(); }
        }

        static RemoteFile() {
            throw new NotImplementedException();
        }

        private RemoteFile() {
            throw new NotImplementedException();
        }

        public RemoteFile(Uri remoteLocation) : this() {
            throw new NotImplementedException();
        }

        public RemoteFile(Uri remoteLocation, string localFolder) : this() {
            throw new NotImplementedException();
        }

        public RemoteFile(Uri remoteLocation, CancellationToken cancellationToken) : this(remoteLocation) {
            throw new NotImplementedException();
        }

        public RemoteFile(Uri remoteLocation, string localFolder, CancellationToken cancellationToken) : this(remoteLocation, localFolder) {
            throw new NotImplementedException();
        }

       
      

        internal void Serialize(Stream outputStream) {
        }

        internal static RemoteFile Deserialize(Stream inputStream) {
            throw new NotImplementedException();
        }


        public Task Preview() {
            throw new NotImplementedException();
        }

        private Task PreviewImpl() {
            throw new NotImplementedException();
        }

        private Task PreviewImplHttp() {
            throw new NotImplementedException();
        }

        private Task PreviewImplFtp() {
            throw new NotImplementedException();
        }

        public Task Get(bool resumeExistingDownload = true) {
            throw new NotImplementedException();
        }


        private Task GetImplHttp(bool resumeExistingDownload = true) {
            throw new NotImplementedException();
        }

        public Task Put(string localFilename = null) {
            throw new NotImplementedException();
        }

        private Task PutImpl(string localFilename = null) {
            throw new NotImplementedException();
        }

        public Task Stop(bool deletePartialDownload = false) {
            throw new NotImplementedException();
        }

        private Task StopImpl(bool deletePartialDownload = false) {
            throw new NotImplementedException();
        }

        public Task Delete() {
            throw new NotImplementedException();
        }

        private Task DeleteImpl() {
            throw new NotImplementedException();
        }

        public Task Move(Uri newRemoteLocation) {
            throw new NotImplementedException();
        }

        private Task MoveImpl(Uri newRemoteLocation) {
            throw new NotImplementedException();
        }

        public void DeleteLocalFile() {
            throw new NotImplementedException();
        }

        private void GenerateLocalFilename(string filename) {
            throw new NotImplementedException();
        }

        private void GenerateLocalFilename() {
            throw new NotImplementedException();
        }
    }
}