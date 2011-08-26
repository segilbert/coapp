//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Original Copyright (c) 2009 Microsoft Corporation. All rights reserved.
//     Changes Copyright (c) 2011 Eric Schultz, 2010  Garrett Serack. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

// -----------------------------------------------------------------------
// Original Code: 
// (c) 2009 Microsoft Corporation -- All rights reserved
// This code is licensed under the MS-PL
// http://www.opensource.org/licenses/ms-pl.html
// Courtesy of the Open Source Techology Center: http://port25.technet.com
// -----------------------------------------------------------------------

namespace CoApp.Toolkit.Extensions {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
#if ! COAPP_ENGINE_CORE 
    using Properties;
#endif
    using Exceptions;
    using Win32;

    /// <summary>
    /// Functions related to handling things regarding files and filesystems.
    /// </summary>
    /// <remarks></remarks>
    public static class FilesystemExtensions {
        /// <summary>
        /// a running counter of for funtions wanting to number files with increasing numbers.
        /// </summary>
        private static int _counter;
        /// <summary>
        /// A hashset of strings that has already been fullpath'd 
        /// </summary>
        private static readonly HashSet<string> _fullPathCache = new HashSet<string>();
        
        /// <summary>
        /// the Kernel filename prefix string for paths that should not be interpreted. 
        /// Just nod, and keep goin'
        /// </summary>
        private const string NonInterpretedPathPrefix = @"\??\";

        /// <summary>
        /// regular expression to identify a UNC path returned by the Kernel.
        /// (needed for path normalization for reparse points)
        /// </summary>
        private static readonly Regex _uncPrefixRx = new Regex(@"\\\?\?\\UNC\\");
        /// <summary>
        /// regular expression to match a drive letter in a low level path
        /// (needed for path normalization for reparse points)
        /// </summary>
        private static readonly Regex _drivePrefixRx = new Regex(@"\\\?\?\\[a-z,A-Z]\:\\");
        
        #pragma warning disable 169
        /// <summary>
        /// regular expression to identify a volume mount point 
        /// (needed for path normalization for reparse points)
        /// </summary>
        private static readonly Regex _volumePrefixRx = new Regex(@"\\\?\?\\Volume");
        #pragma warning restore 169

        /// <summary>
        /// Apparently, Eric has gone insane?
        /// NOTE: subject to cleanup.
        /// </summary>
        private static readonly Regex _invalidDoubleWcRx = new Regex(@"\\.+\*\*|\*\*[^\\]+\\|\*\*\\\*\*");


        /// <summary>
        /// Determines if the childPath is a sub path of the rootPath
        /// </summary>
        /// <param name="rootPath">The root path.</param>
        /// <param name="childPath">The child path.</param>
        /// <returns><c>true</c> if [is sub path] [the specified root path]; otherwise, <c>false</c>.</returns>
        /// <remarks></remarks>
        public static bool IsSubPath(this string rootPath, string childPath) {
            return Path.GetFullPath(childPath).StartsWith(Path.GetFullPath(rootPath), StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Gets the portion of the childPath that is a sub path of the parentPath
        /// Returns string.Empty if the childPath is not a sub path of the parent.
        /// </summary>
        /// <param name="parentPath">The parent path.</param>
        /// <param name="childPath">The child path.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static string GetSubPath(this string parentPath, string childPath) {
            var parent = Path.GetFullPath(parentPath);
            var child = Path.GetFullPath(childPath);
            if (child.StartsWith(parent, StringComparison.CurrentCultureIgnoreCase)) {
                return child.Substring(parent.Length).Trim('/', '\\');
            }
            return string.Empty;
        }

        /// <summary>
        /// Changes the file extension to another extension.
        /// </summary>
        /// <param name="currentFilename">The current filename.</param>
        /// <param name="newExtension">The new extension.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static string ChangeFileExtensionTo(this string currentFilename, string newExtension) {
            return Path.Combine(Path.GetDirectoryName(currentFilename) ?? "",
                Path.GetFileNameWithoutExtension(currentFilename) + "." + newExtension);
        }

        /// <summary>
        /// Gets the relative path between two paths.
        /// </summary>
        /// <param name="currentDirectory">The current directory.</param>
        /// <param name="pathToMakeRelative">The path to make relative.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static string RelativePathTo(this string currentDirectory, string pathToMakeRelative) {
            if (string.IsNullOrEmpty(currentDirectory)) {
                throw new ArgumentNullException("currentDirectory");
            }

            if (string.IsNullOrEmpty(pathToMakeRelative)) {
                throw new ArgumentNullException("pathToMakeRelative");
            }

            currentDirectory = Path.GetFullPath(currentDirectory);
            pathToMakeRelative = Path.GetFullPath(pathToMakeRelative);

            if (!Path.GetPathRoot(currentDirectory).Equals(Path.GetPathRoot(pathToMakeRelative), StringComparison.CurrentCultureIgnoreCase)) {
                return pathToMakeRelative;
            }

            var relativePath = new List<string>();
            var currentDirectoryElements = currentDirectory.Split(Path.DirectorySeparatorChar);
            var pathToMakeRelativeElements = pathToMakeRelative.Split(Path.DirectorySeparatorChar);
            var commonDirectories = 0;

            for (; commonDirectories < Math.Min(currentDirectoryElements.Length, pathToMakeRelativeElements.Length); commonDirectories++) {
                if (
                    !currentDirectoryElements[commonDirectories].Equals(pathToMakeRelativeElements[commonDirectories],
                        StringComparison.CurrentCultureIgnoreCase)) {
                    break;
                }
            }

            for (var index = commonDirectories; index < currentDirectoryElements.Length; index++) {
                if (currentDirectoryElements[index].Length > 0) {
                    relativePath.Add("..");
                }
            }

            for (var index = commonDirectories; index < pathToMakeRelativeElements.Length; index++) {
                relativePath.Add(pathToMakeRelativeElements[index]);
            }

            return string.Join(Path.DirectorySeparatorChar.ToString(), relativePath);
        }

        /// <summary>
        /// Generates a filename based of a template that can contain many different values 
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="filenameHint">The filename hint.</param>
        /// <returns></returns>
        /// <remarks> 
        /// {filename}  - substitutes for the original
        ///               filename, no extension
        ///     {ext}         original extension
        ///     {folder}    - original folder
        ///     {subfolder} - original folder without leading /
        ///     {count}     - a running count of the files
        ///                   downloaded.
        ///     {date}      - the current date (y-m-d)
        ///     {date-long} - the date in long format
        ///     {time}      - the current time (Hh:mm:ss)
        ///     {time-long} - the current time in long fmt
        ///     {ticks}     - the current timestamp as tics
        /// </remarks>
        public static string GenerateTemplatedFilename(this string filename, string filenameHint) {
            var result = filename;
            
            if (!filenameHint.StartsWith("\\\\")) {
                if (filenameHint.StartsWith("/") || filenameHint.StartsWith("\\")) {
                    filenameHint = Environment.CurrentDirectory.Substring(0, 2) + filenameHint;
                }

                if (filenameHint.IndexOf(":") == -1) {
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
            if (!string.IsNullOrEmpty(pr)) {
                result = result.Replace(@"{subfolder}", Path.GetDirectoryName(localPath).Remove(0, pr.Length));
            }

            result = result.Replace(@"{count}", "" + _counter++);
            result = result.Replace(@"{date}", DateTime.Now.ToString("yyyy-MM-dd"));
            result = result.Replace(@"{date-long}", DateTime.Now.ToString("MMMM dd YYYY"));
            result = result.Replace(@"{time}", DateTime.Now.ToString("hh-mm-ss"));
            result = result.Replace(@"{time-long}", DateTime.Now.ToString("HH-mm-ss-ffff"));
            result = result.Replace(@"{ticks}", "" + DateTime.Now.Ticks);

            return result;
        }

        /// <summary>
        /// Generates a filename on a somewhat different template.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        /// <remarks>
        /// {date}      - the current date (y-m-d)
        /// {date-long} - the date in long format
        /// {time}      - the current time (Hh:mm:ss)
        /// {time-long} - the current time in long fmt
        /// {ticks}     - the current timestamp as tics
        /// {counter}   - a running counter
        /// </remarks>
        public static string FormatFilename(this string filename, params string[] parameters) {
            var result = filename;
            result = result.Replace(@"{counter}", "" + _counter++);
            result = result.Replace(@"{date}", DateTime.Now.ToString("yyyy-MM-dd"));
            result = result.Replace(@"{date-long}", DateTime.Now.ToString("MMMM dd YYYY"));
            result = result.Replace(@"{time}", DateTime.Now.ToString("hhmmss"));
            result = result.Replace(@"{time-long}", DateTime.Now.ToString("HHmmssffff"));
            result = result.Replace(@"{ticks}", "" + DateTime.Now.Ticks);

            return result.format(parameters);
        }


        /// <summary>
        /// Enumerates files in a directory, smarter than Direcotry.EnumerateFiles (ie, doesn't throw, when it can't access something)
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="searchPattern">The search pattern.</param>
        /// <param name="searchOption">The search option.</param>
        /// <param name="skipPathPatterns">The skip path patterns.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static IEnumerable<string> DirectoryEnumerateFilesSmarter(this string path, string searchPattern, SearchOption searchOption,
            IEnumerable<string> skipPathPatterns = null) {
            var result = Enumerable.Empty<string>();

            try {
                if (skipPathPatterns != null ? skipPathPatterns.Any(pattern => path.IsWildcardMatch(pattern)) : false) {
                    return result;
                }

                result = Directory.EnumerateFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
            }
            catch {
            }

            if (searchOption == SearchOption.AllDirectories) {
                try {
                    result =
                        result.Union(Directory.EnumerateDirectories(path).Aggregate(result,
                            (current, directory) =>
                                current.Union(DirectoryEnumerateFilesSmarter(directory, searchPattern, SearchOption.AllDirectories))));
                }
                catch {
                }
            }
            return result;
        }



        /// <summary>
        /// A front end to DirectoryEnumerateFilesSmarter that allows for wildcards in the path (and expands it out to a full path first.)
        /// </summary>
        /// <param name="pathMask">The path mask.</param>
        /// <param name="searchOption">The search option.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static IEnumerable<string> FindFilesSmarter(this string pathMask, SearchOption searchOption = SearchOption.TopDirectoryOnly) {
            var path = (pathMask.Replace("*", "$$STAR$$").Replace("?", "$$QUERY$$")).Replace("$$STAR$$", "*").Replace("$$QUERY$$", "?").GetFullPath();
            var mask = path.Substring(path.LastIndexOf("\\") + 1);

            path = path.Substring(0, path.LastIndexOf("\\"));
            return path.DirectoryEnumerateFilesSmarter(mask, searchOption);
        }

        /// <summary>
        /// finds matches for a collection of filenames using FindFilesSmarter (above)
        /// </summary>
        /// <param name="pathMasks">The path masks.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static IEnumerable<string> FindFilesSmarter(this IEnumerable<string> pathMasks) {
            return pathMasks.Aggregate(Enumerable.Empty<string>(), (current, p) => current.Union(p.FindFilesSmarter()));
        }

#if !COAPP_ENGINE_CORE 
        /// <summary>
        ///   always call IsWildcardMatch with a prefix!!!!
        /// </summary>
        /// <param name = "pathMask"></param>
        /// <param name = "pathPrefix"></param>
        /// <returns></returns>
        public static IEnumerable<string> FindFilesSmarterComplex(this string pathMask, string pathPrefix = null) {
            //pathMask safety
            if (String.IsNullOrEmpty(pathMask)) {
                return FindFilesSmarterComplex(pathPrefix);
            }
            if (_invalidDoubleWcRx.IsMatch(pathMask)) {
                throw new ArgumentException(Resources.Invalid_WildcardPath.format(pathMask));
            }


            pathPrefix = String.IsNullOrEmpty(pathPrefix) ? Directory.GetCurrentDirectory() : pathPrefix;

            pathMask = pathMask.Replace("/", "\\");
            var nextPart = pathMask.GetNextPart();
            var onLastPart = nextPart.Item2 == "";

            if (nextPart.Item1 == "**") {
                if (onLastPart) {
                    //we just get every file from here on down

                    return Directory.EnumerateFiles(pathPrefix, "*", SearchOption.AllDirectories);
                }
                else {
                    var partAfterWildcard = nextPart.Item2.GetNextPart();

                    var nextPartIsLast = partAfterWildcard.Item2 == "";

                    if (nextPartIsLast) {
                        return Directory.EnumerateFiles(pathPrefix, partAfterWildcard.Item1, SearchOption.AllDirectories).
                            Aggregate(Enumerable.Empty<string>(),
                                (output, d) => output.Concat(pathPrefix.RelativePathTo(d).FindFilesSmarterComplex(pathPrefix)));
                    }
                    var dirs = Directory.EnumerateDirectories(pathPrefix, partAfterWildcard.Item1, SearchOption.AllDirectories);

                    return dirs.
                        Aggregate(Enumerable.Empty<string>(),
                            (output, d) =>
                                output.Concat(
                                    (pathPrefix.RelativePathTo(d) + "\\" + partAfterWildcard.Item2).FindFilesSmarterComplex(pathPrefix)));
                }
            }
            if (nextPart.Item1.Contains("*")) {
                if (onLastPart) {
                    return Directory.EnumerateFiles(pathPrefix, nextPart.Item1).
                        Aggregate(Enumerable.Empty<string>(),
                            (output, d) => output.Concat(Path.GetFileName(d).FindFilesSmarterComplex(pathPrefix)));
                }
                var dirs = Directory.EnumerateDirectories(pathPrefix, nextPart.Item1);

                return dirs.
                    Aggregate(Enumerable.Empty<string>(),
                        (output, d) => output.Concat((Path.GetFileName(d) + "\\" + nextPart.Item2).FindFilesSmarterComplex(pathPrefix)));
            }
            //recursively keep going
            var newPathPrefix = pathPrefix;

            if (!String.IsNullOrEmpty(nextPart.Item1)) {
                newPathPrefix += "\\" + nextPart.Item1;
            }

            if (onLastPart) {
                return File.Exists(newPathPrefix) ? newPathPrefix.SingleItemAsEnumerable() : Enumerable.Empty<string>();
            }
            return Directory.Exists(newPathPrefix)
                ? nextPart.Item2.FindFilesSmarterComplex(newPathPrefix)
                : Enumerable.Empty<string>();
        }

        public static IEnumerable<string> FindFilesSmarterComplex(this IEnumerable<string> pathMasks) {
            return pathMasks.Aggregate(Enumerable.Empty<string>(), (current, p) => current.Union(p.FindFilesSmarterComplex()));
        }
#endif

        /// <summary>
        /// Gets the name of a file minus it's extension, ie: if the file name is "test.exe", returns "test".
        /// </summary>
        /// <param name="fi">The fi.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static string NameWithoutExt(this FileInfo fi) {
            return fi.Name.Remove(fi.Name.Length - fi.Extension.Length);
        }

        /// <summary>
        /// Directories the exists and is accessible.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static bool DirectoryExistsAndIsAccessible(this string path) {
            try {
                return Directory.Exists(path);
            }
            catch {
            }
            return false;
        }

        /// <summary>
        /// Writes all bytes from the contents of a memorystream to file (as a binary file).
        /// </summary>
        /// <param name="ms">The ms.</param>
        /// <param name="path">The path.</param>
        /// <remarks></remarks>
        public static void WriteAllBytesToFile(this MemoryStream ms, string path) {
            if (string.IsNullOrEmpty(path)) {
                throw new ArgumentNullException("path", "Invalid Path");
            }

            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read)) {
                stream.Write(ms.GetBuffer(), 0, (int) ms.Length);
            }
        }

        /// <summary>
        /// Reads the contents of a file into a memory stream.
        /// </summary>
        /// <param name="ms">The ms.</param>
        /// <param name="path">The path.</param>
        /// <remarks></remarks>
        public static void ReadAllBytesFromFile(this MemoryStream ms, string path) {
            if (string.IsNullOrEmpty(path)) {
                throw new ArgumentNullException("path", "Invalid Path");
            }

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                ms.SetLength(stream.Length);
                stream.Read(ms.GetBuffer(), 0, (int) stream.Length);
            }
        }

        /// <summary>
        /// Tries the hard to delete file.
        /// 
        /// This will try to delete a file.
        /// Failing that, it will move the file out to a temp location and mark it for deletion on reboot.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <remarks></remarks>
        public static void TryHardToDeleteFile(this string filename) {
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
                    Kernel32.MoveFileEx(Directory.Exists(tmpFilename) ? tmpFilename : filename, null,
                        MoveFileFlags.MOVEFILE_DELAY_UNTIL_REBOOT);
                }
                catch {
                    // really. Hmmm. 
                }
            }
        }

        /// <summary>
        /// Tries the hard to delete directory.
        /// 
        /// If it can't, it will move the folder and mark it for deletion on reboot.
        /// </summary>
        /// <param name="directoryName">Name of the directory.</param>
        /// <remarks></remarks>
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
                    Kernel32.MoveFileEx(Directory.Exists(tmpFilename) ? tmpFilename : directoryName, null,
                        MoveFileFlags.MOVEFILE_DELAY_UNTIL_REBOOT);
                }
                catch {
                    // really. Hmmm. 
                }
            }
        }

        /// <summary>
        /// Writes the whole byte array to a filestream. (lazy!)
        /// </summary>
        /// <param name="fileStream">The file stream.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static int Write(this FileStream fileStream, byte[] data) {
            fileStream.Write(data, 0, data.Length);
            return data.Length;
        }

        /// <summary>
        /// Returns the full path of a string.
        /// 
        /// Short circuts the process if the string is a known full path already.
        /// (ie, the result of a preivious GetFullPath())
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static string GetFullPath(this string path) {
            if (_fullPathCache.Contains(path)) {
                return path;
            }
            try {
                path = Path.GetFullPath(path.Trim('"'));
                _fullPathCache.Add(path);
            }
            catch {
            }
            return path;
        }

        /// <summary>
        /// Translates paths starting with \??\ to regular paths.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static string NormalizePath(this string path) {
            if (path.StartsWith(NonInterpretedPathPrefix)) {
                if (_uncPrefixRx.Match(path).Success) {
                    path = _uncPrefixRx.Replace(path, @"\\");
                }

                if (_drivePrefixRx.Match(path).Success) {
                    path = path.Replace(NonInterpretedPathPrefix, "");
                }
            }
            if (path.EndsWith("\\")) {
                var couldBeFilePath = path.Substring(0, path.Length - 1);
                if (File.Exists(couldBeFilePath)) {
                    path = couldBeFilePath;
                }
            }

            return path;
        }

        /// <summary>
        /// This takes a string that is representative of a filename 
        /// and tries to create a path that can be considered the 'canonical' path.
        /// 
        /// path on drives that are mapped as remote shares are rewritten as their \\server\share\path 
        /// </summary>
        /// <returns></returns>
        public static string CanonicalizePath(this string path, bool IsPotentiallyRelativePath = true) {
            Uri pathUri = null;
            try {
                pathUri = new Uri(path);
                if( !pathUri.IsFile ) {
                    throw new PathIsNotFileUriException(path, pathUri);
                }
                
                // is this a unc path?
                if( string.IsNullOrEmpty(pathUri.Host)) {
                    // no, this is a drive:\path path
                    // use API to resolve out the drive letter to see if it is a remote 
                    var drive = pathUri.Segments[1].Replace('/','\\'); // the zero segment is always just '/' 

                    var sb = new StringBuilder(512);
                    var size = sb.Capacity;

                    var error = MPR.WNetGetConnection(drive, sb, ref size);
                    if (error == 0) {
                        if( pathUri.Segments.Length > 2 ) {
                            return pathUri.Segments.Skip(2).Aggregate(sb.ToString().Trim(), (current, item) => current + item);
                        }
                    }
                }
                // not a remote (or resovably-remote) path or 
                // it is already a path that is in it's correct form (via localpath)
                return pathUri.LocalPath;
            }
            catch(UriFormatException) {
                // we could try to see if it is a relative path...
                if( IsPotentiallyRelativePath) {
                    return CanonicalizePath(path.GetFullPath(), false);
                }
                throw new ArgumentException("specified path can not be resolved as a file name or path (unc, url, localpath)", path);
            }

        }

        /// <summary>
        /// Gets the next part.
        /// 
        /// Note: Make Eric document this?
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        private static Tuple<string, string> GetNextPart(this string path) {
            var indexOfSlash = path.IndexOf('\\');
            return indexOfSlash == -1 ? new Tuple<string, string>(path, "") : 
                new Tuple<string, string>(path.Substring(0, indexOfSlash), path.Substring(indexOfSlash + 1));
        }

        /// <summary>
        /// Replaces Unix style file path separators (/) with Windows style (\).
        /// </summary>
        /// <param name="filepath">The filepath.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static string FixFilepathSlashes(this string filepath)
        {
            return filepath.Replace(@"/", @"\");
        }


        /// <summary>
        /// Tells whether a given path is a simple subpath.
        /// A simple subpath has the following characteristics:
        /// - No drive letter or colon
        /// - Does not start with a slash
        /// - Does not contain any path part sections consisting of just "." or ".."
        /// - Does not contain wildcards
        /// </summary>
        /// <param name="path">the path to check</param>
        /// <returns>True if it is a simple subpath, false otherwise.</returns>
        /// <remarks></remarks>
        public static bool IsSimpleSubPath(this string path)
        {
            var temp = path.FixFilepathSlashes();
            if (temp.Contains(":") || temp.Contains("*"))
                return false;
            if (temp.StartsWith(@"\"))
                return false;
            var pathParts = temp.Split('\\');
            if (pathParts.Any((i) => i == ".." || i == "."))
                return false;

            return true;
        }

        public static string EnsureFileIsLocal(this string filename, string localFolder= null) {
            localFolder = localFolder ?? Path.GetTempPath();
            var fullpath = filename.CanonicalizePath();

            if (File.Exists(fullpath)) {
                if (fullpath.StartsWith(@"\\")) {
                    var localCopy = Path.Combine(localFolder, Path.GetFileName(fullpath));
                    File.Copy(fullpath, localCopy);
                    return localCopy;
                }

                return fullpath;
            }
            return null;
        }

        public static IEnumerable<string> GetMinimalPaths( this IEnumerable<string> paths ) {
            if (paths.Any() && paths.Skip(1).Any()) {
                IEnumerable<IEnumerable<string>> newPaths = paths.Select(each => each.GetFullPath()).Select(each => each.Split('\\'));
                while (newPaths.All(each => each.FirstOrDefault() == newPaths.FirstOrDefault().FirstOrDefault())) {
                    newPaths = newPaths.Select(each => each.Skip(1));
                }
                return newPaths.Select(each => each.Aggregate((current, value) => current + "\\" + value));
            }
            return paths.Select(Path.GetFileName);
        }
    }
}