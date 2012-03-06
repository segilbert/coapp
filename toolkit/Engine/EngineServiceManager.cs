//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine {
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Pipes;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Security.Principal;
    using System.ServiceProcess;
    using System.Threading;
    using Exceptions;
    using Extensions;
    using Logging;
    using TimeoutException = System.TimeoutException;

    public static class EngineServiceManager {
        public const string CoAppServiceName = "CoApp Package Installer Service";

        public static bool IsServiceInstalled {
            get { return ServiceController.GetServices().Any(service => service.ServiceName == CoAppServiceName); }
        }

        private static readonly Lazy<ServiceController> _controller = new Lazy<ServiceController>(() => new ServiceController(CoAppServiceName));

        // some dumbass thought that they should just return the last value, forcing developers to 'refresh' to get the current value. 
        // Not quite sure WHY THIS EVER SOUNDED LIKE A GOOD IDEA!? IF I WANTED THE LAST F$(*&%^*ING VALUE, I'D HAVE CACHED IT MYSELF DUMBASS!
        public static ServiceControllerStatus Status { get {
            _controller.Value.Refresh();
            return _controller.Value.Status; 
        }}

        public static bool CanStop { get {
            _controller.Value.Refresh();
            return _controller.Value.CanStop;
        }}

        public static bool IsServiceRunning {
            get {
                return IsServiceInstalled && Status == ServiceControllerStatus.Running;
            }
        }

        public static void TryToStopService() {
            if (!IsServiceInstalled) {
                throw new UnableToStopServiceException("{0} is not installed".format(CoAppServiceName));
            }

            if (Status != ServiceControllerStatus.Stopped && CanStop) {
                _controller.Value.Stop();
            }
        }

       
        public static void TryToStartService(bool secondAttempt = false) {
            if (!IsServiceInstalled) {
                throw new UnableToStartServiceException("{0} is not installed".format(CoAppServiceName));
            }

            Logger.Warning("==[Trying to start Win32 Service]==");

            switch (Status) {
                case ServiceControllerStatus.ContinuePending:
                    Logger.Warning("==[State:Continuing]==");
                    // wait for it to continue.
                    try {
                        _controller.Value.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 0, 0, 10));
                    }
                    catch (TimeoutException) {
                        throw new UnableToStartServiceException(
                            "Service is in the '{0}' state, and didn't respond before timing out.".format(Status.ToString()));
                    }
                    if (Status != ServiceControllerStatus.Running) {
                        if (secondAttempt) {
                            throw new UnableToStartServiceException(
                                "Service is in the '{0}' state, and was expected to be in the 'Running' state.".format(Status.ToString()));
                        }
                        TryToStartService(true);
                    }
                    break;
                case ServiceControllerStatus.PausePending:
                    Logger.Warning("==[State:Pausing]==");
                    try {
                        _controller.Value.WaitForStatus(ServiceControllerStatus.Paused, new TimeSpan(0, 0, 0, 10));
                    }
                    catch (TimeoutException) {
                        throw new UnableToStartServiceException(
                            "Service is in the '{0}' state, and didn't respond before timing out.".format(Status.ToString()));
                    }
                    if (Status == ServiceControllerStatus.Paused) {
                        TryToStartService();
                    }
                    if (Status != ServiceControllerStatus.Running) {
                        if (secondAttempt) {
                            throw new UnableToStartServiceException(
                                "Service is in the '{0}' state, and was expected to be in the 'Running' state.".format(Status.ToString()));
                        }
                        TryToStartService(true);
                    }
                    break;
                case ServiceControllerStatus.Paused:
                    Logger.Warning("==[State:Paused]==");
                    _controller.Value.Continue();
                    try {
                        _controller.Value.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 0, 0, 10));
                    }
                    catch (TimeoutException) {
                        if (secondAttempt) {
                            throw new UnableToStartServiceException(
                                "Service is in the '{0}' state, and didn't respond before timing out.".format(Status.ToString()));
                        }
                        TryToStartService(true);
                    }
                    break;

                case ServiceControllerStatus.Running:
                    Logger.Warning("==[State:Running]==");
                    // duh!
                    break;

                case ServiceControllerStatus.StartPending:
                    Logger.Warning("==[State:Starting]==");
                    try {
                        _controller.Value.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 0, 0, 10));
                    }
                    catch (TimeoutException) {
                        if (secondAttempt) {
                            throw new UnableToStartServiceException(
                                "Service is in the '{0}' state, and didn't respond before timing out.".format(Status.ToString()));
                        }
                        TryToStartService(true);
                    }
                    break;

                case ServiceControllerStatus.StopPending:
                    Logger.Warning("==[State:Stopping]==");
                    try {
                        _controller.Value.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, 0, 0, 10));
                    }
                    catch (TimeoutException) {
                        throw new UnableToStartServiceException(
                            "Service is in the '{0}' state, and didn't respond before timing out.".format(Status.ToString()));
                    }
                    if (Status == ServiceControllerStatus.Stopped) {
                        TryToStartService();
                    }
                    if (Status != ServiceControllerStatus.Running) {
                        if (secondAttempt) {
                            throw new UnableToStartServiceException(
                                "Service is in the '{0}' state, and was expected to be in the 'Running' state.".format(Status.ToString()));
                        }
                        TryToStartService(true);
                    }
                    break;

                case ServiceControllerStatus.Stopped:
                    Logger.Warning("==[State:Stopped]==");
                    _controller.Value.Start();
                    try {
                        _controller.Value.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 0, 0, 10));
                    }   
                    catch (TimeoutException) {
                        if (secondAttempt) {
                            throw new UnableToStartServiceException(
                                "Service is in the '{0}' state, and didn't respond before timing out.".format(Status.ToString()));
                        }
                        TryToStartService(true);
                    }
                    break;
            }
        }

        public static bool IsEngineResponding {
            get {
                lock (typeof (EngineServiceManager)) {
                    Logger.Warning("==[Checking For Process]==");
                    if (IsProcessOrServiceRunning) {
                        Logger.Warning("==[Looks like the process is running]==");
                        for (var i = 60; i > 0; i--) {
                            if (!IsServerPipeConnectionAvailable) {
                                Thread.Sleep(50); // pause to let the service do what it needs to.
                                if (!IsProcessOrServiceRunning) {
                                    // hmm, it's gone away. 
                                    // let's get out of here
                                    return false;
                                }
                            }
                            var testPipe = new NamedPipeClientStream(".", "CoAppInstaller", PipeDirection.InOut, PipeOptions.Asynchronous,
                                TokenImpersonationLevel.Impersonation);
                            try {
                                Logger.Warning("==[Checking For Pipe Response]==");
                                testPipe.Connect(100);
                                testPipe.Close();
                                testPipe.Dispose();
                                Logger.Warning("==[Service Seems to be responding]==");
                                return true;
                            } catch (System.TimeoutException) {
                            }
                        }
                    }
                }
                return false;
            }
        }

        private static bool IsProcessRunning { get { return Process.GetProcessesByName("coapp.service").Any(); }}
        private static bool IsServerPipeConnectionAvailable { get { return System.IO.Directory.GetFiles(@"\\.\pipe\").Where(each => each.StartsWith(@"\\.\pipe\CoAppInstaller")).Any(); } }
        private static bool IsProcessOrServiceRunning { get { return IsServiceRunning || IsProcessRunning; } }

        public static void EnsureServiceIsResponding(bool forceInteractive = false) {
            lock (typeof(EngineServiceManager)) {

                if (forceInteractive) {
                    if (IsServiceInstalled && IsServiceRunning) {
                        TryToStopService();
                        while (IsProcessOrServiceRunning) {
                            Thread.Sleep(20);
                        }
                    }
                    if (!IsEngineResponding) {
                        // it's probably one started interactively before...
                        TryToRunServiceInteractively();
                    }
                    return;
                }

                if (IsServiceInstalled) {
                    if (IsProcessRunning) {
                        if (!IsEngineResponding) {
                            // service is installed & running, but not responding. 
                            // let the client deal with it.
                            try {
                                // it's not running. try to make it go!
                                TryToStartService();
                            } catch {
                                throw new UnableToStartServiceException("Service is installed & running, but not responding to it's pipe.");
                            }
                        }
                        return; // it's running!
                    }

                    try {
                        // it's not running. try to make it go!
                        TryToStartService();
                    } catch {

                    }
                }

                // wouldn't/couldn't start. 
                if (!IsEngineResponding) {
                    // Hmm. We've got to a point where the service isn't running, can seem to start it.
                    // its possible that we've gotten here because this is the first time 
                    // that CoApp is run, and we're just not installed yet.
                    // If it's not installed, and we have enough rights to do that, 
                    // lets find the service exe and ask it to install and start itself.
                    if (!IsServiceInstalled) {
                        InstallAndStartService();
                        if (IsEngineResponding) {
                            return;
                        }

                        if (!IsEngineResponding) {
                            // NOTE TODO REMOVE FOR RELEASE it's probably one started interactively before...
                            TryToRunServiceInteractively();

                        }

                        if (!IsEngineResponding) {
                            // Tried to install it, and it still didn't work?
                            throw new UnableToStartServiceException("Couldn't start the service in any manner.");
                        }
                    }
                }
            }
        }

        private static void InstallAndStartService() {
            // basically, we just need to find *any* coapp.service.exe and tell it to --auto-install
            // and it will do a better search than we could do anyway.
            var coAppRootDirectory = PackageManagerSettings.CoAppRootDirectory;
            var serviceExes = coAppRootDirectory.FindFilesSmarter(@"**\coapp.service.exe");
            if (!serviceExes.IsNullOrEmpty()) {
                foreach (var path in serviceExes) {
                    var processStartInfo = new ProcessStartInfo(path) {
                        Arguments = "--auto-install",
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    var process = Process.Start(processStartInfo);
                    process.WaitForExit();

                    if( IsServiceInstalled) {
                        // AWESOME
                        TryToStartService(); // make sure it is started too.
                        return;
                    }
                    
                }
            }
            // uh, if we got here, we're not going to be able start Coapp...
        }


        private static void TryToRunServiceInteractively() {
            var path = Path.GetDirectoryName((Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).Location);
            var file = Path.Combine(path, "coapp.service.exe");

            if (!File.Exists(file)) {
                file = Path.Combine(Environment.CurrentDirectory, "coapp.service.exe");
                if (!File.Exists(file)) {
                    throw new FileNotFoundException("Can't find CoApp Service EXE");
                }
            }
            Logger.Warning("==[Starting Service '" + file + "' Interactively]==");
            var process = Process.Start(file, "--interactive");
            Thread.Sleep(500);
            var isRunning = IsEngineResponding;
        }

    }
}