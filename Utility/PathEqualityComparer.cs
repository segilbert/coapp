//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Eric Schultz. All rights reserved.
// </copyright>
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
