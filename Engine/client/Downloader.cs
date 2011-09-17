using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Toolkit.Engine.Client {
    using System.IO;
    using System.Threading.Tasks;
    using Extensions;
    using Network;

    public class Downloader {
        private static readonly Dictionary<string, Task> _currentDownloads = new Dictionary<string, Task>();

        public static Task GetRemoteFile(string canonicalName, IEnumerable<string> locations, string targetFolder, bool forceDownload, RemoteFileMessages remoteFileMessages = null, PackageManagerMessages messages = null) {
            if ( messages == null ) {
                messages = new PackageManagerMessages();
            }
            var targetFilename = Path.Combine(targetFolder, canonicalName);
            lock (_currentDownloads) {
                if (_currentDownloads.ContainsKey(targetFilename)) {
                    return _currentDownloads[targetFilename];
                }

                if (File.Exists(targetFilename)) {
                    if (forceDownload) {
                        targetFilename.TryHardToDeleteFile();
                    }
                    else {
                        PackageManager.Instance.RecognizeFile(canonicalName, targetFilename, "<file exists>",
                            new PackageManagerMessages().Extend(messages));
                        return null;
                    }
                }

                // gotta download the file...
                var task = Task.Factory.StartNew(() => {
                    foreach (var location in locations) {
                        try {
                            var uri = new Uri(location);
                            if (uri.IsFile) {
                                // try to copy the file local.
                                var remoteFile = uri.AbsoluteUri.CanonicalizePath();

                                // if this fails, we'll just move down the line.
                                File.Copy(remoteFile, targetFilename);
                                PackageManager.Instance.RecognizeFile(canonicalName, targetFilename, uri.AbsoluteUri,
                                    new PackageManagerMessages().Extend(messages));
                                return;
                            }

                            var rf = RemoteFile.GetRemoteFile(uri, targetFilename);
                            rf.Get(new RemoteFileMessages {
                                Completed = (itemUri) => {
                                   PackageManager.Instance.RecognizeFile(canonicalName, targetFilename, uri.AbsoluteUri, new PackageManagerMessages().Extend(messages)); 
                                    if( remoteFileMessages != null ) {
                                        remoteFileMessages.Completed(itemUri);
                                    }
                                },
                                Failed = (itemUri) => {
                                    if (File.Exists(targetFilename)) {
                                        targetFilename.TryHardToDeleteFile();
                                        if( remoteFileMessages != null ) {
                                            remoteFileMessages.Failed(itemUri);
                                        }
                                    }
                                },
                                Progress = (itemUri, percent) => {
                                    PackageManager.Instance.DownloadProgress(canonicalName, percent);
                                    if( remoteFileMessages != null ) {
                                        remoteFileMessages.Progress(itemUri, percent);
                                    }
                                }
                            }).Wait();
                            
                            if (File.Exists(targetFilename)) {
                                return;
                            }
                        }
                        catch {
                            // bogus, dude.
                            // try the next one.
                        }
                    }

                    PackageManager.Instance.UnableToAcquire(canonicalName, new PackageManagerMessages());

                }, TaskCreationOptions.AttachedToParent).ContinueWith(antecedent => {
                    lock (_currentDownloads) {
                        _currentDownloads.Remove(targetFilename);
                    }
                }, TaskContinuationOptions.AttachedToParent);

                _currentDownloads.Add(targetFilename, task);
                return task;
            }
        }
    }
}
