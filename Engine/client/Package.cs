using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Toolkit.Engine.Client {
    using System.Collections.ObjectModel;

    public class Package {
    public readonly ObservableCollection<Package> Dependencies = new ObservableCollection<Package>();

        public class Party {
            public string Name { get; set; }
            public string Url { get; set; }
            public string Email { get; set; }
        }

        public string CosmeticName {
            get; set; }

        public string ProductCode {
            get;
            set;
        }

        public string Name {
            get;
            set;
        }

        public string Architecture {
            get;
            set;
        }

        public string PublicKeyToken {
            get;
            set;
        }
        public UInt64 Version {
            get;
            set;
        }

        public readonly MultiplexedProperty<string> FeedLocation = new MultiplexedProperty<string>((x, y) => Changed());

        private static object Changed() {
            
            throw new NotImplementedException();
        }

        public readonly MultiplexedProperty<Uri> RemoteLocation = new MultiplexedProperty<Uri>((x, y) => Changed());
        public readonly MultiplexedProperty<string> LocalPackagePath = new MultiplexedProperty<string>((x, y) => Changed(), false);

        public List<PackageAssemblyInfo> Assemblies = new List<PackageAssemblyInfo>();

        public Party Publisher { get; set; }
        public IEnumerable<Party> Contributors { get; set; }

        public bool HasLocalFile {
            get;
            set;
        }

        public bool HasRemoteLocation {
            get;
            set;
        }

        public bool CanSatisfy {
            get;
            set;
        }

        public bool CouldNotDownload {
            get;
            set;
        }

        public bool PackageFailedInstall {
            get;
            set;
        }
    }
}
