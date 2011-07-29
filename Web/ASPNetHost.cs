//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Web {
    using System;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Web;
    using System.Web.Hosting;
    using Extensions;

    public class WorkerRequest : SimpleWorkerRequest {
        public object ParameterData;

        public WorkerRequest(string page, string queryString, TextWriter output) : base(page, queryString, output) {
        }

        public WorkerRequest(string page, string queryString, TextWriter output, object o) : base(page, queryString, output) {
            ParameterData = o;
        }

        public override void SetEndOfSendNotification(EndOfSendNotification callback, object extraData) {
            base.SetEndOfSendNotification(callback, extraData);
            if(ParameterData != null) {
                var context = extraData as HttpContext;
                if(context != null) {
                    context.Items.Add("Content", ParameterData);
                }
            }
        }
    }

    public class SmallAspNetHost : MarshalByRefObject {
        public void ProcessRequest(string page) {
            HttpRuntime.ProcessRequest(new WorkerRequest(page, null, Console.Out));
        }

        public void ProcessRequest(string page, object o) {
            HttpRuntime.ProcessRequest(new WorkerRequest(page, null, Console.Out, o));
        }

        public AppDomain GetAppDomain() {
            return Thread.GetDomain();
        }
    }

    // ‹› 0x8b 0x9b 
    // «» 0xab 0xbb (alt-174, alt-175)
    public class AspNetRuntime : MarshalByRefObject {
        public string RootFolder { get; private set; }
        private string binFolder;

        public AspNetRuntime(string tmpPathName) {
            RootFolder = Path.Combine(Path.GetTempPath(), tmpPathName);
            binFolder = Path.Combine(RootFolder, "bin");

            if(!Directory.Exists(RootFolder)) {
                Directory.CreateDirectory(RootFolder);
            }

            if(!Directory.Exists(binFolder)) {
                Directory.CreateDirectory(binFolder);
            }

            AddAssembly(this.Assembly());
            AddAssembly(Assembly.GetExecutingAssembly());
        }

        private void AddAssembly(Assembly asmbly) {
            var destfile = Path.Combine(binFolder, Path.GetFileName(asmbly.Location));
            File.Copy(asmbly.Location, destfile, true);
        }

        private void HostedDomainHasBeenUnloaded(object source, EventArgs e) {
            aspNetHostIsUnloaded.Set();
        }

        private ManualResetEvent aspNetHostIsUnloaded;

        public void Run(string[] pages) {
            SmallAspNetHost host = null;

            try {
                host = (SmallAspNetHost) ApplicationHost.CreateApplicationHost(typeof(SmallAspNetHost), "/", RootFolder);

                aspNetHostIsUnloaded = new ManualResetEvent(false);

                host.GetAppDomain().DomainUnload += HostedDomainHasBeenUnloaded;

                foreach(string page in pages) {
                    var newPage = Path.Combine(RootFolder, Path.GetFileName(page));
                    var txt = File.ReadAllText(page, Encoding.UTF8);

                    txt = txt.Replace("‹", "<%=").Replace("›", "%>").Replace("«", "<%").Replace("»", "%>");

                    if(!new Regex(@"\<\%.*@Page.*Language.*\%\>").IsMatch(txt)) {
                        txt = @"<%@ Page Language=""c#"" %>" + txt;
                    }

                    File.WriteAllText(newPage, txt, Encoding.UTF8);

                    host.ProcessRequest(Path.GetFileName(page));
                }
            }
            finally {
                // tell the host to unload
                if(host != null) {
                    AppDomain.Unload(host.GetAppDomain());

                    // wait for it to unload
                    aspNetHostIsUnloaded.WaitOne();

                    Directory.Delete(RootFolder, true);
                }
            }
        }
    }
}