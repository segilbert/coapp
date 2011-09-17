using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Bootstrapper {
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Windows.Media.Imaging;

    public static class NativeResources {
        private const int LOAD_LIBRARY_AS_DATAFILE = 0x00000002;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

        [DllImport("kernel32.dll")]
        private static extern IntPtr FindResource(IntPtr hModule, int lpID, string lpType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int SizeofResource(IntPtr hModule, IntPtr hResInfo);

        public static MemoryStream GetBinaryResource(int resourceId) {
            var hMod = LoadLibraryEx("coapp.resources.dll", IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE); 
            var hRes = FindResource(hMod, resourceId, "BINARY"); 
            var size = SizeofResource(hMod, hRes); 
            var pt = LoadResource(hMod, hRes);
            var buffer = new byte[size];
            Marshal.Copy(pt, buffer, 0, (int)size);
            var m = new MemoryStream(buffer);
            return m;
        }

        public static BitmapImage GetBitmapImage(int resourceId) {
            var image = new BitmapImage();
            image.BeginInit();
            image.StreamSource = NativeResources.GetBinaryResource(resourceId);
            image.EndInit();
            return image;
        }
    }
}
