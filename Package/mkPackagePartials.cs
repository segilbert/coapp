using System.Linq;

namespace CoApp.Toolkit.Package
{

    public partial class FileType : IFileType
    {
        [System.Xml.Serialization.XmlIgnore]
        string IFileType.NameProp
        {
            get { return this.name; }
            set { this.name = value; }
        }

        [System.Xml.Serialization.XmlIgnore]
        string IFileType.ExtProp
        {
            get { return this.ext; }
            set { this.ext = value; }
        }

        [System.Xml.Serialization.XmlIgnore]
        string IFileType.SrcProp
        {
            get { return this.Src; }
        }
    }

    public partial class PolicyAssembly : IFileType
    {
        [System.Xml.Serialization.XmlIgnore]
        string IFileType.NameProp
        {
            get { return this.name; }
            set { this.name = value; }
        }

        [System.Xml.Serialization.XmlIgnore]
        string IFileType.ExtProp
        {
            get { return this.ext; }
            set { this.ext = value; }
        }

        [System.Xml.Serialization.XmlIgnore]
        string IFileType.SrcProp
        {
            get { return this.Src; }
        }
    }

    public partial class Properties
    {
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string IconValueBase64 { get; set;  }
    }

    public partial class Package
    {
        public string AddUrl(string uri, string type)
        {
            var urlAlreadyAdded = (from u in Urls
                                  where u.URL == uri &&
                                        u.Type == type
                                  select u).FirstOrDefault();
            if (urlAlreadyAdded == null)
            {
                var newHuid = new Huid(Name, Version, Arch.ToString(), this.Publisher.PublicKeyToken, uri, type);

                var urlToAdd = new Url() {URL = uri, Type = type, url_guid= (string)newHuid};
                Urls.Add(urlToAdd);
                return (string) newHuid;
            }

            return urlAlreadyAdded.url_guid;

        }
    }
}
