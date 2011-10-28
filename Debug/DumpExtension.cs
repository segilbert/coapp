//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Debug {
    using System.Diagnostics;
    using System.IO;
    using Extensions;
    using LINQPad;

    public static class DumpExtension {
        public static T Dump<T>(this T o) {
            var localUrl = "LinqPadDump.html".GenerateTemporaryFilename();
            using (var writer = Util.CreateXhtmlWriter(true, 100)) {
                writer.Write(o);
                File.WriteAllText(localUrl, writer.ToString());
            }
            Process.Start(localUrl);
            return o;
        }
    }
}