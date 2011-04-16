//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Win32 {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Exceptions;
    using Extensions;

    public class LegacySymlink : ISymlink {
        private const string legacySymlinkInfo = "legacySymlinkInfo";
        private const string linkedFile = "linkedfile:";
        private const string originalFile = "originalfile:";
        private static readonly int linkedFileLength = linkedFile.Length;
        private static readonly int originalFileLength = originalFile.Length;

        public void MakeFileLink(string linkPath, string actualFilePath) {
            linkPath = linkPath.GetFullPath();
            actualFilePath = GetActualPath(actualFilePath.GetFullPath());

            if (!File.Exists(actualFilePath)) {
                throw new FileNotFoundException("Cannot link to non-existent file", actualFilePath);
            }
            if (Directory.Exists(linkPath)) {
                throw new ConflictingFileOrFolderException(linkPath);
            }

            if (File.Exists(linkPath) && IsSymlink(linkPath)) {
                ChangeLinkTarget(linkPath, actualFilePath);
                return;
            }

            if( File.Exists(linkPath) ) {
                linkPath.TryHardToDeleteFile();
            }

            Kernel32.CreateHardLink(linkPath, actualFilePath, IntPtr.Zero);
            AddSymlinkToAlternateStream(actualFilePath, linkPath);
        }

        public void MakeDirectoryLink(string linkPath, string actualFolderPath) {
            linkPath = linkPath.GetFullPath();
            actualFolderPath = GetActualPath(actualFolderPath.GetFullPath());

            if (!Directory.Exists(actualFolderPath)) {
                throw new FileNotFoundException("Cannot link to non-existent directory", actualFolderPath);
            }

            if (Directory.Exists(linkPath) && IsSymlink(linkPath)) {
                ReparsePoint.ChangeReparsePointTarget(linkPath, actualFolderPath);
                return;
            }

            if (File.Exists(linkPath) || Directory.Exists(linkPath)) {
                throw new ConflictingFileOrFolderException(linkPath);
            }

            ReparsePoint.CreateJunction(linkPath, actualFolderPath);
        }

        public void ChangeLinkTarget(string linkPath, string newActualPath) {
            linkPath = linkPath.GetFullPath();
            newActualPath = GetActualPath(newActualPath.GetFullPath());
            var oldActualPath = GetActualPath(linkPath);
            if (oldActualPath.Equals(newActualPath, StringComparison.CurrentCultureIgnoreCase)) {
                return;
            }

            if (!IsSymlink(linkPath)) {
                throw new PathIsNotSymlinkException(linkPath);
            }

            if (Directory.Exists(linkPath)) {
                ReparsePoint.ChangeReparsePointTarget(linkPath, newActualPath);
            }
            else if( File.Exists(linkPath)) {
                DeleteSymlink(linkPath);
                MakeFileLink(linkPath, newActualPath);
            }
            else {
                throw new DirectoryNotFoundException("Target directory not found");
            }
        }

        public void DeleteSymlink(string linkPath) {
            linkPath = linkPath.GetFullPath();
            if (!File.Exists(linkPath) && !Directory.Exists(linkPath)) {
                return;
            }

            if (IsSymlink(linkPath)) {
                if (File.Exists(linkPath)) {
                    string canonicalFilePath;
                    var alternates = GetAlternateStreamData(linkPath, out canonicalFilePath).ToList();
                    linkPath.TryHardToDeleteFile();
                    SetAlternateStreamData(canonicalFilePath, alternates);
                }
                else if (Directory.Exists(linkPath)) {
                    linkPath.TryHardToDeleteDirectory();
                }
            } else {
                throw new IOException("Path is not a link.");
            }
        }

        public bool IsSymlink(string linkPath) {
            linkPath = linkPath.GetFullPath();
            if (File.Exists(linkPath)) {
                if (GetFileInfo(linkPath).NumberOfLinks > 1) {
                    string canonicalFilePath;
                    var alternates = GetAlternateStreamData(linkPath, out canonicalFilePath);
                    var result = !linkPath.Equals(canonicalFilePath, StringComparison.CurrentCultureIgnoreCase);
                    try {
                        if (result && !alternates.Contains(linkPath)) {
                            AddSymlinkToAlternateStream(canonicalFilePath, linkPath);
                        }
                    } catch {
                        // just trying to clean-as-we-go
                    }

                    return result;
                }
                try {
                    var s = new FileInfo(linkPath).GetAlternateDataStream(legacySymlinkInfo);
                    if (s.Exists)
                        s.Delete();
                } catch {
                    // just try to clean as we go...
                }
                return false;
            }
            if (Directory.Exists(linkPath)) {
                return ReparsePoint.IsReparsePoint(linkPath);
            }
            throw new FileNotFoundException("Link does not exist");
        }

        public string GetActualPath(string linkPath) {
            linkPath = linkPath.GetFullPath();
            if(File.Exists(linkPath)) {
                string result;
                GetAlternateStreamData(linkPath, out result);
                return result;
            } 
            if( Directory.Exists(linkPath)) {
                return ReparsePoint.GetActualPath(linkPath);
            }
            return linkPath;
        }


        private static void AddSymlinkToAlternateStream( string originalPath, string filename ) {
            string canonicalFilePath;
            var alternates = GetAlternateStreamData(originalPath, out canonicalFilePath).ToList();
            alternates.Add(filename);
            SetAlternateStreamData(canonicalFilePath, alternates);
        }

        private static void SetAlternateStreamData(string filename, IEnumerable<string> linkPaths) {
            filename = filename.GetFullPath();;
            linkPaths = linkPaths.Uniq(StringComparison.CurrentCultureIgnoreCase);

            if(!File.Exists(filename) )
                throw new FileNotFoundException("Can not set alternate stream info on non-existent file.");
            
            var fileInformation = GetFileInfo(filename);

            if (fileInformation.NumberOfLinks > 1) {
                var s = new FileInfo(filename).GetAlternateDataStream(legacySymlinkInfo);
                s.Delete();
                using (var fstream = s.OpenWrite()) {
                    fstream.Write("{0}{1}\r\n".format(originalFile, filename).ToByteArray());
                    foreach (var path in linkPaths) {
                        var linkFileInformation = GetFileInfo(filename);
                        if (linkFileInformation.FileIndex == fileInformation.FileIndex) {
                            fstream.Write("{0}{1}\r\n".format(linkedFile, path.GetFullPath()).ToByteArray());
                        }
                    }
                    fstream.Close();
                }
            } else {
                var s = new FileInfo(filename).GetAlternateDataStream(legacySymlinkInfo);
                if (s.Exists)
                    s.Delete();
            }
        }

        private static IEnumerable<string> GetAlternateStreamData(string filename, out string canonicalFilePath) {
            filename = filename.GetFullPath();;

            if (!File.Exists(filename))
                throw new FileNotFoundException("Can not get alternate stream info from non-existent file.");
            var fileInformation = GetFileInfo(filename);

            if (fileInformation.NumberOfLinks > 1) {
                var s = new FileInfo(filename).GetAlternateDataStream(legacySymlinkInfo);
                if (s.Exists) {
                    using (var fstream = s.OpenRead()) {
                        var buffer = new byte[s.Size];
                        fstream.Read(buffer, 0, buffer.Length);
                        var lines = buffer.ToUtf8String().Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                        var origFiles =
                            (from line in lines where line.StartsWith(originalFile) select line.Substring(originalFileLength)).Reverse();
                        canonicalFilePath = (from entry in origFiles where File.Exists(entry) select entry).FirstOrDefault() ?? filename;
                        var canonicalFileInfo = GetFileInfo(canonicalFilePath);

                        if (fileInformation.FileIndex != canonicalFileInfo.FileIndex) {
                            // if the canonical path isn't actually linked with this one
                            // then this is it's own canonical file.
                            canonicalFileInfo = fileInformation;
                            canonicalFilePath = filename;
                        }

                        // I need this in a local variable again so I can use it in the linq query below.
                        filename = canonicalFilePath;

                        var result = from line in lines where line.StartsWith(linkedFile) select line.Substring(linkedFileLength);
                        result = from file in result where File.Exists(file) && GetFileInfo(file).FileIndex == canonicalFileInfo.FileIndex && !file.Equals(filename, StringComparison.CurrentCultureIgnoreCase) select file;

                        return result;
                    }
                }
            }
            canonicalFilePath = filename;
            return Enumerable.Empty<string>();
        }

        public void ScanFolder(string path) {
            // just scan thru a folder and check IsSymlink on each file
            // this will automatically rebuild the data.
            // ideally, start with a folder considered to be canonical 
            // (in the event that all the symlink data is missing)
            var files = path.DirectoryEnumerateFilesSmarter("*", SearchOption.AllDirectories);
            foreach (var f in files)
                IsSymlink(f);
        }

        private static ByHandleFileInformation GetFileInfo(string filename) {
            filename = filename.GetFullPath();;
            
            if (!File.Exists(filename))
                throw new FileNotFoundException("Can not get file information for non-existent file.");

            ByHandleFileInformation result;
            using ( var fileHandle = Kernel32.CreateFile(filename, NativeFileAccess.GenericRead, FileShare.Read | FileShare.Write | FileShare.Delete , IntPtr.Zero, FileMode.Open,NativeFileAttributesAndFlags.Normal, IntPtr.Zero) ) {
                Kernel32.GetFileInformationByHandle(fileHandle.DangerousGetHandle(), out result);
            }
            return result;
        }
    }
}