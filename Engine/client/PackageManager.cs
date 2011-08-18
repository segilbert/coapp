//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Toolkit.Engine.Client {
    using System.IO.Pipes;
    using System.Security.Principal;
    using System.Threading.Tasks;

    public class Package {

    };

    public class PackageManager {

        public static PackageManager Instance = new PackageManager();
        private NamedPipeClientStream _pipe;
        internal const int BufferSize = 8192;

        public bool IsConnected {
            get {
                return _pipe != null ? _pipe.IsConnected : false;
            }
        }
        

        private PackageManager() {
               
        }

        private void Connect(string clientName, string sessionId = null ) {
            if (IsConnected)
                return;

            sessionId = sessionId ?? DateTime.Now.Ticks.ToString();
            
            _pipe = new NamedPipeClientStream(".", "CoAppInstaller" , PipeDirection.InOut, PipeOptions.Asynchronous, TokenImpersonationLevel.Impersonation );

            
            try {
                _pipe.Connect();
                _pipe.ReadMode = PipeTransmissionMode.Message;
            } catch {
                _pipe = null;
                throw new Exception("Unable to connect to CoApp Service");
            }
            var incomingMessage = new byte[BufferSize];
            Task.Factory.StartNew(() => { _pipe.ReadAsync(incomingMessage, 0, BufferSize); });
            
            

        }

        private void Disconnect() {
            var pipe = _pipe;
            _pipe = null; 
            pipe.Close();
            pipe.Dispose();
        }

         /// <summary>
        ///   Writes the message to the stream asyncly.
        /// </summary>
        /// <param name = "message">The request.</param>
        /// <returns></returns>
        /// <remarks>
        /// </remarks>
        private void WriteAsync(UrlEncodedMessage message) {
            if (IsConnected) {
                try {
                    _pipe.WriteLineAsync(message.ToString()).ContinueWith(antecedent => { System.Console.WriteLine("Async Write Fail!? (1)"); }, TaskContinuationOptions.OnlyOnFaulted);
                }
                catch (Exception e) {
                     System.Console.WriteLine("Async Write Fail!? (2)");
                }
            }
        }

        private void StartSession(string clientId, string sessionId ) {

            WriteAsync(new UrlEncodedMessage("start-session") {
                {"client" , clientId },
                {"id"  , sessionId },
            });
        }
        
        public event Action NoPackagesFound;
        public event Action<string , DateTime, bool> FeedDetails;
        public event Action<string, int> ScanningPackagesProgress;
        public event Action<string, int> InstallingPackageProgress;
        public event Action<string, int> RemovingPackageProgress;
        public event Action<string> InstalledPackage;
        public event Action<string> RemovedPackage;
        public event Action<string,string,string> FailedPackageInstall;
        public event Action<string,string> FailedPackageRemoval;
        public event Action<string, IEnumerable<string>, string, bool> RequireRemoteFile;
        public event Action<string, bool, string> SignatureValidation;
        public event Action<string> PermissionRequired;
        public event Action<string, string, string > Error;
        public event Action<string, string, string> Warning;
        public event Action<string> FeedAdded;
        public event Action<string> FeedRemoved;
        public event Action<string> FeedSuppressed;
        public event Action NoFeedsFound;
        public event Action<string> FileNotFound;
        public event Action<string> UnknownPackage;
        public event Action<string> PackageBlocked;
        public event Action<string,string> FileNotRecognized;
        public event Action<string> Recognized;
        public event Action<string> OperationCancelled;
        public event Action<string, string, string> UnexpectedFailure;

        public event Action<Package, IEnumerable<Package>> PackageHasPotentialUpgrades;
        public event Action<Package> UnableToDownloadPackage;
        public event Action<Package> UnableToInstallPackage;
        public event Action<Package, IEnumerable<Package>> UnableToResolveDependencies;
        public event Action<Package, IEnumerable<Package>> PackageInformation;
        public event Action<Package> PackageDetails;
    }
}
