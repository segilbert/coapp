//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Eric Schultz. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Utility
{
    using System.Collections.Generic;
    using Toolkit.Extensions;
    using System.IO;

    public class PathEqualityComparer : IEqualityComparer<string>
    {
        //public static PathEqualityComparer StatPathComp = new PathEqualityComparer();
        public bool Equals(string x, string y)
        {
            var xPath = x.GetFullPath();
            var yPath = y.GetFullPath();
            return string.Compare(xPath, yPath, System.StringComparison.InvariantCultureIgnoreCase) == 0;
        }

        public int GetHashCode(string obj)
        {
            return obj.GetFullPath().ToUpperInvariant().GetHashCode();
        }
    }
}
