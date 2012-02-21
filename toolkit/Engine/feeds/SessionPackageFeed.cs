using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Toolkit.Engine.Feeds {
    using Extensions;
    using Tasks;

    internal class SessionPackageFeed  : PackageFeed {
        internal static string CanonicalLocation = "CoApp://SessionPackages";

        internal static SessionPackageFeed Instance { get {
            return SessionCache<SessionPackageFeed>.Value[CanonicalLocation] ??
                (SessionCache<SessionPackageFeed>.Value[CanonicalLocation] = new SessionPackageFeed());
        }}

        /// <summary>
        /// contains the list of packages in the direcory. (may be recursive)
        /// </summary>
        private readonly List<Package> _packageList = new List<Package>();

        private SessionPackageFeed() : base(CanonicalLocation) {
            Scanned = true;
            LastScanned = DateTime.Now;
        }

        internal void Add(Package package) {
            if( !_packageList.Contains(package)) {
                _packageList.Add(package);
                Scanned = true;
                LastScanned = DateTime.Now;
                NewPackageManager.Instance.Updated();
            }
        }

        /// <summary>
        /// Finds packages based on the canonical details of the package.
        /// 
        /// Supports wildcard in pattern match.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <param name="arch"></param>
        /// <param name="publicKeyToken"></param>
        /// <param name="packageFilter">The package filter.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        internal override IEnumerable<Package> FindPackages(string name, string version, string arch, string publicKeyToken) { 
            return from p in _packageList where 
                (string.IsNullOrEmpty(name) || p.Name.IsWildcardMatch(name)) &&
                (string.IsNullOrEmpty(version) || p.Version.ToString().IsWildcardMatch(version)) &&
                (string.IsNullOrEmpty(arch) || p.Architecture.ToString().IsWildcardMatch(arch)) &&
                (string.IsNullOrEmpty(publicKeyToken) || p.PublicKeyToken.IsWildcardMatch(publicKeyToken)) select p;        }
    }
}
