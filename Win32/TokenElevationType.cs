//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Win32 {
    /// <summary>
    /// The TOKEN_ELEVATION_TYPE enumeration indicates the elevation type of 
    /// token being queried by the GetTokenInformation function or set by 
    /// the SetTokenInformation function.
    /// </summary>
    public enum TokenElevationType {
        TokenElevationTypeDefault = 1,
        TokenElevationTypeFull,
        TokenElevationTypeLimited
    }
}