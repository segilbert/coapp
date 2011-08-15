//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
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

namespace CoApp.Toolkit.Network {
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;
    using Extensions;
    using Tasks;
    using Console = System.Console;

    public class HttpServer {
        private readonly string _host;
        private readonly int _port;
        private readonly HttpListener _listener = new HttpListener();
        private readonly Dictionary<string, string> _virtualDirs = new Dictionary<string, string>();

        public HttpServer(string host = "*", int port = 80 ) {
            _host = host.ToLower();
            _port = port;
        }

        public void AddVirtualDir(string prefix,string localPath) {
            if (string.IsNullOrEmpty(prefix))
                prefix = string.Empty;

            prefix = ("/" + prefix + "/");
            while( prefix.IndexOf("//") >-1) {
                prefix = prefix.Replace("//", "/").ToLower();
            }
            _virtualDirs.Add(prefix, localPath);

            var listenerPrefix = string.Format("http://{0}:{1}{2}", _host, _port, prefix);
            _listener.Prefixes.Add(listenerPrefix);
        }

        public string GetLocalPath(Uri requestUri) {
           var lp = requestUri.LocalPath.ToLower();
            return (from vdPrefix in (from k in _virtualDirs.Keys orderby k.Length descending select k)
                    let index = lp.IndexOf(vdPrefix)
                    where index == 0
                    let localPath = lp.Substring(vdPrefix.Length)
                    select Path.Combine(_virtualDirs[vdPrefix], localPath)).FirstOrDefault();
        }

        public string GetDirectoryListing(string directory) {
            if (Directory.Exists(directory)) {
                var b = new StringBuilder();
                var di = new DirectoryInfo(directory);
                b.Append("Directory Listing:<br/></hr><table><tr style='font-weight:bold;'><td>Name</td><td style='text-align: center'>Date</td><td style='text-align: center'>Size</td></tr>");
                foreach (DirectoryInfo d in di.GetDirectories())
                    b.Append(string.Format("<tr><td>[<a href='{0}'>{0}</a>]</td><td style='text-align: right'>{1}</td><td style='text-align: right'></td></tr>\r\n", d.Name, d.LastWriteTime.ToString()));
                foreach (FileInfo f in di.GetFiles())
                    b.Append(string.Format("<tr><td><a href='{0}'>{0}</a></td><td style='text-align: right'>{1}</td><td style='text-align: right'>{2}</td></tr>\r\n", f.Name, f.LastWriteTime.ToString(), f.Length));
                b.Append("</table>");

                return b.ToString();
            }
            return null;
        }

        public DateTime GetLocationLastModified( string location ) {
            return Directory.Exists(location) ? Directory.GetLastWriteTimeUtc(location)
                : File.Exists(location) ? File.GetLastWriteTimeUtc(location) : DateTime.Now;
        }

        private bool Exists(string location ) {
            if( string.IsNullOrEmpty(location))
                return false;
            return Directory.Exists(location) || File.Exists(location);
        }

        private long GetContentLength(string location) {
            if (Directory.Exists(location)) {
                return GetDirectoryListing(location).Length;
            } 
            var fi = new FileInfo(location);
            return fi.Length;
        }

        public void Start() {
            _listener.Start();

            Task.Factory.FromAsync<HttpListenerContext>(_listener.BeginGetContext, _listener.EndGetContext, _listener).ContinueWith(
                (antecedent) => {
                    Start(); // start a new listener.

                    try {
                        var request = antecedent.Result.Request;
                        var response = antecedent.Result.Response;
                        var lp = GetLocalPath(request.Url);

                        switch( request.HttpMethod ) {
                            case "HEAD":
                                if (Exists(lp)) {
                                    response.AddHeader("Last-Modified", GetLocationLastModified(lp).ToString("r"));
                                    response.ContentLength64 = GetContentLength(lp);
                                } else {
                                    response.StatusCode = (int)HttpStatusCode.NotFound;
                                }
                                response.Close();

                                break;
                            case "GET":
                                if (!Exists(lp)) {
                                    response.StatusCode = (int)HttpStatusCode.NotFound;
                                    response.Close();
                                    break;
                                }
                                response.AddHeader("Last-Modified", GetLocationLastModified(lp).ToString("r"));
                                response.ContentLength64 = GetContentLength(lp);
                                if( Directory.Exists(lp)) {
                                    response.ContentType = "text/html";
                                    var buf = GetDirectoryListing(lp).ToByteArray();
                                    response.OutputStream.Write(buf,0,buf.Length);
                                    response.OutputStream.Flush();
                                    response.Close();
                                    break;
                                }

                                var data = File.ReadAllBytes(lp);
                                response.OutputStream.Write(data, 0, data.Length);
                                response.Close();
                                break;
                            case "POST":

                                break;

                            default:
                                Console.WriteLine("Unknown HTTP VERB : {0}", request.HttpMethod );
                                break;
                        }
                    }
                    catch( Exception e) {
                        Console.WriteLine("HTTP Server Error: {0}",e.Message);
                    }
                }, TaskContinuationOptions.AttachedToParent);
        }

        public void Stop() {
            // _listener.Abort();
            _listener.Stop();
        }

    }
}
