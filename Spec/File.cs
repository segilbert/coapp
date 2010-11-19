//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------


namespace CoApp.Toolkit.Spec {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;

    public partial class File {
        [XmlIgnore]
        public string RelativeFilePath { 
            get { return Path.Combine(location, name); }
        set { 
            name = Path.GetFileName(value);
            extension = Path.GetExtension(value);
            location = Path.GetDirectoryName(value);
        }
        }
    }

}
