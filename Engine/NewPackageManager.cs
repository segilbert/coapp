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
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Tasks;

    public class NewPackageManager {

        public static NewPackageManager Instance = new NewPackageManager();

        private NewPackageManager() {
            
        }

        public Task FindPackages( NewPackageManagerMessages messages ) {
            var t = Task.Factory.StartNew(() => {
                messages.Register();

                NewPackageManagerMessages.Invoke.UnexpectedFailure(new NotImplementedException());

                // 
                // NewPackageManagerMessages.Invoke.PackageInformation(package);

            }).AutoManage();
            return t;
        }

        public Task GetPackageDetails(string canonicalName, NewPackageManagerMessages messages) {
            var t = Task.Factory.StartNew(() => {
                messages.Register();

                NewPackageManagerMessages.Invoke.UnexpectedFailure(new NotImplementedException());

                // 
                // NewPackageManagerMessages.Invoke.PackageInformation(package);

            }).AutoManage();
            return t;
        }

        public Task InstallPackage(string canonicalName, bool? autoUpgrade, bool? force, NewPackageManagerMessages messages) {
            var t = Task.Factory.StartNew(() => {
                messages.Register();

                NewPackageManagerMessages.Invoke.UnexpectedFailure(new NotImplementedException());

                // 
                // NewPackageManagerMessages.Invoke.PackageInformation(package);

            }).AutoManage();
            return t;
        }

        public Task ListFeeds(NewPackageManagerMessages messages) {
            var t = Task.Factory.StartNew(() => {
                messages.Register();

                NewPackageManagerMessages.Invoke.UnexpectedFailure(new NotImplementedException());


                // 
                // NewPackageManagerMessages.Invoke.PackageInformation(package);

            }).AutoManage();
            return t;
        }

        public Task RemoveFeed(string location, bool? session, NewPackageManagerMessages messages) {
            var t = Task.Factory.StartNew(() => {
                messages.Register();

                NewPackageManagerMessages.Invoke.UnexpectedFailure(new NotImplementedException());

                // 
                // NewPackageManagerMessages.Invoke.PackageInformation(package);

            }).AutoManage();
            return t;
        }

        public Task AddFeed(string location, bool? session, NewPackageManagerMessages messages) {
            var t = Task.Factory.StartNew(() => {
                messages.Register();

                NewPackageManagerMessages.Invoke.UnexpectedFailure(new NotImplementedException());

                // 
                // NewPackageManagerMessages.Invoke.PackageInformation(package);

            }).AutoManage();
            return t;
        }

        public Task VerifyFileSignature(string filename, NewPackageManagerMessages messages) {
            var t = Task.Factory.StartNew(() => {
                messages.Register();

                NewPackageManagerMessages.Invoke.UnexpectedFailure(new NotImplementedException());

                // 
                // NewPackageManagerMessages.Invoke.PackageInformation(package);

            }).AutoManage();
            return t;
        }

        public Task SetPackage(string canonicalName, bool? active, bool? required, bool? blocked, NewPackageManagerMessages messages) {
            var t = Task.Factory.StartNew(() => {
                messages.Register();

                NewPackageManagerMessages.Invoke.UnexpectedFailure(new NotImplementedException());

                // 
                // NewPackageManagerMessages.Invoke.PackageInformation(package);

            }).AutoManage();
            return t;
        }

        public Task RemovePackage(string canonicalName, NewPackageManagerMessages messages) {
            var t = Task.Factory.StartNew(() => {
                messages.Register();

                NewPackageManagerMessages.Invoke.UnexpectedFailure(new NotImplementedException());

                // 
                // NewPackageManagerMessages.Invoke.PackageInformation(package);

            }).AutoManage();
            return t;
        }

        public Task UnableToAcquire(string referenceId, NewPackageManagerMessages messages) {
            var t = Task.Factory.StartNew(() => {
                messages.Register();

                NewPackageManagerMessages.Invoke.UnexpectedFailure(new NotImplementedException());

                // 
                // NewPackageManagerMessages.Invoke.PackageInformation(package);

            }).AutoManage();
            return t;
        }

        public Task RecognizeFile(string referenceId, string localLocation, string remoteLocation, NewPackageManagerMessages messages) {
            var t = Task.Factory.StartNew(() => {
                messages.Register();

                NewPackageManagerMessages.Invoke.UnexpectedFailure(new NotImplementedException());

                // 
                // NewPackageManagerMessages.Invoke.PackageInformation(package);

            }).AutoManage();
            return t;
        }

        internal void Updated() {
            
        }

        internal IEnumerable<string> BlockedScanLocations {
            get { return Enumerable.Empty<string>(); }
        }

        internal Package GetPackageFromFilename(string filename ) {
            throw new NotImplementedException();
        }

        internal Package GetPackageByDetails(string name, ulong version, string architecture, string publicKeyToken, string id) {
            throw new NotImplementedException();
        }

        internal IEnumerable<Package> InstalledPackages {
            get {
                throw new NotImplementedException();
            }
        }
    }

    public class PackageManagerSessionData {
        
    }
    
}
