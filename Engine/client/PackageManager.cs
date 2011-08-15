using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Toolkit.Engine.Client {
    using System.Threading.Tasks;
    using Tasks;

    public class PackageManager {
        internal Task<IEnumerable<Package>> GetInstalledPackages(PackageManagerMessages packageManagerMessages) {
            throw new NotImplementedException();
        }

        public IEnumerable<string> SessionFeedLocations {
            get;
            set;
        }

        internal Task<IEnumerable<Package>> GetPackagesInScanLocations(PackageManagerMessages packageManagerMessages) {
            throw new NotImplementedException();
        }

        public bool Pretend {
            get;
            set;
        }

        public int MaximumPackagesToProcess {
            get;
            set;
        }

        public IEnumerable<string> PackagesAsSpecified {
            get;
            set;
        }

        public IEnumerable<string> PackagesAreUpgradable {
            get;
            set;
        }

        public IEnumerable<string> DoNotScanLocations {
            get;
            set;
        }

        public void FlushCache() {
            throw new NotImplementedException();
        }

        public void EnsureCoAppIsInstalledInPath() {
            throw new NotImplementedException();
        }

        public void RunCompositionOnInstlledPackages() {
            throw new NotImplementedException();
        }

        public Task  RemovePackages(IEnumerable<string> parameters, object packageManagerMessages) {
            throw new NotImplementedException();
        }

        public Task InstallPackages(IEnumerable<Package> packages, MessageHandlers messageHandlers = null) {
           throw new NotImplementedException();
        }

        public Task InstallPackages(IEnumerable<string> packageMasks, MessageHandlers messageHandlers = null) {
            throw new NotImplementedException();
        }

        public Task Upgrade(IEnumerable<string> packageList, MessageHandlers messageHandlers = null) {
              throw new NotImplementedException();
        }

        public void GenerateAtomFeed(string _feedOutputFile, string _feedPackageSource, bool _feedRecursive, string _feedRootUrl, string _feedPackageUrl, string _feedActualUrl, string _feedTitle) {
            throw new NotImplementedException();
        }

         public Task AddSystemFeeds(IEnumerable<string> feedLocations, MessageHandlers messageHandlers = null) {
            throw new NotImplementedException();
        }

        public Task DeleteSystemFeeds(IEnumerable<string> feedLocations, MessageHandlers messageHandlers = null) {
            throw new NotImplementedException();
        }

        public IEnumerable<string> SystemFeedLocations{ get; set; }
    }
}
