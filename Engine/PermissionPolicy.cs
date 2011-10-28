using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Toolkit.Engine {
    using System.Security.Principal;
    using Configuration;
    using Extensions;
    using Win32;

    internal class PermissionPolicy {
        private static RegistryView _policies = PackageManagerSettings.CoAppSettings["Policy"];
        private static SecurityIdentifier _administratorsGroup = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
        internal string Name;
        internal string Description;
        internal IEnumerable<SecurityIdentifier> groups;
        private PermissionPolicy( string name, string description, IEnumerable<WellKnownSidType> defaults ) {
            Name = name;
            Description = description;
            var policies = _policies["#" + name].StringsValue;
            groups = policies.Any() ? policies.Select(each => new SecurityIdentifier(each)) : defaults.Select(each => new SecurityIdentifier(each,null));
        }

        internal static PermissionPolicy Connect = new PermissionPolicy( "Connect", "Allows access to communicate with the CoApp Service", new[] { WellKnownSidType.WorldSid });
        internal static PermissionPolicy EnumeratePackages = new PermissionPolicy( "EnumeratePackages", "Allows access to query the system for installed packages", new[] {  WellKnownSidType.WorldSid });
        internal static PermissionPolicy UpdatePackage = new PermissionPolicy( "UpdatePackage", "Allows a newer version of an package that is currently installed to be installed", new[] { WellKnownSidType.WorldSid });

        internal static PermissionPolicy InstallPackage = new PermissionPolicy( "InstallPackage", "Allows a new package to be installed", new[] {  WellKnownSidType.BuiltinAdministratorsSid });
        internal static PermissionPolicy RemovePackage = new PermissionPolicy( "RemovePackage", "Allows a package to be removed", new[] { WellKnownSidType.BuiltinAdministratorsSid });
        internal static PermissionPolicy ChangeActivePackage = new PermissionPolicy( "ChangeActivePackage", "Allows a user to change which version of a package is the active (default) one", new[] { WellKnownSidType.BuiltinAdministratorsSid});
        internal static PermissionPolicy ChangeRequiredState = new PermissionPolicy( "ChangeRequiredState", "Allows a user to change whether a given package is required (user requested)", new[] { WellKnownSidType.BuiltinAdministratorsSid });
        internal static PermissionPolicy ChangeBlockedState = new PermissionPolicy( "ChangeBlockedState", "Allows a user to change whether a given package is blocked from being upgraded", new[] { WellKnownSidType.BuiltinAdministratorsSid });
        internal static PermissionPolicy EditSystemFeeds = new PermissionPolicy( "EditSystemFeeds", "Allows users to edit remembered feeds for the system", new[] { WellKnownSidType.BuiltinAdministratorsSid});
        internal static PermissionPolicy EditSessionFeeds = new PermissionPolicy( "EditSessionFeeds", "Allows users to edit remembered feeds for the session", new[] {  WellKnownSidType.WorldSid });
        
        internal static PermissionPolicy PauseService = new PermissionPolicy("PauseService", "Allows users to place the CoApp Service into a suspended (paused) state", new[] {WellKnownSidType.BuiltinAdministratorsSid });
        internal static PermissionPolicy StopService = new PermissionPolicy("StopService", "Allows users to stop the CoApp Service", new[] { WellKnownSidType.BuiltinAdministratorsSid });

        /// <summary>
        /// Determines whether the user has access to the policy.
        /// Run this while impersonating the user 
        /// </summary>
        internal bool HasPermission {
            get {
                var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());

                if (WindowsVersionInfo.IsVistaOrBeyond) {
                    // manual check against administrator permissions.
                    if (groups.Contains(_administratorsGroup)) {
                        if (AdminPrivilege.IsProcessElevated()) {
                            return true;
                        }
                    }
                    return groups.Where(each => each != _administratorsGroup).Any(principal.IsInRole);
                }

                return groups.Any(principal.IsInRole);
            }
        }

    }

}
