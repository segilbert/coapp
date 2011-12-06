namespace CoApp.Toolkit.OData {
    using System;
    using System.Collections.Generic;
    using System.Data.Services;
    using System.Data.Services.Common;
    using System.Linq;
    using Extensions;

    public class PackageService {
        private static DataServiceHost host;

        public static void Start(string baseAddress = "http://localhost:8192/PackageService/pkg.svc") {
            host = new DataServiceHost(typeof (PackageServiceImpl), new[] {new Uri(baseAddress)});
            host.Open();
        }

        public static void Stop() {
            if (host != null) {
                host.Close();
            }
        }
    }

    public class PackageServiceImpl : DataService<PackageDataSource> {
        // This method is called only once to initialize service-wide policies.  
        public static void InitializeService(DataServiceConfiguration config) {
            config.SetEntitySetAccessRule("*", EntitySetRights.AllRead);
            config.SetServiceOperationAccessRule("*", ServiceOperationRights.AllRead);
            config.MaxResultsPerCollection = 10000;
            config.DataServiceBehavior.MaxProtocolVersion = DataServiceProtocolVersion.V2;
            config.UseVerboseErrors = true;
        }
    }

    [EntityPropertyMapping("Name", SyndicationItemProperty.Title, SyndicationTextContentKind.Plaintext, true)]
    [EntityPropertyMapping("PublisherName", SyndicationItemProperty.AuthorName, SyndicationTextContentKind.Plaintext, true)]
    [EntityPropertyMapping("PublisherLocation", SyndicationItemProperty.AuthorUri, SyndicationTextContentKind.Plaintext, true)]
    [EntityPropertyMapping("PackageDescription", SyndicationItemProperty.Summary, SyndicationTextContentKind.Html, true)]
    
    [DataServiceKey("PackageId")]
    public class Package {
        private string _packageDescription;

        public Guid PackageId { get; set; }
        public string Name { get; set; }
        public string Architecture { get; set; }
        public Int64 VersionLong { get; set; }
        public string PublisherName { get; set; }
        public string PublisherLocation { get; set; }
        public string PublicKeyToken { get; set; }
        public Int64 BindingPolicyMinVersion { get; set; }
        public Int64 BindingPolicyMaxVersion { get; set; }

        public string Dependencies { get; set; }
        public string PackageDescription {
            get { return "{0}<br/><br/><a href='{1}'>Package Download</a>".format(_packageDescription, PackageFile); }
            set { _packageDescription = value; }
        }

        public string PackageFile { get; set; }
        public Package[] PackageDependencies { get; set; }
    }

    public class PackageDataSource {
        private readonly List<Package> _samplePackageRecordList;

        public PackageDataSource() {
            _samplePackageRecordList = new List<Package>();

            for (byte i = 0; i < 100; i++) {
                var pr = new Package {
                    PackageId = new Guid(i, i, i, i, i, i, i, i, i, i, i),
                    // eeeew
                    // PackageId = i,
                    Name = string.Format("Name{0}", i),
                    Architecture = string.Format("x{0}", i%1 == 0 ? "86" : "64"),

                    VersionLong = 1L << 48,
                    PublisherName = "PublisherName #{0}".format(i),
                    PublisherLocation = "http://foo.coapp.org/publishernumber{0}".format(i),
                    PublicKeyToken = "1231231231231231",
                    PackageDescription =
                        "<html><head><title>title foo</title></head><body>This is the text of the package description <a href='http://coapp.org'>a link</a></body></html>",
                    PackageFile = "http://foo/bar.zip"
                };
                if (i == 2) {
                    var deps = new[] {_samplePackageRecordList[0]};
                    pr.PackageDependencies = deps;


                    pr.Dependencies = string.Join("|", pr.PackageDependencies.Select(p => p.PackageId.ToString()));
                }
                _samplePackageRecordList.Add(pr);
            }
        }

        public IQueryable<Package> Packages {
            get { return _samplePackageRecordList.AsQueryable(); }
        }
    }
}