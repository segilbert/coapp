//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.AzureFS {
    using System.Runtime.InteropServices;

    public class Api {

        public enum Weekday {
            Sunday,
            Monday,
            Tuesday,
            Wednesday,
            Thursday,
            Friday,
            Saturday
        } ;

        [DllExport("Connect")]
        public static int Connect([MarshalAs(UnmanagedType.LPWStr)]string name, [MarshalAs(UnmanagedType.LPWStr)]string password) {
            System.Console.WriteLine("name: {0}", name);
            System.Console.WriteLine("password: {0}", password);
            return (name + password).GetHashCode();
        }

        [DllExport("ConnectA")]
        public static int ConnectA(string name, string password) {
            System.Console.WriteLine("name: {0}", name);
            System.Console.WriteLine("password: {0}", password);
            return (name + password).GetHashCode();
        }

        [DllExport("DayOfWeek")]
        public static Weekday DayOfWeek() {
            return Weekday.Thursday;
        }
    }
}
