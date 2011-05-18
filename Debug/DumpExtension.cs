//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Debug {
    using System.Diagnostics;
    using System.IO;
    using LINQPad;

    public static class DumpExtension {
        public static T Dump<T>(this T o) {
            var localUrl = Path.GetTempFileName() + ".html";
            using (var writer = Util.CreateXhtmlWriter(true)) {
                writer.Write(o);
                File.WriteAllText(localUrl, writer.ToString());
            }
            Process.Start(localUrl);
            return o;
        }
    }
}