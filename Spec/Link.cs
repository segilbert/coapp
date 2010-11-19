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

    public partial class Link {
        [XmlIgnore]
        public string Extension { get { return Path.GetExtension(output); }}

        [XmlIgnore]
        public string Name { get { return Path.GetFileNameWithoutExtension(output); } }
    }

}
