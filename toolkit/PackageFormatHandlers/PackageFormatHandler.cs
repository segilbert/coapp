﻿//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------
namespace CoApp.Toolkit.PackageFormatHandlers {
    using System;
    using System.Collections.Generic;
    using Engine;

    internal interface IPackageFormatHandler {
        void Install(Package package, Action<int> progress = null);
        void Remove(Package package, Action<int> progress = null);
        Composition GetCompositionData(Package package);

        bool IsInstalled(Guid productCode);
    }
}