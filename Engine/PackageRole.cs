//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine {
    /// <summary>
    /// Different types of package roles
    /// </summary>
    /// <remarks></remarks>
    public enum PackageRole {
        /// <summary>
        /// Shared Library (.NET Assembly, or native DLL)
        /// </summary>
        SharedLib,
        
        /// <summary>
        /// Developer Library (.NET assembly or .lib/.h files)
        /// </summary>
        DeveloperLib,
        
        /// <summary>
        /// Source Code MSI
        /// </summary>
        Source,
        
        /// <summary>
        /// Application (binaries, etc)
        /// </summary>
        Application
    }
}