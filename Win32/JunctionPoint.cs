//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Changes Copyright (c) 2011 Garrett Serack . All rights reserved.
//     JunctionPoint Original Code from http://www.codeproject.com/KB/files/JunctionPointsNet.aspx
// </copyright>
//-----------------------------------------------------------------------


namespace CoApp.Toolkit.Win32 {
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using Microsoft.Win32.SafeHandles;

    public static class JunctionPoint {
       

        /// <summary>
        ///   Creates a junction point from the specified directory to the specified target directory.
        /// </summary>
        /// <remarks>
        ///   Only works on NTFS.
        /// </remarks>
        /// <param name = "junctionPoint">The junction point path</param>
        /// <param name = "targetDir">The target directory</param>
        /// <param name = "overwrite">If true overwrites an existing reparse point or empty directory</param>
        /// <exception cref = "IOException">Thrown when the junction point could not be created or when
        ///   an existing directory was found and <paramref name = "overwrite" /> if false</exception>
        public static void Create(string junctionPoint, string targetDir, bool overwrite) {
            ReparsePoint.CreateJunction(junctionPoint, targetDir);
            /*
            targetDir = Path.GetFullPath(targetDir);

            if (!Directory.Exists(targetDir)) {
                throw new IOException("Target path does not exist or is not a directory.");
            }

            if (Directory.Exists(junctionPoint)) {
                if (!overwrite) {
                    throw new IOException("Directory already exists and overwrite parameter is false.");
                }
            }
            else {
                Directory.CreateDirectory(junctionPoint);
            }
            /*
            using (var handle = ReparsePoint.Open(junctionPoint, NativeFileAccess.GenericWrite)) {
                var targetDirBytes = Encoding.Unicode.GetBytes(ReparsePoint.NonInterpretedPathPrefix + Path.GetFullPath(targetDir));

                var reparseDataBuffer = new ReparsePoint {
                    ReparseTag = ReparsePoint.IO_REPARSE_TAG_MOUNT_POINT,
                    ReparseDataLength = (ushort) (targetDirBytes.Length + 12),
                    SubstituteNameOffset = 0,
                    SubstituteNameLength = (ushort) targetDirBytes.Length,
                    PrintNameOffset = (ushort) (targetDirBytes.Length + 2),
                    PrintNameLength = 0,
                    PathBuffer = new byte[0x3ff0]
                };

                Array.Copy(targetDirBytes, reparseDataBuffer.PathBuffer, targetDirBytes.Length);

                var inBufferSize = Marshal.SizeOf(reparseDataBuffer);
                var inBuffer = Marshal.AllocHGlobal(inBufferSize);

                try {
                    Marshal.StructureToPtr(reparseDataBuffer, inBuffer, false);

                    int bytesReturned;
                    var result = Kernel32.DeviceIoControl(handle.DangerousGetHandle(), ReparsePoint.FSCTL_SET_REPARSE_POINT,
                        inBuffer, targetDirBytes.Length + 20, IntPtr.Zero, 0, out bytesReturned, IntPtr.Zero);

                    if (!result) {
                        ThrowLastWin32Error("Unable to create junction point.");
                    }
                }
                finally {
                    Marshal.FreeHGlobal(inBuffer);
                }
            }
             * */
        }

        /// <summary>
        ///   Deletes a junction point at the specified source directory along with the directory itself.
        ///   Does nothing if the junction point does not exist.
        /// </summary>
        /// <remarks>
        ///   Only works on NTFS.
        /// </remarks>
        /// <param name = "junctionPoint">The junction point path</param>
        public static void Delete(string junctionPoint) {
            if (!Directory.Exists(junctionPoint)) {
                if (File.Exists(junctionPoint)) {
                    throw new IOException("Path is not a junction point.");
                }

                return;
            }
            /*
            using (SafeFileHandle handle = ReparsePoint.Open(junctionPoint, NativeFileAccess.GenericWrite)) {
                var reparseDataBuffer = new ReparsePoint {
                    ReparseTag = ReparsePoint.IO_REPARSE_TAG_MOUNT_POINT,
                    ReparseDataLength = 0,
                    PathBuffer = new byte[0x3ff0]
                };

                var inBufferSize = Marshal.SizeOf(reparseDataBuffer);
                var inBuffer = Marshal.AllocHGlobal(inBufferSize);
                try {
                    Marshal.StructureToPtr(reparseDataBuffer, inBuffer, false);

                    int bytesReturned;
                    var result = Kernel32.DeviceIoControl(handle.DangerousGetHandle(), ReparsePoint.FSCTL_DELETE_REPARSE_POINT,
                        inBuffer, 8, IntPtr.Zero, 0, out bytesReturned, IntPtr.Zero);

                    if (!result) {
                        ThrowLastWin32Error("Unable to delete junction point.");
                    }
                }
                finally {
                    Marshal.FreeHGlobal(inBuffer);
                }

                try {
                    Directory.Delete(junctionPoint);
                }
                catch (IOException ex) {
                    throw new IOException("Unable to delete junction point.", ex);
                }
            }*/

        }

        /// <summary>
        ///   Determines whether the specified path exists and refers to a junction point.
        /// </summary>
        /// <param name = "path">The junction point path</param>
        /// <returns>True if the specified path represents a junction point</returns>
        /// <exception cref = "IOException">Thrown if the specified path is invalid
        ///   or some other error occurs</exception>
        public static bool IsPathJunction(string path) {
            if (!Directory.Exists(path)) {
                return false;
            }
            return ReparsePoint.IsReparsePoint(path) && ReparsePoint.Open(path).IsSymlinkOrJunction;
        }

        /// <summary>
        ///   Gets the target of the specified junction point.
        /// </summary>
        /// <remarks>
        ///   Only works on NTFS.
        /// </remarks>
        /// <param name = "junctionPoint">The junction point path</param>
        /// <returns>The target of the junction point</returns>
        /// <exception cref = "IOException">Thrown when the specified path does not
        ///   exist, is invalid, is not a junction point, or some other error occurs</exception>
        public static string GetTarget(string junctionPoint) {
            if( !ReparsePoint.IsReparsePoint(junctionPoint) ) {
                throw new IOException("Path is not a junction point.");
            }
            return ReparsePoint.Open(junctionPoint).SubstituteName;
        }
       
        private static void ThrowLastWin32Error(string message) {
            throw new IOException(message, Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
        }
    }
}