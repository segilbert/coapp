using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Toolkit.Engine {
    using System.Security.Principal;
    using Configuration;
    using Extensions;
    using Toolkit.Exceptions;
    using Win32;

    public class PermissionPolicy {
        private static RegistryView _policies = PackageManagerSettings.CoAppSettings["Policy"];
        private static SecurityIdentifier _administratorsGroup = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
        internal string Name;
        internal string Description;
        internal static IEnumerable<PermissionPolicy> AllPolicies = Enumerable.Empty<PermissionPolicy>();
        private readonly IEnumerable<WellKnownSidType> _defaults;
        private IEnumerable<SecurityIdentifier> _groups;

        private RegistryView policyView;
        private PermissionPolicy( string name, string description, IEnumerable<WellKnownSidType> defaults ) {
            Name = name;
            Description = description;
            _defaults = defaults;
            policyView = _policies["#" + Name];
            Refresh();
            AllPolicies = AllPolicies.UnionSingleItem(this).ToArray();
        }

        internal IEnumerable<string> Sids {
            get { lock (this) { return _groups.Select(each => each.ToString()); } }
        }

        internal IEnumerable<string> Accounts {
            get { lock (this) { return _groups.Select(each => each.Translate(typeof(NTAccount)) as NTAccount).Where(each => each != null).Select(each => each.ToString()); } }
        }

        private void Refresh() {
            var policies = policyView.StringsValue;
            _groups = policies.Any() ? policies.Select(each => new SecurityIdentifier(each)) : _defaults.Select(each => new SecurityIdentifier(each, null));
        }

        private SecurityIdentifier FindSid(string account) {
            SecurityIdentifier sid = null;
            try {
                // first, let's try this as a sid (SDDL) string
                sid = new SecurityIdentifier(account);
                return sid;
            }
            catch {
            }

            try {
                // maybe it's an account/group name
                var name = new NTAccount(account);
                sid = (SecurityIdentifier)name.Translate(typeof(SecurityIdentifier));
                if( sid != null) {
                    return sid;    
                }
            }
            catch {
            }

            throw new UnknownAccountException(account);
        }

        internal void Add( string account ) {
            lock (this) {
                var sid = FindSid(account);
                
                if( !_groups.Contains(sid)) {
                    policyView.StringsValue = _groups.UnionSingleItem(sid).Select(each => each.ToString());
                }
                Refresh();
            }
        }

        internal void Remove(string account) {
            lock (this) {
                if( account == "*") { // reset to default.
                    policyView.StringsValue = null;
                    Refresh();
                    return;
                }

                var sid = FindSid(account);

                if (_groups.Contains(sid)) {     
                    policyView.StringsValue = _groups.Where(each => each != sid).Select(each => each.ToString());
                }
                Refresh();
            }
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
        internal static PermissionPolicy ModifyPolicy = new PermissionPolicy("ModifyPolicy", "Allows users to change policy values for CoApp", new[] { WellKnownSidType.BuiltinAdministratorsSid });
        
        internal static PermissionPolicy Symlink = new PermissionPolicy("Symlink", "Allows users to create and edit symlinks", new[] { WellKnownSidType.BuiltinAdministratorsSid });

        /// <summary>
        /// Determines whether the user has access to the policy.
        /// Run this while impersonating the user 
        /// </summary>
        internal bool HasPermission {
            get {
                var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());

                if (WindowsVersionInfo.IsVistaOrBeyond) {
                    // manual check against administrator permissions.
                    if (_groups.Contains(_administratorsGroup)) {
                        if (AdminPrivilege.IsProcessElevated()) {
                            return true;
                        }
                    }
                    return _groups.Where(each => each != _administratorsGroup).Any(principal.IsInRole);
                }

                return _groups.Any(principal.IsInRole);
            }
        }
    }
}
