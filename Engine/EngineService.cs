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
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Pipes;
    using System.Linq;
    using System.Security.AccessControl;
    using System.Security.Principal;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensions;
    using Tasks;
    using Win32;

    /// <summary>
    /// The actual service for CoApp.
    /// 
    /// 
    /// </summary>
    /// <remarks>
    /// NOTE: EXPLICITLY IGNORE, NOT READY FOR TESTING.
    /// </remarks>
    public class UrlEncodedMessage : IEnumerable<string> {
        /// <summary>
        /// 
        /// </summary>
        private static char[] query = new[] {
            '?'
        };

        /// <summary>
        /// 
        /// </summary>
        private static char[] separator = new[] {
            '&'
        };

        /// <summary>
        /// 
        /// </summary>
        private static char[] equals = new[] {
            '='
        };

        /// <summary>
        /// 
        /// </summary>
        internal string Command;
        /// <summary>
        /// 
        /// </summary>
        internal IDictionary<string, string> Data;

        /// <summary>
        /// Initializes a new instance of the <see cref="UrlEncodedMessage"/> class.
        /// </summary>
        /// <param name="rawMessage">The raw message.</param>
        /// <remarks></remarks>
        public UrlEncodedMessage(string rawMessage) {
            var parts = rawMessage.Split(query, StringSplitOptions.RemoveEmptyEntries);
            Command = parts.FirstOrDefault().UrlDecode().ToLower();
            Data = (parts.Skip(1).FirstOrDefault() ?? "").Split(separator, StringSplitOptions.RemoveEmptyEntries).Select(
                    p => p.Split(@equals, StringSplitOptions.RemoveEmptyEntries)).ToDictionary(s => s[0].UrlDecode(),
                        s => s.Length > 1 ? s[1].UrlDecode() : String.Empty);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UrlEncodedMessage"/> class.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="data">The data.</param>
        /// <remarks></remarks>
        public UrlEncodedMessage( string command, IDictionary<string, string> data ) {
            Command = command;
            Data = data;
        }

        /// <summary>
        /// Gets or sets the <see cref="System.String"/> with the specified key.
        /// </summary>
        /// <remarks></remarks>
        public string this[string key] {
            get { return Data.ContainsKey(key) ? Data[key] : String.Empty; }
            set {
                if (Data.ContainsKey(key)) {
                    Data[key] = value;
                }
                else {
                    Data.Add(key, value);
                }
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        /// <remarks></remarks>
        public override string ToString() {
            return Data.Any()
                ? Data.Keys.Aggregate(Command.UrlEncode().ToLower() + "?", (current, k) => current + (k.UrlEncode() + "=" + Data[k].UrlEncode()))
                : Command.UrlEncode();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.</returns>
        /// <remarks></remarks>
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        /// <summary>
        /// Adds the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <remarks></remarks>
        public void Add(string key, string value) {
            this[key] = value;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.</returns>
        /// <remarks></remarks>
        public IEnumerator<string> GetEnumerator() {
            return Data.Keys.GetEnumerator();
        }

        /// <summary>
        /// Gets the collection.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        internal IEnumerable<string> GetCollection(string p) {
            var rx = new Regex(@"${0}\[.\n]\]^".format(Regex.Escape(p)));
            foreach( var k in Data.Keys ) {
                if( rx.IsMatch(k)) {
                    yield return Data[k];
                }
            }
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks></remarks>
    public static class AsyncPipeExtensions {
        /// <summary>
        /// Read from a stream asynchronously.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="buffer">An array of bytes to be filled by the read operation.</param>
        /// <param name="offset">The offset at which data should be stored.</param>
        /// <param name="count">The number of bytes to be read.</param>
        /// <returns>A Task containing the number of bytes read.</returns>
        /// <remarks></remarks>
        public static Task<int> ReadAsync(this Stream stream, byte[] buffer, int offset, int count) {
            if (stream == null) {
                throw new ArgumentNullException("stream");
            }
            return CoTask<int>.Factory.FromAsync(stream.BeginRead, stream.EndRead, buffer, offset, count, stream /* object state */);
        }

        /// <summary>
        /// Write to a stream asynchronously.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="buffer">An array of bytes to be written.</param>
        /// <param name="offset">The offset from which data should be read to be written.</param>
        /// <param name="count">The number of bytes to be written.</param>
        /// <returns>A Task representing the completion of the asynchronous operation.</returns>
        /// <remarks></remarks>
        public static Task WriteAsync(this Stream stream, byte[] buffer, int offset, int count) {
            if (stream == null) {
                throw new ArgumentNullException("stream");
            }
            return CoTask.Factory.FromAsync(stream.BeginWrite, stream.EndWrite, buffer, offset, count, stream);
        }

        /// <summary>
        /// Writes the line async.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="message">The message.</param>
        /// <param name="objs">The objs.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static Task WriteLineAsync(this Stream stream, string message, params object[] objs) {
            var bytes = (message.format(objs).Trim() + "\r\n").ToByteArray();
            return stream.WriteAsync(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Writes the async.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static Task WriteAsync(this Stream stream, UrlEncodedMessage request) {
            return stream.WriteLineAsync(request.ToString());
        }

        /// <summary>
        /// Writes the async.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="message">The message.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static Task WriteAsync( this Stream stream, string message, IDictionary<string,string> parameters) {
            return stream.WriteAsync(new UrlEncodedMessage(message, parameters));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks></remarks>
    public class EngineService {
        /// <summary>
        /// 
        /// </summary>
        private const string PipeName = @"CoAppInstaller";
        /// <summary>
        /// 
        /// </summary>
        private const string OutputPipeName = @"CoAppInstaller-";

        /// <summary>
        /// 
        /// </summary>
        private const int Instances = -1;
        /// <summary>
        /// 
        /// </summary>
        internal const int BufferSize = 8192;
        /// <summary>
        /// 
        /// </summary>
        private static readonly Lazy<EngineService> Instance = new Lazy<EngineService>(() => new EngineService());

        /// <summary>
        /// 
        /// </summary>
        private bool _isRunning;
        /// <summary>
        /// 
        /// </summary>
        private Task _engineTask;

        /// <summary>
        /// 
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        /// <summary>
        /// 
        /// </summary>
        private PipeSecurity _pipeSecurity;

        /// <summary>
        /// 
        /// </summary>
        private List<MessagePump> _activeMessagePumps = new List<MessagePump>();

        /// <summary>
        /// Stops this instance.
        /// </summary>
        /// <remarks></remarks>
        public static void Stop() {
            // this should stop the task
            Instance.Value._cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        /// <remarks></remarks>
        public static void Start() {
            // this should spin up a task and start listening for commands
            Instance.Value.Main();
        }

        /// <summary>
        /// Gets a value indicating whether this instance is running.
        /// </summary>
        /// <remarks></remarks>
        public static bool IsRunning {
            get { return Instance.Value._isRunning; }
        }

        /// <summary>
        /// Mains this instance.
        /// </summary>
        /// <remarks></remarks>
        private void Main() {
            if (_isRunning) {
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _isRunning = true;

            try {
                _engineTask = CoTask.Factory.StartNew(() => {
                    try {
                        _pipeSecurity = new PipeSecurity();
                        _pipeSecurity.AddAccessRule(new PipeAccessRule("Everyone", PipeAccessRights.ReadWrite, AccessControlType.Allow));
                        _pipeSecurity.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().Owner, PipeAccessRights.FullControl, AccessControlType.Allow));

                        // start two listeners by default--each listener will also spawn a new empty one.
                        StartListener();
                        StartListener();

                        while (!Tasklet.IsCancellationRequested) {
                            try {
                                Tasklet.WaitforCurrentChildTasks();
                            }
                            catch (Exception e) {
                                Console.WriteLine(e.GetType());
                                Console.WriteLine(e.Message);
                                Console.WriteLine(e.StackTrace);
                            }
                        }
                    }
                    finally {
                        Console.WriteLine("[Done.========================]");
                        _isRunning = false;
                    }
                }, _cancellationTokenSource.Token);
            }
            catch (Exception ex) {
                Console.Write(ex.GetType());
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }


        /// <summary>
        /// Starts the listener.
        /// </summary>
        /// <remarks></remarks>
        private void StartListener() {
            try {
                if (_isRunning) {
                    Console.WriteLine("Starting new listener");
                    var serverPipe = new NamedPipeServerStream(PipeName, PipeDirection.InOut, Instances, PipeTransmissionMode.Message, PipeOptions.Asynchronous,
                        BufferSize, BufferSize, _pipeSecurity);
                    CoTask.Factory.FromAsync(serverPipe.BeginWaitForConnection, serverPipe.EndWaitForConnection, serverPipe).ContinueWithParent(t => {
                        StartListener(); // start next one!

                        if (serverPipe.IsConnected) {
                            var serverInput = new byte[BufferSize];

                            serverPipe.ReadAsync(serverInput, 0, serverInput.Length).ContinueWith(antecedent => {
                                var rawMessage = Encoding.UTF8.GetString(serverInput, 0, antecedent.Result);
                                if (string.IsNullOrEmpty(rawMessage)) {
                                    return;
                                }

                                var requestMessage = new UrlEncodedMessage(rawMessage);

                                // first command must be "startsession"
                                if (!requestMessage.Command.Equals("startsession", StringComparison.CurrentCultureIgnoreCase)) {
                                    return;
                                }

                                // check for the required parameters. 
                                // close the session if they are not here.
                                if (string.IsNullOrEmpty(requestMessage["id"]) || string.IsNullOrEmpty(requestMessage.Data["client"])) {
                                    return;
                                }

                                if (requestMessage["async"].Equals("false", StringComparison.CurrentCultureIgnoreCase)) {
                                    Console.WriteLine("Starting Two-Pipe Client");
                                    StartResponsePipeAndProcessMesages(requestMessage.Data["client"], requestMessage["id"], serverPipe);
                                }
                                else {
                                    Console.WriteLine("Starting Async Client");
                                    _activeMessagePumps.Add(new MessagePump(requestMessage.Data["client"], requestMessage["id"], serverPipe, serverPipe));
                                }
                            }).Wait();
                        }

                       
                    });
                }
            }
            catch (Exception e) {
                Console.Write(e.GetType());
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }


        /// <summary>
        /// Starts the response pipe and process mesages.
        /// </summary>
        /// <param name="clientId">The client id.</param>
        /// <param name="sessionId">The session id.</param>
        /// <param name="serverPipe">The server pipe.</param>
        /// <remarks></remarks>
        private void StartResponsePipeAndProcessMesages(string clientId, string sessionId, NamedPipeServerStream serverPipe) {
            try {
                var channelname = OutputPipeName + sessionId;
                var responsePipe = new NamedPipeServerStream(channelname, PipeDirection.Out, Instances, PipeTransmissionMode.Message, PipeOptions.Asynchronous,
                    BufferSize, BufferSize, _pipeSecurity);
                CoTask.Factory.FromAsync(responsePipe.BeginWaitForConnection, responsePipe.EndWaitForConnection, responsePipe).ContinueWithParent(t => {
                    if (responsePipe.IsConnected) {
                        _activeMessagePumps.Add(new MessagePump(clientId, sessionId, serverPipe, responsePipe));
                    }
                });
            }
            catch (Exception e) {
                Console.Write(e.GetType());
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks></remarks>
    public class MessagePump {
        /// <summary>
        /// 
        /// </summary>
        private string _clientId;
        /// <summary>
        /// 
        /// </summary>
        private readonly string _sessionId;
        /// <summary>
        /// 
        /// </summary>
        private readonly string _userId;
        /// <summary>
        /// 
        /// </summary>
        private readonly NamedPipeServerStream _serverPipe;
        /// <summary>
        /// 
        /// </summary>
        private readonly NamedPipeServerStream _responsePipe;

        /// <summary>
        /// 
        /// </summary>
        private readonly PackageManager _packageManager = new PackageManager();

        /// <summary>
        /// Starts the session.
        /// </summary>
        /// <param name="clientId">The client id.</param>
        /// <param name="sessionId">The session id.</param>
        /// <param name="serverPipe">The server pipe.</param>
        /// <param name="responsePipe">The response pipe.</param>
        /// <remarks></remarks>
        public void StartSession(string clientId, string sessionId, NamedPipeServerStream serverPipe, NamedPipeServerStream responsePipe ) {
            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePump"/> class.
        /// </summary>
        /// <param name="clientId">The client id.</param>
        /// <param name="sessionId">The session id.</param>
        /// <param name="serverPipe">The server pipe.</param>
        /// <param name="responsePipe">The response pipe.</param>
        /// <remarks></remarks>
        protected MessagePump(string clientId, string sessionId, NamedPipeServerStream serverPipe, NamedPipeServerStream responsePipe ) {
            _clientId = clientId;
            _sessionId = sessionId;
            _serverPipe = serverPipe;
            _responsePipe = responsePipe;

            CoTask.Factory.StartNew(ProcessMesages).ContinueWithParent( (antecedent) => {
                // remove from collection in engine?
                Console.WriteLine("Client Has Left [{0}]-[{1}]", _clientId, _sessionId);
             
                GC.Collect();
            });
        }

        /// <summary>
        /// Processes the mesages.
        /// </summary>
        /// <remarks></remarks>
        private void ProcessMesages() {
            using (_serverPipe) {
                using (_responsePipe) {
                    Task readTask = null;

                    _serverPipe.RunAsClient(() => {
                        Console.WriteLine("impersonated identity: {0}", WindowsIdentity.GetCurrent().Name);
                        try {
                            Console.WriteLine("IsAdmin: {0}", AdminPrivilege.IsProcessElevated());
                        }
                        catch (Exception e) {
                            Console.WriteLine(e.Message);
                            Console.WriteLine(e.StackTrace);
                        }
                    });

                    _responsePipe.WriteAsync(new UrlEncodedMessage("OK") { { "id", _sessionId } });

                    while (EngineService.IsRunning && _serverPipe.IsConnected && _responsePipe.IsConnected) {
                        Console.WriteLine("In Loop");

                        try {
                            // if there is currently a task reading the from the stream, let's skip it this time.
                            if (readTask == null || readTask.IsCompleted) {
                                var serverInput = new byte[EngineService.BufferSize];
                                readTask = _serverPipe.ReadAsync(serverInput, 0, serverInput.Length).ContinueWith(antecedent => {
                                    var rawMessage = Encoding.UTF8.GetString(serverInput, 0, antecedent.Result);

                                    if (string.IsNullOrEmpty(rawMessage)) {
                                        return;
                                    }

                                    Dispatch( new UrlEncodedMessage(rawMessage) );

                                });
                            }

                            readTask.Wait(650);

                            if (_serverPipe != _responsePipe) {
                                
                                try {
                                    _responsePipe.WriteAsync(new UrlEncodedMessage("Zzzz"));
                                }
                                catch (Exception e) {
                                    Console.Write(e.GetType());
                                    Console.WriteLine(e.Message);
                                    Console.WriteLine(e.StackTrace);
                                }
                            }
                            // message
                        }
                        catch (Exception e) {
                            Console.Write(e.GetType());
                            Console.WriteLine(e.Message);
                            Console.WriteLine(e.StackTrace);
                        }
                    }
                    Console.WriteLine("-2");
                }
                Console.WriteLine("-1");
            }
            Console.WriteLine("We're leaving ProcsssMessages");
        }

        /// <summary>
        /// Dispatches the specified request message.
        /// </summary>
        /// <param name="requestMessage">The request message.</param>
        /// <remarks></remarks>
        private void Dispatch(UrlEncodedMessage requestMessage) {
            switch (requestMessage.Command) {
                case "removepackages":
                    // get the package names collection and run the command
                    RemovePackages(requestMessage.GetCollection("packageNames"));
                    break;
                
                default:
                    // not recognized command, return error code.
                    _responsePipe.WriteAsync(new UrlEncodedMessage("unknowncommand") { { "command", requestMessage.Command } });
                    break;
            }
        }

        // example of a function that we publicize
        /// <summary>
        /// Waits the specified duration.
        /// </summary>
        /// <param name="duration">The duration.</param>
        /// <remarks></remarks>
        private void Wait(int duration) {
            Thread.Sleep(duration);
            _responsePipe.WriteAsync(new UrlEncodedMessage("Waited") {{ "duration", duration.ToString()}});
        }

        // example of a function that we publicize
        /// <summary>
        /// Removes the packages.
        /// </summary>
        /// <param name="packageNames">The package names.</param>
        /// <remarks></remarks>
        private void RemovePackages(IEnumerable<string> packageNames) {
            _packageManager.RemovePackages(packageNames, new PackageManagerMessages {
                RemovingPackage = (package) => {
                    Console.Write("\r\nRemoving: {0}", package.CosmeticName);
                    _responsePipe.WriteAsync(new UrlEncodedMessage("removingpackage") { { "cosmeticname", package.CosmeticName } });
                },
            });
            
        }
    }
}