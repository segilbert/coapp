//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Original Copyright (c) 2009 Microsoft Corporation. All rights reserved.
//     Changes Copyright (c) 2010  Garrett Serack. All rights reserved.
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

    public class ProgramFinder
    {
        private static readonly List<string> commonSearchLocations = new List<string>();
        private readonly List<string> searchLocations = new List<string>();
        private readonly List<string> recursiveSearchLocations = new List<string>();

        public static ProgramFinder ProgramFiles;
        public static ProgramFinder ProgramFilesAndSys32;
        public static ProgramFinder ProgramFilesAndDotNet;
        public static ProgramFinder ProgramFilesSys32AndDotNet;
        public static bool IgnoreCache;

        static ProgramFinder()
        {
            AddCommonSearchLocations("%path%");
            AddCommonSearchLocations(Environment.CurrentDirectory);

            ProgramFiles = new ProgramFinder();
            ProgramFiles.AddRecursiveSearchLocations(@"%ProgramFiles(x86)%;%ProgramFiles%;%ProgramW6432%");

            ProgramFilesAndSys32 = new ProgramFinder();
            ProgramFilesAndSys32.AddRecursiveSearchLocations(@"%ProgramFiles(x86)%;%ProgramFiles%;%ProgramW6432%;%SystemRoot%\system32");

            ProgramFilesAndDotNet = new ProgramFinder();
            ProgramFilesAndDotNet.AddRecursiveSearchLocations(@"%ProgramFiles(x86)%;%ProgramFiles%;%ProgramW6432%;%SystemRoot%\Microsoft.NET");

            ProgramFilesSys32AndDotNet = new ProgramFinder();
            ProgramFilesSys32AndDotNet.AddRecursiveSearchLocations(@"%ProgramFiles(x86)%;%ProgramFiles%;%ProgramW6432%;%SystemRoot%\system32;%SystemRoot%\Microsoft.NET");
        }

        private static void AddPathsToList(string paths, List<string> list)
        {
            foreach (string s in Environment.ExpandEnvironmentVariables(paths).Split(';'))
                if (!string.IsNullOrEmpty(s))
                {
                    string fullPath = Path.GetFullPath(s);
                    if (Directory.Exists(fullPath))
                        list.Add(fullPath);
                }
        }

        public void AddSearchLocations(string paths)
        {
            AddPathsToList(paths, searchLocations);
        }

        public void AddRecursiveSearchLocations(string paths)
        {
            AddPathsToList(paths, recursiveSearchLocations);
        }

        public static void AddCommonSearchLocations(string paths)
        {
            AddPathsToList(paths, commonSearchLocations);
        }

        private string FindFileRecursively(string directory, string toolFilename, string minimumToolVersion)
        {
            string result = Path.Combine(directory, toolFilename);
            if (File.Exists(result) && IsToolVersionGood(result, minimumToolVersion))
                return result;

            foreach (string dir in Directory.GetDirectories(directory))
                if (null != (result = FindFileRecursively(dir, toolFilename, minimumToolVersion)))
                    return result;

            return null;
        }

        private string FindFileLatestRecursively(string directory, string toolFilename, string bestCandidate)
        {
            bestCandidate = Latest(Path.Combine(directory, toolFilename), bestCandidate);

            foreach (string dir in Directory.GetDirectories(directory))
                bestCandidate = FindFileLatestRecursively(dir, toolFilename, bestCandidate);

            return bestCandidate;
        }


        public string FindFile(string fileName, string minimumToolVersion)
        {
            string result = GetCachedPath(fileName);

            if (string.IsNullOrEmpty(result))
            {
                foreach (string path in commonSearchLocations)
                {
                    result = Path.Combine(path, fileName);
                    if (File.Exists(result) && IsToolVersionGood(result, minimumToolVersion))
                    {
                        SetCachedPath(fileName, result);
                        return result;
                    }
                }
                foreach (string path in searchLocations)
                {
                    result = Path.Combine(path, fileName);
                    if (File.Exists(result) && IsToolVersionGood(result, minimumToolVersion))
                    {
                        SetCachedPath(fileName, result);
                        return result;
                    }
                }
                foreach (string path in recursiveSearchLocations)
                {
                    result = FindFileRecursively(path, fileName, minimumToolVersion);
                    if (!string.IsNullOrEmpty(result))
                    {
                        SetCachedPath(fileName, result);
                        return result;
                    }
                }
            }

            return result;
        }

        private string GetCachedPath(string toolEntry)
        {
            if (IgnoreCache)
                return null;

            RegistryKey regkey = null;
            string result = null;
            try
            {
                regkey = Registry.CurrentUser.CreateSubKey(@"Software\gsToolkit\Tools");

                if (null == regkey)
                    return null;

                result = regkey.GetValue(toolEntry, null) as string;

                if (null != result && !File.Exists(result))
                    regkey.DeleteValue(toolEntry);

            }
            catch
            {
            }
            finally
            {
                if (null != regkey)
                    regkey.Close();
            }
            return result;
        }

        private void SetCachedPath(string toolEntry, string location)
        {
            RegistryKey regkey = null;
            try
            {
                regkey = Registry.CurrentUser.CreateSubKey(@"Software\gsToolkit\Tools");

                if (null == regkey)
                    return;

                regkey.SetValue(toolEntry, location);
            }
            catch
            {
            }
            finally
            {
                if (null != regkey)
                    regkey.Close();
            }
        }

        public string FindFile(string fileName)
        {
            return FindFile(fileName, null);
        }

        public string FindFileLatestVersion(string fileName)
        {
            string bestCandidate = GetCachedPath(fileName);

            if (string.IsNullOrEmpty(bestCandidate))
            {
                foreach (string path in commonSearchLocations)
                    bestCandidate = Latest(Path.Combine(path, fileName), bestCandidate);

                foreach (string path in searchLocations)
                    bestCandidate = Latest(Path.Combine(path, fileName), bestCandidate);

                foreach (string path in recursiveSearchLocations)
                    bestCandidate = FindFileLatestRecursively(path, fileName, bestCandidate);

                if (!string.IsNullOrEmpty(bestCandidate))
                    SetCachedPath(fileName, bestCandidate);
            }
            return bestCandidate;
        }

        private bool IsToolVersionGood(string fileName, string minimumToolVersion)
        {
            // if the tool version isn't specified, who cares. 
            if (null == minimumToolVersion)
                return true;

            string toolVersion = GetToolVersion(fileName);

            // if the child class doesn't support versioning, who cares.
            if (string.IsNullOrEmpty(toolVersion))
                return true;

            return IsNewer(toolVersion, minimumToolVersion);
        }

        private string Latest(string newCandidate, string encumbentCandidate)
        {
            if (string.IsNullOrEmpty(newCandidate) || !File.Exists(newCandidate))
                return File.Exists(encumbentCandidate) ? encumbentCandidate : null;

            if (string.IsNullOrEmpty(encumbentCandidate))
                return File.Exists(newCandidate) ? newCandidate : null;

            string newVersion = GetToolVersion(newCandidate);
            if (string.IsNullOrEmpty((newVersion)))
                return encumbentCandidate;

            string encumbentVersion = GetToolVersion(encumbentCandidate);
            if (string.IsNullOrEmpty((encumbentVersion)))
                return newCandidate;

            return IsNewer(newVersion, encumbentVersion) ? newCandidate : encumbentCandidate;
        }

        private bool IsNewer(string version1, string version2)
        {
            string[] v1 = version1.Split('.');
            string[] v2 = version1.Split('.');

            for (int i = 0; i < v1.Length; i++)
            {
                if (i == version2.Length) // ran out of comparisons, got to the end of what we care about, must be ok.
                    return true;

                int A;
                int M;

                if (!Int32.TryParse(v1[i], out A))
                    return false; // only handling #'s right now.

                if (!Int32.TryParse(v2[i], out M))
                    return false; // only handling #'s right now.

                if (A < M)
                    return false; // if this one doesn't meet, fail.
                if (A > M)
                    return true; // if this one exceeds, win.

            }
            return true;

        }

        unsafe public virtual string GetToolVersion(string FileName)
        {
            int handle;
            // Figure out how much version info there is:
            int size = NativeMethods.GetFileVersionInfoSize(FileName, out handle);

            if (0 == size)
                return null;

            byte[] buffer = new byte[size];

            if (!NativeMethods.GetFileVersionInfo(FileName, handle, size, buffer))
                return null;

            short* subBlock;
            uint len;
            // Get the locale info from the version info:
            if (!NativeMethods.VerQueryValue(buffer, @"\VarFileInfo\Translation", out subBlock, out len))
                return null;

            string spv = @"\StringFileInfo\" + subBlock[0].ToString("X4") + subBlock[1].ToString("X4") + @"\ProductVersion";

            // Get the ProductVersion value for this program:
            string versionInfo;

            return !NativeMethods.VerQueryValue(buffer, spv, out versionInfo, out len) ? null : versionInfo;
        }

        public static ExecutableInfo GetExeType(string filename)
        {
            using (FileStream s = File.OpenRead(filename))
            {
                byte[] buffer = new byte[4096];
                int iRead = s.Read(buffer, 0, 4096);

                unsafe
                {
                    fixed (byte* p_Data = buffer)
                    {
                        IMAGE_DOS_HEADER* idh = (IMAGE_DOS_HEADER*)p_Data;
                        IMAGE_NT_HEADERS32* inhs = (IMAGE_NT_HEADERS32*)(idh->e_lfanew + p_Data);
                        // MachineType m_MachineType = (MachineType)inhs->FileHeader.Machine;

                        if (inhs->OptionalHeader.Magic == 0x20b)
                        {
                            if (((IMAGE_NT_HEADERS64*)inhs)->OptionalHeader.DataDirectory.Size > 0)
                                return ExecutableInfo.x64 | ExecutableInfo.managed;
                            return ExecutableInfo.x64 | ExecutableInfo.native;
                        }
                        if (inhs->OptionalHeader.DataDirectory.Size > 0)
                            return ExecutableInfo.x86 | ExecutableInfo.managed;
                        return ExecutableInfo.x86 | ExecutableInfo.native;
                    }
                }
            }
        }
    }
}
