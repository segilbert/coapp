﻿//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine.Exceptions {
    using System;
    using System.Collections.Generic;
    using Toolkit.Exceptions;

    internal class PackageHasPotentialUpgradesException : CoAppException {
        internal Package UnsatisfiedPackage;
        internal IEnumerable<Package> SatifactionOptions;

        internal PackageHasPotentialUpgradesException(Package unsatisfiedPackage, IEnumerable<Package> satisfactionOptions) {
            UnsatisfiedPackage = unsatisfiedPackage;
            SatifactionOptions = satisfactionOptions;
        }
    }
}