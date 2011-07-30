//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine {
    using System.IO.Pipes;
    using System.Threading;
    using System.Threading.Tasks;
    using Tasks;

    public static class EngineService {
        private const string PipeName = @"CoAppInstaller";
        private const int Instances = 16;
        private const int BufferSize = 8192;

        private static Task _engineTask;
        public static bool IsRunning;
        private static NamedPipeServerStream _serverPipe; 

        public static void Start() {
            // this should spin up a task and start listening for commands
            if (IsRunning) {
                return;
            }
            IsRunning = true;
            _engineTask = CoTask.Factory.StartNew(() => {
                CreateStream();
                
                while (_serverPipe != null && IsRunning ) {
                    
                    
                    _serverPipe.WaitForConnection();


                    Thread.Sleep(1000);
                }
            });
        }

        public static void Stop() {
            // this should stop the task
            IsRunning = false;
        }

        private static void CreateStream() {
             _serverPipe = new NamedPipeServerStream(PipeName, PipeDirection.InOut, Instances, PipeTransmissionMode.Message, PipeOptions.Asynchronous, BufferSize, BufferSize);
        }
    }
}