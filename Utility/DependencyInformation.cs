//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Utility {
    public class DependencyInformation {
        public string Status { get; set; }
        public string Module { get; set; }
        public string Filename{ get; set; }
        public string FileTimeStamp { get; set; }
        public string LinkTimeStamp { get; set; }
        public string FileSize { get; set; }
        public string Attr { get; set; }
        public string LinkChecksum { get; set; }
        public string RealChecksum { get; set; }
        public string CPU { get; set; }
        public string Subsystem { get; set; }
        public string Symbols { get; set; }
        public string PreferredBase { get; set; }
        public string ActualBase { get; set; }
        public string VirtualSize { get; set; }
        public string LoadOrder { get; set; }
        public string FileVer { get; set; }
        public string ProductVer { get; set; }
        public string ImageVer { get; set; }
        public string LinkerVer { get; set; }
        public string OSVer { get; set; }
        public string SubsystemVer { get; set; }
    }
}