//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace CoApp.Toolkit.PackageFormatHandlers {
    using System;

    internal interface IPackageFormatHandler {
        // void Install(string packagePath, Action<int> progress = null);
        void Install(CoApp.Toolkit.Engine.Package package, Action<int> progress = null);
        // void Remove(string packagePath, Action<int> progress = null);
        void Remove(CoApp.Toolkit.Engine.Package package, Action<int> progress = null);
        bool IsInstalled(string productCode);
    }
}
