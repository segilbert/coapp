//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Original Copyright (c) 2009 Microsoft Corporation. All rights reserved.
//     Changes Copyright (c) 2010  Eric Schultz, Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

// -----------------------------------------------------------------------
// Original Code: 
// (c) 2009 Microsoft Corporation -- All rights reserved
// This code is licensed under the MS-PL
// http://www.opensource.org/licenses/ms-pl.html
// Courtesy of the Open Source Techology Center: http://port25.technet.com
// -----------------------------------------------------------------------

namespace CoApp.Toolkit.Utility {
    using Microsoft.Win32;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Diagnostics;
    using Extensions;

    public class ProgramFinder {
        private static IEnumerable<string> commonSearchLocations = new List<string>();
        private IEnumerable<string> searchLocations = new List<string>();
        private IEnumerable<string> recursiveSearchLocations = new List<string>();

        public static ProgramFinder ProgramFiles;
        public static ProgramFinder ProgramFilesAndSys32;
        public static ProgramFinder ProgramFilesAndDotNet;
        public static ProgramFinder ProgramFilesSys32AndDotNet;
        private static readonly Dictionary<string, ExecutableInfo> ExeTypeCache = new Dictionary<string, ExecutableInfo>();
        private static readonly Dictionary<string, string> ToolVersionCache = new Dictionary<string, string>();
        private static readonly Dictionary<string, UInt64> ToolVersionNumericCache = new Dictionary<string, UInt64>();

        public static bool IgnoreCache;

        static ProgramFinder() {
            AddCommonSearchLocations("%path%");
            AddCommonSearchLocations(Environment.CurrentDirectory);

            ProgramFiles = new ProgramFinder("", @"%ProgramFiles(x86)%;%ProgramFiles%;%ProgramW6432%");
            ProgramFilesAndSys32 = new ProgramFinder("",@"%ProgramFiles(x86)%;%ProgramFiles%;%ProgramW6432%;%SystemRoot%\system32");
            ProgramFilesAndDotNet = new ProgramFinder("", @"%ProgramFiles(x86)%;%ProgramFiles%;%ProgramW6432%;%SystemRoot%\Microsoft.NET");
            ProgramFilesSys32AndDotNet = new ProgramFinder("", @"%ProgramFiles(x86)%;%ProgramFiles%;%ProgramW6432%;%SystemRoot%\system32;%SystemRoot%\Microsoft.NET");
        }

        public ProgramFinder(string searchPath) {
            AddSearchLocations(searchPath);
        }
        
        public ProgramFinder(string searchPath, string recursivePath) {
            AddSearchLocations(searchPath);
            AddRecursiveSearchLocations(recursivePath);
        }

        private static void AddPathsToList(string paths,ref IEnumerable<string> list) {
            list = list.Union(
                from eachPath in
                    Environment.ExpandEnvironmentVariables(paths).Split(new []{ ';' }, StringSplitOptions.RemoveEmptyEntries)
                where Directory.Exists(Path.GetFullPath(eachPath))
                select eachPath);
        }

        private void AddSearchLocations(string paths) {
            AddPathsToList(paths, ref searchLocations);
        }

        private void AddRecursiveSearchLocations(string paths) {
            AddPathsToList(paths,ref recursiveSearchLocations);
        }

        private static void AddCommonSearchLocations(string paths) {
            AddPathsToList(paths,ref commonSearchLocations);
        }

        public string ScanForFile(string filename, string minimumVersion, IEnumerable<string> filters = null) {
            return ScanForFile(filename, ExecutableInfo.none, minimumVersion,filters);
        }

        public string ScanForFile(string filename, ExecutableInfo executableType, IEnumerable<string> filters) {
            return ScanForFile(filename, executableType, "0.0", filters);
        }

        public string ScanForFile(string filename, ExecutableInfo executableType = ExecutableInfo.none, string minimumVersion = "0.0", IEnumerable<string> filters = null) {
            if (!IgnoreCache) {
                string result = GetCachedPath(filename, executableType, minimumVersion);
                if (!string.IsNullOrEmpty(result))
                    return result;
            }

            var ver = minimumVersion.VersionStringToUInt64();

            var files = commonSearchLocations.Union(searchLocations).AsParallel().SelectMany(
                directory => DirectoryEnumerateFilesSmarter(directory, filename, SearchOption.TopDirectoryOnly))
                .Union(
                    recursiveSearchLocations.AsParallel().SelectMany(
                        directory => DirectoryEnumerateFilesSmarter(directory, filename, SearchOption.AllDirectories)));

            files = files.Where(file => (GetExeType(file) & executableType) == executableType && GetToolVersionNumeric(file) >= ver);

            if( filters != null  ) {
                files = filters.Aggregate(files, (current, filter) => (from eachFile in current
                                                                       where !eachFile.IsWildcardMatch(filter)
                                                                       select eachFile));
            }

            var filePath = files.MaxElement(GetToolVersionNumeric);
            if (!string.IsNullOrEmpty(filePath)) {
                SetCachedPath(filename, filePath, executableType, minimumVersion);
                SetCachedPath(filename, filePath, executableType, GetToolVersion(filePath));
            }

            return filePath;
        }

        private static string GetCachedPath(string toolEntry, ExecutableInfo executableInfo, string minimumToolVersion) {
            return GetCachedPath("{0}/{1}/{2}".format(toolEntry, executableInfo, minimumToolVersion));
        }

        private static void SetCachedPath(string toolEntry, string location, ExecutableInfo executableInfo, string minimumToolVersion) {
            SetCachedPath("{0}/{1}/{2}".format(toolEntry, executableInfo, minimumToolVersion), location);
        }

        private static string GetCachedPath(string toolEntry) {
            if (IgnoreCache)
                return null;

            RegistryKey regkey = null;
            string result = null;
            try {
                regkey = Registry.CurrentUser.CreateSubKey(@"Software\CoApp\Tools");

                if (null == regkey)
                    return null;

                result = regkey.GetValue(toolEntry, null) as string;

                if (null != result && !File.Exists(result))
                    regkey.DeleteValue(toolEntry);

            }
            catch {
            }
            finally {
                if (null != regkey)
                    regkey.Close();
            }
            return result;
        }

        private static void SetCachedPath(string toolEntry, string location) {
            RegistryKey regkey = null;
            try {
                regkey = Registry.CurrentUser.CreateSubKey(@"Software\CoApp\Tools");

                if (null == regkey)
                    return;

                regkey.SetValue(toolEntry, location);
            }
            catch {
            }
            finally {
                if (null != regkey)
                    regkey.Close();
            }
        }

        public static string GetToolVersion(string fileName) {
            try {
                if (ToolVersionCache.ContainsKey(fileName))
                    return ToolVersionCache[fileName];

                FileVersionInfo info = FileVersionInfo.GetVersionInfo(fileName);
                string fv = info.FileVersion;
                if( !string.IsNullOrEmpty(fv) ) {
                    fv = fv.Substring(0, fv.PositionOfFirstCharacterNotIn("0123456789."));
                }
                ToolVersionCache.Add(fileName,fv);

                return fv;
            }
            catch {
                return string.Empty;
            }
        }

        private static IEnumerable<string> DirectoryEnumerateFilesSmarter(string path, string searchPattern, SearchOption searchOption) {
            IEnumerable<string> result = Enumerable.Empty<string>();
            
            try {
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


        public static UInt64 GetToolVersionNumeric(string fileName) {
            if (ToolVersionNumericCache.ContainsKey(fileName))
                return ToolVersionNumericCache[fileName];

            var fv = GetToolVersion(fileName).VersionStringToUInt64();
            ToolVersionNumericCache.Add(fileName,fv);
            return fv;
        }

        public static ExecutableInfo GetExeType(string filename) {
            if (ExeTypeCache.ContainsKey(filename))
                return ExeTypeCache[filename];

            using (FileStream s = File.OpenRead(filename)) {
                var buffer = new byte[4096];
                int iRead = s.Read(buffer, 0, 4096);
                ExecutableInfo result;
                if (buffer[0] != 77 || buffer[1] != 90 || iRead < 2048) {
                    result = ExecutableInfo.none;
                }
                else {
                    unsafe {
                        fixed (byte* pData = buffer) {
                            var idh = (IMAGE_DOS_HEADER*) pData;
                            var inhs = (IMAGE_NT_HEADERS32*) (idh->e_lfanew + pData);

                            result = inhs->OptionalHeader.Magic == 0x20b
                                         ? (((IMAGE_NT_HEADERS64*) inhs)->OptionalHeader.DataDirectory.Size > 0
                                                ? ExecutableInfo.x64 | ExecutableInfo.managed
                                                : ExecutableInfo.x64 | ExecutableInfo.native)
                                         : (inhs->OptionalHeader.DataDirectory.Size > 0
                                                ? ExecutableInfo.x86 | ExecutableInfo.managed
                                                : ExecutableInfo.x86 | ExecutableInfo.native);
                        }
                    }
                }
                ExeTypeCache.Add(filename, result);
                return result;
            }
        }
    }
}
