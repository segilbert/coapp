//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Win32 {
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Security.AccessControl;
    using System.Text;
    using System.Text.RegularExpressions;
    using Console;
    using Exceptions;
    using Microsoft.Win32.SafeHandles;
    
    public class ReparsePoint  {

        [StructLayout(LayoutKind.Sequential)]
        private struct ReparseBuffer {
            /// <summary>
            /// Reparse point tag. 
            /// </summary>
            public uint ReparseTag;

            /// <summary>
            /// Size, in bytes, of the data after the Reserved member. This can be calculated by:
            /// (4 * sizeof(ushort)) + SubstituteNameLength + PrintNameLength + 
            /// (namesAreNullTerminated ? 2 * sizeof(char) : 0);
            /// </summary>
            public ushort ReparseDataLength;

            /// <summary>
            /// Reserved. do not use. 
            /// </summary>
            public ushort Reserved;

            /// <summary>
            /// Offset, in bytes, of the substitute name string in the PathBuffer array.
            /// </summary>
            public ushort SubstituteNameOffset;

            /// <summary>
            /// Length, in bytes, of the substitute name string. If this string is null-terminated,
            /// SubstituteNameLength does not include space for the null character.
            /// </summary>
            public ushort SubstituteNameLength;

            /// <summary>
            /// Offset, in bytes, of the print name string in the PathBuffer array.
            /// </summary>
            public ushort PrintNameOffset;

            /// <summary>
            /// Length, in bytes, of the print name string. If this string is null-terminated,
            /// PrintNameLength does not include space for the null character. 
            /// </summary>
            public ushort PrintNameLength;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x3FF0)]
            public byte[] PathBuffer;

        }

        /// <summary>
        ///   The file or directory is not a reparse point.
        /// </summary>
        public const int ERROR_NOT_A_REPARSE_POINT = 4390;

        /// <summary>
        ///   The reparse point attribute cannot be set because it conflicts with an existing attribute.
        /// </summary>
        public const int ERROR_REPARSE_ATTRIBUTE_CONFLICT = 4391;

        /// <summary>
        ///   The data present in the reparse point buffer is invalid.
        /// </summary>
        public const int ERROR_INVALID_REPARSE_DATA = 4392;

        /// <summary>
        ///   The tag present in the reparse point buffer is invalid.
        /// </summary>
        public const int ERROR_REPARSE_TAG_INVALID = 4393;

        /// <summary>
        ///   There is a mismatch between the tag specified in the request and the tag present in the reparse point.
        /// </summary>
        public const int ERROR_REPARSE_TAG_MISMATCH = 4394;

        /// <summary>
        ///   Command to set the reparse point data block.
        /// </summary>
        public const int FSCTL_SET_REPARSE_POINT = 0x000900A4;

        /// <summary>
        ///   Command to get the reparse point data block.
        /// </summary>
        public const int FSCTL_GET_REPARSE_POINT = 0x000900A8;

        /// <summary>
        ///   Command to delete the reparse point data base.
        /// </summary>
        public const int FSCTL_DELETE_REPARSE_POINT = 0x000900AC;

        /// <summary>
        ///   Reparse point tag used to identify mount points and junction points.
        /// </summary>
        public const uint IO_REPARSE_TAG_MOUNT_POINT = 0xA0000003;

        /// <summary>
        ///   Reparse point tag used to identify symlinks
        /// </summary>
        public const uint IO_REPARSE_TAG_SYMLINK = 0xA000000C;

        /// <summary>
        ///   This prefix indicates to NTFS that the path is to be treated as a non-interpreted
        ///   path in the virtual file system.
        /// </summary>
        public const string NonInterpretedPathPrefix = @"\??\";

        public static Regex UncPrefixRx = new Regex(@"\\\?\?\\UNC\\");
        public static Regex DrivePrefixRx = new Regex(@"\\\?\?\\[a-z,A-Z]\:\\");
        public static Regex VolumePrefixRx = new Regex(@"\\\?\?\\Volume");

        private ReparseBuffer reparseDataBuffer;

        private ReparsePoint(IntPtr buffer) {
            if( buffer == IntPtr.Zero )
                throw new ArgumentNullException("buffer");

            reparseDataBuffer = (ReparseBuffer)Marshal.PtrToStructure(buffer, typeof(ReparseBuffer));
        }

        private static SafeFileHandle OpenFile(string reparsePoint, NativeFileAccess accessMode) {
            var reparsePointHandle = Kernel32.CreateFile(reparsePoint, accessMode,
                FileShare.Read | FileShare.Write | FileShare.Delete ,
                IntPtr.Zero, FileMode.Open, NativeFileAttributesAndFlags.BackupSemantics | NativeFileAttributesAndFlags.OpenReparsePoint,
                IntPtr.Zero);

            if (Marshal.GetLastWin32Error() != 0) {
                throw new IOException("Unable to open reparse point.", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
            }

            return reparsePointHandle;
        }

        public static bool IsReparsePoint(string path) {
            return (File.GetAttributes(path) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
        }

       
        public bool IsSymlinkOrJunction {
            get {
                return (reparseDataBuffer.ReparseTag == IO_REPARSE_TAG_MOUNT_POINT || reparseDataBuffer.ReparseTag == IO_REPARSE_TAG_SYMLINK) && !IsMountPoint;
            }
        }

        public bool IsRelativeSymlink {
            get {
                return reparseDataBuffer.ReparseTag == IO_REPARSE_TAG_SYMLINK ? (reparseDataBuffer.PathBuffer[0] & 1) == 1 : false;
            }
        }

        public bool IsMountPoint {
            get { return VolumePrefixRx.Match(SubstituteName).Success; }
        }


        public string PrintName {
            get {
                var extraOffset = reparseDataBuffer.ReparseTag == IO_REPARSE_TAG_SYMLINK ? 4 : 0;
                return reparseDataBuffer.PrintNameLength > 0 ? Encoding.Unicode.GetString(reparseDataBuffer.PathBuffer, reparseDataBuffer.PrintNameOffset + extraOffset, reparseDataBuffer.PrintNameLength) : string.Empty;
            }
        }

        public string SubstituteName {
            get {
                var extraOffset = reparseDataBuffer.ReparseTag == IO_REPARSE_TAG_SYMLINK ? 4 : 0;
                return reparseDataBuffer.SubstituteNameLength > 0 ? Encoding.Unicode.GetString(reparseDataBuffer.PathBuffer, reparseDataBuffer.SubstituteNameOffset + extraOffset, reparseDataBuffer.SubstituteNameLength) : string.Empty;
            }
        }

        public static string GetActualPath(string linkPath) {
            if (!IsReparsePoint(linkPath)) {
                throw new PathIsNotSymlinkException(linkPath);
            }

            var reparsePoint = Open(linkPath);

            var target = reparsePoint.SubstituteName;

            if (target.StartsWith(NonInterpretedPathPrefix)) {
                if (UncPrefixRx.Match(target).Success) {
                    target = UncPrefixRx.Replace(target, @"\\");
                }

                if (DrivePrefixRx.Match(target).Success) {
                    target = target.Replace(NonInterpretedPathPrefix, "");
                }
            }

            if (reparsePoint.IsRelativeSymlink)
                target = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(linkPath), target));

            return target;
        }

        public static ReparsePoint Open(string path) {
            if(!IsReparsePoint(path) )
                throw new IOException("Path is not reparse point");

            using (var handle = OpenFile(path, NativeFileAccess.GenericRead)) {
                if( handle == null)
                    throw new IOException("Unable to get information about reparse point.");

                var outBufferSize = Marshal.SizeOf(typeof(ReparseBuffer));
                var outBuffer = Marshal.AllocHGlobal(outBufferSize);

                try {
                    int bytesReturned;
                    var result = Kernel32.DeviceIoControl(handle.DangerousGetHandle(), FSCTL_GET_REPARSE_POINT, IntPtr.Zero, 0, outBuffer, outBufferSize, out bytesReturned, IntPtr.Zero);

                    if (!result) {
                        var error = Marshal.GetLastWin32Error();
                        if (error == ERROR_NOT_A_REPARSE_POINT) {
                            throw new IOException("Path is not a reparse point.", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
                        }

                        throw new IOException("Unable to get information about reparse point.", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
                    }
                    return new ReparsePoint(outBuffer);
                }
                finally {
                    Marshal.FreeHGlobal(outBuffer);
                }
            }
        }

        public static ReparsePoint CreateJunction(string path, string targetDirectory) {
            path = Path.GetFullPath(path);
            targetDirectory = Path.GetFullPath(targetDirectory);

            if (!Directory.Exists(targetDirectory)) {
                throw new IOException("Target path does not exist or is not a directory.");
            }

            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }

            using (var handle = OpenFile(path, NativeFileAccess.GenericWrite)) {
                var substituteName = Encoding.Unicode.GetBytes(NonInterpretedPathPrefix + targetDirectory);
                var printName = Encoding.Unicode.GetBytes(targetDirectory);

                var reparseDataBuffer = new ReparseBuffer {
                    ReparseTag = IO_REPARSE_TAG_MOUNT_POINT,
                    
                    SubstituteNameOffset = 0,
                    SubstituteNameLength = (ushort)substituteName.Length,
                    PrintNameOffset = (ushort)(substituteName.Length+2),
                    PrintNameLength = (ushort)printName.Length,
                    PathBuffer = new byte[0x3ff0],
                };

                reparseDataBuffer.ReparseDataLength = (ushort)(reparseDataBuffer.PrintNameLength + reparseDataBuffer.PrintNameOffset + 10);

                Array.Copy(substituteName, reparseDataBuffer.PathBuffer, substituteName.Length);
                Array.Copy(printName, 0, reparseDataBuffer.PathBuffer, reparseDataBuffer.PrintNameOffset , printName.Length);

                var inBufferSize = Marshal.SizeOf(reparseDataBuffer);
                var inBuffer = Marshal.AllocHGlobal(inBufferSize);

                try {
                    Marshal.StructureToPtr(reparseDataBuffer, inBuffer, false);

                    int bytesReturned;
                    var result = Kernel32.DeviceIoControl(handle.DangerousGetHandle(), ReparsePoint.FSCTL_SET_REPARSE_POINT,
                        inBuffer, reparseDataBuffer.ReparseDataLength +8 , IntPtr.Zero, 0, out bytesReturned, IntPtr.Zero);

                    if (!result) {
                        throw new IOException("Unable to create junction point.", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
                    }
                    return Open(path);
                }
                finally {
                    Marshal.FreeHGlobal(inBuffer);
                }
            }
        }

        /* // work in progress... just trying my own brand of symbolic link creation
        public static ReparsePoint CreateSymlink(string path, string targetDirectory) {
            path = Path.GetFullPath(path);
            targetDirectory = Path.GetFullPath(targetDirectory);

            if (!Directory.Exists(targetDirectory)) {
                throw new IOException("Target path does not exist or is not a directory.");
            }

            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }

            using (var handle = OpenFile(path, NativeFileAccess.GenericWrite)) {
                var substituteName = Encoding.Unicode.GetBytes(NonInterpretedPathPrefix + targetDirectory);
                var printName = Encoding.Unicode.GetBytes(targetDirectory);
                var extraOffset = 4;

                var reparseDataBuffer = new ReparseBuffer {
                    ReparseTag = IO_REPARSE_TAG_SYMLINK,

                    SubstituteNameOffset = 0,
                    SubstituteNameLength = (ushort)substituteName.Length,
                    PrintNameOffset = (ushort)(substituteName.Length + 2),
                    PrintNameLength = (ushort)printName.Length,
                    PathBuffer = new byte[0x3ff0],
                };

                reparseDataBuffer.ReparseDataLength = (ushort)(reparseDataBuffer.PrintNameLength + reparseDataBuffer.PrintNameOffset + 10 +extraOffset);

                Array.Copy(substituteName, 0 , reparseDataBuffer.PathBuffer, extraOffset, substituteName.Length);
                Array.Copy(printName, 0, reparseDataBuffer.PathBuffer, reparseDataBuffer.PrintNameOffset + extraOffset, printName.Length);

                var inBufferSize = Marshal.SizeOf(reparseDataBuffer);
                var inBuffer = Marshal.AllocHGlobal(inBufferSize);

                try {
                    Marshal.StructureToPtr(reparseDataBuffer, inBuffer, false);

                    int bytesReturned;
                    var result = Kernel32.DeviceIoControl(handle.DangerousGetHandle(), ReparsePoint.FSCTL_SET_REPARSE_POINT,
                        inBuffer, reparseDataBuffer.ReparseDataLength + 8, IntPtr.Zero, 0, out bytesReturned, IntPtr.Zero);

                    if (!result) {
                        throw new IOException("Unable to create symlink point.", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
                    }
                    return Open(path);
                }
                finally {
                    Marshal.FreeHGlobal(inBuffer);
                }
            }
        }
         */

        public static ReparsePoint ChangeJunctionTarget(string path, string targetDirectory) {
            path = Path.GetFullPath(path);
            targetDirectory = Path.GetFullPath(targetDirectory);
            if(!IsReparsePoint(path)) 
                throw new IOException("Path is not a reparse point.");

            var reparsePoint = Open(path);
            if( reparsePoint.reparseDataBuffer.ReparseTag != IO_REPARSE_TAG_MOUNT_POINT )
                throw new IOException("ChangeJunctionTarget only works on junction mount points.");

            if (!Directory.Exists(targetDirectory)) {
                throw new IOException("Target path does not exist or is not a directory.");
            }

            using (var handle = OpenFile(path, NativeFileAccess.GenericWrite)) {
                var substituteName = Encoding.Unicode.GetBytes(NonInterpretedPathPrefix + targetDirectory);
                var printName = Encoding.Unicode.GetBytes(targetDirectory);
                var extraOffset = reparsePoint.reparseDataBuffer.ReparseTag == IO_REPARSE_TAG_SYMLINK ? 4 : 0;

                reparsePoint.reparseDataBuffer.SubstituteNameOffset = 0;
                reparsePoint.reparseDataBuffer.SubstituteNameLength = (ushort) substituteName.Length;
                reparsePoint.reparseDataBuffer.PrintNameOffset = (ushort) (substituteName.Length + 2);
                reparsePoint.reparseDataBuffer.PrintNameLength = (ushort) printName.Length;
                reparsePoint.reparseDataBuffer.PathBuffer = new byte[0x3ff0];

                reparsePoint.reparseDataBuffer.ReparseDataLength = (ushort)(reparsePoint.reparseDataBuffer.PrintNameLength + reparsePoint.reparseDataBuffer.PrintNameOffset + 10 +extraOffset );

                Array.Copy(substituteName, 0, reparsePoint.reparseDataBuffer.PathBuffer, extraOffset, substituteName.Length);
                Array.Copy(printName, 0, reparsePoint.reparseDataBuffer.PathBuffer, reparsePoint.reparseDataBuffer.PrintNameOffset + extraOffset, printName.Length);

                var inBufferSize = Marshal.SizeOf(reparsePoint.reparseDataBuffer);
                var inBuffer = Marshal.AllocHGlobal(inBufferSize);

                try {
                    Marshal.StructureToPtr(reparsePoint.reparseDataBuffer, inBuffer, false);

                    int bytesReturned;
                    var result = Kernel32.DeviceIoControl(handle.DangerousGetHandle(), ReparsePoint.FSCTL_SET_REPARSE_POINT,
                        inBuffer, reparsePoint.reparseDataBuffer.ReparseDataLength + 8, IntPtr.Zero, 0, out bytesReturned, IntPtr.Zero);

                    if (!result) {
                        throw new IOException("Unable to modify reparse point.", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
                    }
                    return Open(path);
                }
                finally {
                    Marshal.FreeHGlobal(inBuffer);
                }
            }
        }

    }
}