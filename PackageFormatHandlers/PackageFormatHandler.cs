//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace CoApp.Toolkit.PackageFormatHandlers {
    using System;
    using System.Collections.Generic;
    using Engine;

    internal interface IPackageFormatHandler {
        void Install(Package package, Action<int> progress = null);
        void Remove(Package package, Action<int> progress = null);
        IEnumerable<CompositionRule> GetCompositionRules(Package package);

        bool IsInstalled(string productCode);
    }
}