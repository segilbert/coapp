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
    using System.IO.Pipes;
    using System.Linq;
    using System.Security.Principal;
    using System.ServiceProcess;
    using System.Threading;
    using Exceptions;
    using Extensions;
    using TimeoutException = System.ServiceProcess.TimeoutException;

    public static class EngineServiceManager {
        public const string CoAppServiceName = "CoApp Package Installer Service";

        public static bool IsServiceInstalled {
            get { return ServiceController.GetServices().Any(service => service.ServiceName == CoAppServiceName); }
        }

        private static readonly Lazy<ServiceController> _controller = new Lazy<ServiceController>(() => new ServiceController(CoAppServiceName));

        public static bool IsServiceRunning {
            get { return IsServiceInstalled && _controller.Value.Status == ServiceControllerStatus.Running; }
        }

        public static void TryToStopService() {
            if (!IsServiceInstalled) {
                throw new UnableToStopServiceException("{0} is not installed".format(CoAppServiceName));
            }

            if (_controller.Value.Status != ServiceControllerStatus.Stopped && _controller.Value.CanStop) {
                _controller.Value.Stop();
            }
        }

        public static void TryToStartService(bool secondAttempt = false) {
            if (!IsServiceInstalled) {
                throw new UnableToStartServiceException("{0} is not installed".format(CoAppServiceName));
            }

            switch (_controller.Value.Status) {
                case ServiceControllerStatus.ContinuePending:
                    // wait for it to continue.
                    try {
                        _controller.Value.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 0, 0, 10));
                    }
                    catch (TimeoutException) {
                        throw new UnableToStartServiceException(
                            "Service is in the '{0}' state, and didn't respond before timing out.".format(_controller.Value.Status.ToString()));
                    }
                    if (_controller.Value.Status != ServiceControllerStatus.Running) {
                        if (secondAttempt) {
                            throw new UnableToStartServiceException(
                                "Service is in the '{0}' state, and was expected to be in the 'Running' state.".format(_controller.Value.Status.ToString()));
                        }
                        TryToStartService(true);
                    }
                    break;
                case ServiceControllerStatus.PausePending:
                    try {
                        _controller.Value.WaitForStatus(ServiceControllerStatus.Paused, new TimeSpan(0, 0, 0, 10));
                    }
                    catch (TimeoutException) {
                        throw new UnableToStartServiceException(
                            "Service is in the '{0}' state, and didn't respond before timing out.".format(_controller.Value.Status.ToString()));
                    }
                    if (_controller.Value.Status == ServiceControllerStatus.Paused) {
                        TryToStartService();
                    }
                    if (_controller.Value.Status != ServiceControllerStatus.Running) {
                        if (secondAttempt) {
                            throw new UnableToStartServiceException(
                                "Service is in the '{0}' state, and was expected to be in the 'Running' state.".format(_controller.Value.Status.ToString()));
                        }
                        TryToStartService(true);
                    }
                    break;
                case ServiceControllerStatus.Paused:
                    _controller.Value.Continue();
                    try {
                        _controller.Value.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 0, 0, 10));
                    }
                    catch (TimeoutException) {
                        if (secondAttempt) {
                            throw new UnableToStartServiceException(
                                "Service is in the '{0}' state, and didn't respond before timing out.".format(_controller.Value.Status.ToString()));
                        }
                        TryToStartService(true);
                    }
                    break;

                case ServiceControllerStatus.Running:
                    // duh!
                    break;

                case ServiceControllerStatus.StartPending:
                    try {
                        _controller.Value.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 0, 0, 10));
                    }
                    catch (TimeoutException) {
                        if (secondAttempt) {
                            throw new UnableToStartServiceException(
                                "Service is in the '{0}' state, and didn't respond before timing out.".format(_controller.Value.Status.ToString()));
                        }
                        TryToStartService(true);
                    }
                    break;

                case ServiceControllerStatus.StopPending:
                    try {
                        _controller.Value.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, 0, 0, 10));
                    }
                    catch (TimeoutException) {
                        throw new UnableToStartServiceException(
                            "Service is in the '{0}' state, and didn't respond before timing out.".format(_controller.Value.Status.ToString()));
                    }
                    if (_controller.Value.Status == ServiceControllerStatus.Stopped) {
                        TryToStartService();
                    }
                    if (_controller.Value.Status != ServiceControllerStatus.Running) {
                        if (secondAttempt) {
                            throw new UnableToStartServiceException(
                                "Service is in the '{0}' state, and was expected to be in the 'Running' state.".format(_controller.Value.Status.ToString()));
                        }
                        TryToStartService(true);
                    }
                    break;

                case ServiceControllerStatus.Stopped:
                    _controller.Value.Start();
                    try {
                        _controller.Value.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 0, 0, 10));
                    }
                    catch (TimeoutException) {
                        if (secondAttempt) {
                            throw new UnableToStartServiceException(
                                "Service is in the '{0}' state, and didn't respond before timing out.".format(_controller.Value.Status.ToString()));
                        }
                        TryToStartService(true);
                    }
                    break;
            }
        }

        public static bool IsServiceResponding {
            get {
                lock (typeof (EngineServiceManager)) {
                    if (IsServiceRunning | Process.GetProcessesByName("coapp.service").Any()) {
                        var testPipe = new NamedPipeClientStream(".", "CoAppInstaller", PipeDirection.InOut, PipeOptions.Asynchronous,
                            TokenImpersonationLevel.Impersonation);
                        try {
                            testPipe.Connect(100);
                            testPipe.Close();
                            testPipe.Dispose();
                        }
                        catch (TimeoutException) {
                            return false;
                        }
                        return true;
                    }
                    return false;
                }
            }
        }


        public static void EnsureServiceIsResponding() {
            lock (typeof(EngineServiceManager)) {
                if (IsServiceInstalled) {
                    if (IsServiceRunning) {
                        if (!IsServiceResponding) {
                            // service is installed & running, but not responding. 
                            // let the client deal with it.
                            throw new UnableToStartServiceException("Service is installed & running, but not responding to it's pipe.");
                        }
                        return; // it's running!
                    }

                    try {
                        // it's not running. try to make it go!
                        TryToStartService();
                    }
                    catch (UnableToStartServiceException) {
#if DEBUG
                        // wouldn't start. 
                        // in debug mode, let's try it interactively.
                        if (!IsServiceResponding) {
                            TryToRunServiceInteractively();
                        }
#else
    // wouldn't start. 
    // make it someone elses problem.
                        rethrow;
#endif
                    }
                    // assumably, it's running if we got this far.
                }
#if DEBUG
                // is the service responding yet?
                if (!IsServiceResponding) {
                    TryToRunServiceInteractively();
                }
#endif
                // is the service responding yet?
                if (!IsServiceResponding) {
                    throw new UnableToStartServiceException("Couldn't start the service in any manner.");
                }
            }
        }

#if DEBUG
        private static void TryToRunServiceInteractively() {
            if (!IsServiceRunning | !Process.GetProcessesByName("coapp.service").Any()) {
                Process.Start("coapp.service.exe", "--interactive");
                for (var i = 0; i < 10 && !IsServiceResponding; i++) {
                    Thread.Sleep(300); // give it a chance to startup.
                }
            }
        }
#endif
    }
}