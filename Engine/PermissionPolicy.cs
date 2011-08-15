using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Toolkit.Engine {
    using System.Security.Principal;
    using Extensions;
    using Win32;

    internal class PermissionPolicy {
        internal string Name;
        internal string Description;
        internal IEnumerable<string> groups;
        private PermissionPolicy( string name, string description, IEnumerable<string> defaults ) {
            Name = name;
            Description = description;
            groups = defaults;
        }

        internal static PermissionPolicy Connect = new PermissionPolicy( "Connect", "Allows access to communicate with the CoApp Service", new[] { "Everyone" });
        internal static PermissionPolicy EnumeratePackages = new PermissionPolicy( "EnumeratePackages", "Allows access to query the system for installed packages", new[] { "Everyone" });
        internal static PermissionPolicy UpdatePackage = new PermissionPolicy( "UpdatePackage", "Allows a newer version of an package that is currently installed to be installed", new[] { "Everyone" });

        internal static PermissionPolicy InstallPackage = new PermissionPolicy( "InstallPackage", "Allows a new package to be installed", new[] { "Administrators" });
        internal static PermissionPolicy RemovePackage = new PermissionPolicy( "RemovePackage", "Allows a package to be removed", new[] { "Administrators" });
        internal static PermissionPolicy ChangeActivePackage = new PermissionPolicy( "ChangeActivePackage", "Allows a user to change which version of a package is the active (default) one", new[] { "Administrators" });
        internal static PermissionPolicy ChangeRequiredState = new PermissionPolicy( "ChangeRequiredState", "Allows a user to change whether a given package is required (user requested)", new[] { "Administrators" });
        internal static PermissionPolicy ChangeBlockedState = new PermissionPolicy( "ChangeBlockedState", "Allows a user to change whether a given package is blocked from being upgraded", new[] { "Administrators" });
        internal static PermissionPolicy EditFeeds = new PermissionPolicy( "EditFeeds", "Allows users to edit remembered feeds", new[] { "Administrators" });

        /// <summary>
        /// Determines whether the user has access to the policy.
        /// Run this while impersonating the user 
        /// </summary>
        internal bool HasPermission {
            get {
                // manual check against administrator permissions.
                if( groups.ContainsIgnoreCase("Administrators") ) {
                    if(AdminPrivilege.IsProcessElevated()) {
                        return true;
                    }
                }
                var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
                return (from g in groups where g.Equals("administrators", StringComparison.CurrentCultureIgnoreCase) select g).Any(principal.IsInRole);
            }
        }

    }

}
