//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Original Copyright (c) 2009 Microsoft Corporation. All rights reserved.
//     Changes Copyright (c) 2010  Eric Schultz, Garrett Serack. All rights reserved.
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

namespace CoApp.Toolkit.Utility {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Extensions;
    using Microsoft.Win32;
    using Win32;
    using RegistryView = Configuration.RegistryView;

    /// <summary>
    /// Utility to search for an executable in the file system
    /// </summary>
    public class ProgramFinder {
        private static IEnumerable<string> _commonSearchLocations = new List<string>();
        private IEnumerable<string> _searchLocations = new List<string>();
        private IEnumerable<string> _recursiveSearchLocations = new List<string>();

        /// <summary>
        /// A ProgramFinder which searches in all Program Files directories
        /// </summary>
        public static ProgramFinder ProgramFiles;
        /// <summary>
        /// A ProgramFinder which searches in Program Files and Windows' system32 folder
        /// </summary>
        public static ProgramFinder ProgramFilesAndSys32;
        /// <summary>
        /// A ProgramFinder which searches in Program Files, GAC and Window's .NET tools
        /// </summary>
        public static ProgramFinder ProgramFilesAndDotNet;
        /// <summary>
        /// A ProgramFinder which searches in Program Files, Windows' system32 folder, GAC and Window's .NET tools
        /// </summary>
        public static ProgramFinder ProgramFilesSys32AndDotNet;
        /// <summary>
        /// A ProgramFinder which searches in Program Files, .NET tools, WDK and Windows SDK
        /// </summary>
        public static ProgramFinder ProgramFilesAndDotNetAndSdk;

        /// <summary>
        /// Disable cacheing (no lookup, no writes)
        /// </summary>
        public static bool IgnoreCache;

        static ProgramFinder() {
            AddCommonSearchLocations("%path%");
            AddCommonSearchLocations(Environment.CurrentDirectory);
            AddCommonSearchLocations(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));

            ProgramFiles = new ProgramFinder("", @"%ProgramFiles(x86)%;%ProgramFiles%;%ProgramW6432%");
            ProgramFilesAndSys32 = new ProgramFinder("", @"%ProgramFiles(x86)%;%ProgramFiles%;%ProgramW6432%;%SystemRoot%\system32");
            ProgramFilesAndDotNet = new ProgramFinder("", @"%ProgramFiles(x86)%;%ProgramFiles%;%ProgramW6432%;%SystemRoot%\Microsoft.NET");
            ProgramFilesSys32AndDotNet = new ProgramFinder("",
                @"%ProgramFiles(x86)%;%ProgramFiles%;%ProgramW6432%;%SystemRoot%\system32;%SystemRoot%\Microsoft.NET");

            var sdkFolder = RegistryView.System[@"SOFTWARE\Microsoft\Microsoft SDKs\Windows", "CurrentInstallFolder"].Value as string;
            var wdkFolder = RegistryView.System[@"SOFTWARE\Wow6432Node\Microsoft\WDKDocumentation\7600.091201\Setup", "Build"].Value as string;
            
            if (string.IsNullOrEmpty(wdkFolder)) {
                wdkFolder = RegistryView.System[@"SOFTWARE\Microsoft\WDKDocumentation\7600.091201\Setup", "Build"].Value as string;
            }

            ProgramFilesAndDotNetAndSdk = string.IsNullOrEmpty(sdkFolder)
                ? ProgramFilesAndDotNet
                : new ProgramFinder("", @"%ProgramFiles(x86)%;%ProgramFiles%;%ProgramW6432%;%SystemRoot%\Microsoft.NET;" + sdkFolder + ";" + wdkFolder);
        }

        /// <summary>
        /// A ProgramFinder which searches in given paths and common locations
        /// </summary>
        /// <param name="searchPath">Paths to search (semicolon ';' delimited)</param>
        public ProgramFinder(string searchPath) {
            AddSearchLocations(searchPath);
        }

        /// <summary>
        /// A ProgramFinder which searches in given paths and common locations
        /// </summary>
        /// <param name="searchPath">Paths to search (semicolon ';' delimited</param>
        /// <param name="recursivePath">Paths to search recursively (semicolon ';' delimited</param>
        public ProgramFinder(string searchPath, string recursivePath) {
            AddSearchLocations(searchPath);
            AddRecursiveSearchLocations(recursivePath);
        }

        private static void AddPathsToList(string paths, ref IEnumerable<string> list) {
            list = list.Union(
                from eachPath in
                    Environment.ExpandEnvironmentVariables(paths).Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries)
                where Directory.Exists(eachPath.GetFullPath())
                select eachPath);
        }

        private void AddSearchLocations(string paths) {
            AddPathsToList(paths, ref _searchLocations);
        }

        private void AddRecursiveSearchLocations(string paths) {
            AddPathsToList(paths, ref _recursiveSearchLocations);
        }

        private static void AddCommonSearchLocations(string paths) {
            AddPathsToList(paths, ref _commonSearchLocations);
        }

        /// <summary>
        /// Finds a tool in the file system
        /// </summary>
        /// <param name="filename">The name of the tool</param>
        /// <param name="minimumVersion">Minimum required version</param>
        /// <param name="filters">A list of tool names which should be excluded from the scan</param>
        /// <returns></returns>
        public string ScanForFile(string filename, string minimumVersion, IEnumerable<string> filters = null) {
            return ScanForFile(filename, ExecutableInfo.none, minimumVersion, filters);
        }

        /// <summary>
        /// Finds a tool in the file system
        /// </summary>
        /// <param name="filename">The name of the tool</param>
        /// <param name="executableType">Platform or assembly information</param>
        /// <param name="filters">A list of tool names which should be excluded from the scan</param>
        /// <returns></returns>
        public string ScanForFile(string filename, ExecutableInfo executableType, IEnumerable<string> filters) {
            return ScanForFile(filename, executableType, "0.0", filters);
        }

        /// <summary>
        /// Finds a tool in the file system
        /// </summary>
        /// <param name="filename">The name of the tool</param>
        /// <param name="executableType">Platform or assembly info</param>
        /// <param name="minimumVersion">Minimum required version</param>
        /// <param name="excludeFilters">A list of tool names which should be excluded from the scan</param>
        /// <param name="includeFilters">A list of tool names which should be included in the scan</param>
        /// <param name="rememberMissingFile">Disables searching for files which are known to be missing.</param>
        /// <param name="tagWithCosmeticVersion"></param>
        /// <returns></returns>
        public string ScanForFile(string filename, ExecutableInfo executableType = ExecutableInfo.none, string minimumVersion = "0.0",
            IEnumerable<string> excludeFilters = null, IEnumerable<string> includeFilters= null, bool rememberMissingFile = false, string tagWithCosmeticVersion = null ) {
            if (!IgnoreCache) {
                var result = GetCachedPath(filename, executableType, tagWithCosmeticVersion ?? minimumVersion);

                if ("NOT-FOUND".Equals(result)) {
                    if (rememberMissingFile) {
                        return null; // we've asked to remember that we haven't found this
                    }
                    result = null; // we've not asked to remember that, so we'll let it try to find it again.
                }

                if (!string.IsNullOrEmpty(result)) {
                    return result;
                }
            }

            Notify("[One moment.. Scanning for utility({0}/{1}/{2})]", filename, executableType.ToString(), tagWithCosmeticVersion ?? minimumVersion);

            var ver = minimumVersion.VersionStringToUInt64();

            var files = _commonSearchLocations.Union(_searchLocations).SelectMany(
                directory => directory.DirectoryEnumerateFilesSmarter("**\\"+filename, SearchOption.TopDirectoryOnly))
                .Union(
                    _recursiveSearchLocations.AsParallel().SelectMany(
                        directory => directory.DirectoryEnumerateFilesSmarter("**\\"+filename, SearchOption.AllDirectories)));

            if (executableType != ExecutableInfo.none || ver != 0) {
                files =
                    files.Where(
                        file =>
                            (PEInfo.Scan(file).ExecutableInfo & executableType) == executableType &&
                                PEInfo.Scan(file).FileVersionLong >= ver);
            }

            if (includeFilters != null) {
                files = includeFilters.Aggregate(files, (current, filter) => (from eachFile in current
                                                                              where eachFile.IsWildcardMatch(filter)
                                                                              select eachFile));
            }

            if (excludeFilters != null) {
                files = excludeFilters.Aggregate(files, (current, filter) => (from eachFile in current
                    where !eachFile.IsWildcardMatch(filter)
                                                                              select eachFile));
            }

            var filePath = files.MaxElement(each => PEInfo.Scan(each).FileVersionLong);

            if (!string.IsNullOrEmpty(filePath) || rememberMissingFile) {
                SetCachedPath(filename, string.IsNullOrEmpty(filePath) ? "NOT-FOUND" : filePath , executableType, tagWithCosmeticVersion ?? minimumVersion);
                try {
                    SetCachedPath(filename, string.IsNullOrEmpty(filePath) ? "NOT-FOUND" : filePath, executableType,
                        PEInfo.Scan(filePath).FileVersion);
                } catch {
                    
                }
            }

            return filePath;
        }

        private static string GetCachedPath(string toolEntry, ExecutableInfo executableInfo, string minimumToolVersion) {
            return GetCachedPath("{0}/{1}/{2}".format(toolEntry, executableInfo, minimumToolVersion));
        }

        /// <summary>
        /// Save a tool to the registry
        /// </summary>
        /// <param name="toolEntry">Name of the tool</param>
        /// <param name="location">Full path to the tool</param>
        /// <param name="executableInfo">Platform or assembly information</param>
        /// <param name="minimumToolVersion"></param>
        private static void SetCachedPath(string toolEntry, string location, ExecutableInfo executableInfo, string minimumToolVersion) {
            SetCachedPath("{0}/{1}/{2}".format(toolEntry, executableInfo, minimumToolVersion), location);
        }

        /// <summary>
        /// Look up a given tool in the registry. (Returns special strings.)
        /// </summary>
        /// <param name="toolEntry">A formatted tool entry</param>
        /// <returns>A string from the cache (a full path or "NOT-FOUND")</returns>
        private static string GetCachedPath(string toolEntry) {
            if (IgnoreCache) {
                return null;
            }

            var view = Configuration.RegistryView.CoAppUser[@"Tools#" + toolEntry];

            var result = view.StringValue;

            // if we've remembered that we've not found something...
            if( "NOT-FOUND".Equals( result, StringComparison.CurrentCultureIgnoreCase ) ) {
                return result;
            }

            if (null != result && !File.Exists(result) ) {
                view.StringValue = null;
            }

            return result;
        }

        /// <summary>
        /// Save a tool to the registry
        /// </summary>
        /// <param name="toolEntry">Formatted tool entry</param>
        /// <param name="location">Full path to the tool</param>
        private static void SetCachedPath(string toolEntry, string location) {
            Configuration.RegistryView.CoAppUser[@"Tools#" + toolEntry].StringValue = location;
        }

        /// <summary>
        /// Write something to the console
        /// </summary>
        /// <param name="message">A formatted message</param>
        /// <param name="arguments">Zero or more strings to format</param>
        private static void Notify(string message, params string[] arguments) {
            Console.WriteLine(message.format(arguments));
        }
    }
}