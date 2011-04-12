//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Original Copyright (c) 2009 Microsoft Corporation. All rights reserved.
//     Changes Copyright (c) 2011 Eric Schultz, 2010  Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

// -----------------------------------------------------------------------
// Original Code: 
// (c) 2009 Microsoft Corporation -- All rights reserved
// This code is licensed under the MS-PL
// http://www.opensource.org/licenses/ms-pl.html
// Courtesy of the Open Source Techology Center: http://port25.technet.com
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.Specialized;

namespace CoApp.Toolkit.Extensions {
    using System;
    using System.IO;
    using System.Linq;
    using Win32;

    public static class FilesystemExtensions {
        private static int counter;

        /// <summary>
        ///   Determines if the childPath is a sub path of the rootPath
        /// </summary>
        /// <param name = "rootPath"></param>
        /// <param name = "childPath"></param>
        /// <returns></returns>
        public static bool IsSubPath(this string rootPath, string childPath) {
            return Path.GetFullPath(childPath).StartsWith(Path.GetFullPath(rootPath), StringComparison.CurrentCultureIgnoreCase);
        }

        ///<summary>
        ///  Gets the portion of the childPath that is a sub path of the parentPath
        ///
        ///  Returns string.Empty if the childPath is not a sub path of the parent.
        ///</summary>
        ///<param name = "parentPath"></param>
        ///<param name = "childPath"></param>
        ///<returns></returns>
        public static string GetSubPath(this string parentPath, string childPath) {
            var parent = Path.GetFullPath(parentPath);
            var child = Path.GetFullPath(childPath);
            if(child.StartsWith(parent, StringComparison.CurrentCultureIgnoreCase)) {
                return child.Substring(parent.Length).Trim('/', '\\');
            }
            return string.Empty;
        }

        /*
        /// <summary>
        ///   Returns the relative path from the fixedPath for the desiredPath
        /// </summary>
        /// <param name = "fixedPath"></param>
        /// <param name = "desiredPath"></param>
        /// <returns></returns>
        public static string GetRelativePath(this string fixedPath, string desiredPath) {
            var parent = Path.GetFullPath(fixedPath);
            var child = Path.GetFullPath(desiredPath);
            return null;
        }

        /// <summary>
        ///   Returns the absolute path of relative path, relative to assumedCurrentDirectory.
        /// </summary>
        /// <param name = "assumedCurrentDirectory"></param>
        /// <param name = "relativePath"></param>
        /// <returns></returns>
        public static string ResolveRelativePath(this string assumedCurrentDirectory, string relativePath) {

            return null;
        }
        */

       public static string ChangeFileExtensionTo(this string currentFilename, string newExtension) {
           return Path.Combine(Path.GetDirectoryName(currentFilename)??"", Path.GetFileNameWithoutExtension(currentFilename) + "." + newExtension);
       }

        public static string RelativePathTo(this string currentDirectory, string pathToMakeRelative) {
            if (string.IsNullOrEmpty(currentDirectory))
                throw new ArgumentNullException("currentDirectory");

            if (string.IsNullOrEmpty(pathToMakeRelative))
                throw new ArgumentNullException("pathToMakeRelative");

            currentDirectory = Path.GetFullPath(currentDirectory);
            pathToMakeRelative = Path.GetFullPath(pathToMakeRelative);

            if (!Path.GetPathRoot(currentDirectory).Equals(Path.GetPathRoot(pathToMakeRelative), StringComparison.CurrentCultureIgnoreCase))
                return pathToMakeRelative;

            var relativePath = new List<string>();
            var currentDirectoryElements = currentDirectory.Split(Path.DirectorySeparatorChar);
            var pathToMakeRelativeElements = pathToMakeRelative.Split(Path.DirectorySeparatorChar);
            var commonDirectories = 0;

            for (; commonDirectories < Math.Min(currentDirectoryElements.Length, pathToMakeRelativeElements.Length); commonDirectories++) {
                if (!currentDirectoryElements[commonDirectories].Equals(pathToMakeRelativeElements[commonDirectories], StringComparison.CurrentCultureIgnoreCase))
                    break;
            }

            for (var index = commonDirectories; index < currentDirectoryElements.Length; index++)
                if (currentDirectoryElements[index].Length > 0)
                    relativePath.Add("..");

            for (var index = commonDirectories; index < pathToMakeRelativeElements.Length; index++)
                relativePath.Add(pathToMakeRelativeElements[index]);

            return string.Join(Path.DirectorySeparatorChar.ToString(), relativePath);
        }
        public static string CanonicalizePath(this string filename, string filenameHint) {
            var result = filename;
            /*
                {filename}  - substitutes for the original
                              filename, no extension
                {ext}         original extension
                {folder}    - original folder
                {subfolder} - original folder without leading /
                {count}     - a running count of the files
                              downloaded.
                {date}      - the current date (y-m-d)
                {date-long} - the date in long format
                {time}      - the current time (Hh:mm:ss)
                {time-long} - the current time in long fmt
                {ticks}     - the current timestamp as tics

             *
            */
            if(!filenameHint.StartsWith("\\\\")) {
                if(filenameHint.StartsWith("/") || filenameHint.StartsWith("\\")) {
                    filenameHint = Environment.CurrentDirectory.Substring(0, 2) + filenameHint;
                }

                if(filenameHint.IndexOf(":") == -1) {
                    filenameHint = Path.Combine(Environment.CurrentDirectory, filenameHint);
                }
            }

            var uri = new Uri(filenameHint);
            filenameHint = uri.AbsolutePath;

            var localPath = uri.LocalPath.Replace("/", "\\");

            result = result.Replace(@"{filename}", Path.GetFileNameWithoutExtension(localPath));
            result = result.Replace(@"{ext}", Path.GetExtension(localPath));
            result = result.Replace(@"{folder}", Path.GetDirectoryName(localPath));
            var pr = Path.GetPathRoot(localPath);
            if(!string.IsNullOrEmpty(pr)) {
                result = result.Replace(@"{subfolder}", Path.GetDirectoryName(localPath).Remove(0, pr.Length));
            }

            result = result.Replace(@"{count}", "" + counter++);
            result = result.Replace(@"{date}", DateTime.Now.ToString("yyyy-MM-dd"));
            result = result.Replace(@"{date-long}", DateTime.Now.ToString("MMMM dd YYYY"));
            result = result.Replace(@"{time}", DateTime.Now.ToString("hh-mm-ss"));
            result = result.Replace(@"{time-long}", DateTime.Now.ToString("HH-mm-ss-ffff"));
            result = result.Replace(@"{ticks}", "" + DateTime.Now.Ticks);

            return result;
        }
        public static string FormatFilename(this string filename, params string[] parameters) {
            var result = filename;
            /*
                {date}      - the current date (y-m-d)
                {date-long} - the date in long format
                {time}      - the current time (Hh:mm:ss)
                {time-long} - the current time in long fmt
                {ticks}     - the current timestamp as tics
                {counter}   - a running counter
             *
            */
           
            result = result.Replace(@"{counter}", "" + counter++);
            result = result.Replace(@"{date}", DateTime.Now.ToString("yyyy-MM-dd"));
            result = result.Replace(@"{date-long}", DateTime.Now.ToString("MMMM dd YYYY"));
            result = result.Replace(@"{time}", DateTime.Now.ToString("hhmmss"));
            result = result.Replace(@"{time-long}", DateTime.Now.ToString("HHmmssffff"));
            result = result.Replace(@"{ticks}", "" + DateTime.Now.Ticks);

            return result.format(parameters);
        }


        public static IEnumerable<string> DirectoryEnumerateFilesSmarter(this string path, string searchPattern, SearchOption searchOption, IEnumerable<string> skipPathPatterns = null) {
            IEnumerable<string> result = Enumerable.Empty<string>();

            try {
                if (skipPathPatterns != null ? skipPathPatterns.Any(pattern => path.IsWildcardMatch(pattern)) : false)
                    return result;

                result = Directory.EnumerateFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
            }
            catch { }

            if (searchOption == SearchOption.AllDirectories) {
                try {
                    result = result.Union(Directory.EnumerateDirectories(path).Aggregate(result, (current, directory) => current.Union(DirectoryEnumerateFilesSmarter(directory, searchPattern, SearchOption.AllDirectories))));
                }
                catch { }
            }
            return result;
        }

        public static IEnumerable<string> FindFilesSmarter( this string pathMask ) {
            var path = Path.GetFullPath(pathMask.Replace("*", "$$STAR$$").Replace("?", "$$QUERY$$")).Replace("$$STAR$$","*").Replace( "$$QUERY$$", "?");
            var mask = path.Substring(path.LastIndexOf("\\") + 1);
            path = path.Substring(0, path.LastIndexOf("\\"));
            return path.DirectoryEnumerateFilesSmarter(mask, SearchOption.TopDirectoryOnly);
        }

        public static IEnumerable<string> FindFilesSmarter( this IEnumerable<string> pathMasks) {
            return pathMasks.Aggregate(Enumerable.Empty<string>(), (current, p) => current.Union(p.FindFilesSmarter()));
        }

        /// <summary>
        /// Gets the name of a file minus it's extension, ie: if the file name is "test.exe", returns "test".
        /// </summary>
        /// <param name="fi"></param>
        /// <returns></returns>
        public static string NameWithoutExt(this FileInfo fi)
        {
            return fi.Name.Remove(fi.Name.Length - fi.Extension.Length);
        }

        public static bool DirectoryExistsAndIsAccessible(this string path) {
            try {
                return Directory.Exists(path);
            }
            catch  {
                
            }
            return false;
        }

        public static void WriteAllBytesToFile(this MemoryStream ms, string path) {
            if (string.IsNullOrEmpty(path)) {
                throw new ArgumentNullException("path", "Invalid Path");
            }

            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read)) {
                stream.Write(ms.GetBuffer(), 0, (int)ms.Length);
            }
        }

        public static void ReadAllBytesFromFile(this MemoryStream ms, string path) {
            if (string.IsNullOrEmpty(path)) {
                throw new ArgumentNullException("path", "Invalid Path");
            }

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                ms.SetLength(stream.Length);
                stream.Read(ms.GetBuffer(), 0, (int)stream.Length);
            }
        }

        public static void TryHardToDeleteFile( this string filename ) {
            if (File.Exists(filename)) {
                try {
                    File.Delete(filename);
                }
                catch {
                    // didn't take, eh?
                }
            }

            if (File.Exists(filename)) {
                try {
                    // move the file to the tmp folder (which can be done even if locked)
                    // and tell the OS to remove it next reboot.
                    var tmpFilename = Path.GetTempFileName();
                    File.Delete(tmpFilename);
                    File.Move(filename, tmpFilename);
                    Kernel32.MoveFileEx(Directory.Exists(tmpFilename) ? tmpFilename : filename, null, MoveFileFlags.MOVEFILE_DELAY_UNTIL_REBOOT);
                }
                catch {
                    // really. Hmmm. 
                }
            }
        }

        public static void TryHardToDeleteDirectory(this string directoryName) {
            if (Directory.Exists(directoryName)) {
                try {
                    Directory.Delete(directoryName);
                }
                catch {
                    // didn't take, eh?
                }
            }

            if (File.Exists(directoryName)) {
                try {
                    // move the folder to the tmp folder (which can be done even if locked)
                    // and tell the OS to remove it next reboot.
                    var tmpFilename = Path.GetTempFileName();
                    File.Delete(tmpFilename);
                    Directory.Move(directoryName, tmpFilename);
                    Kernel32.MoveFileEx(Directory.Exists(tmpFilename) ? tmpFilename : directoryName, null,MoveFileFlags.MOVEFILE_DELAY_UNTIL_REBOOT);
                }
                catch {
                    // really. Hmmm. 
                }
            }
        }

        public static int Write(this FileStream fileStream, byte[] data) {
            fileStream.Write(data, 0, data.Length);
            return data.Length;
        }
    }
}