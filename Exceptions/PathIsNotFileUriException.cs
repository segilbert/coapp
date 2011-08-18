using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Toolkit.Exceptions {
    public class PathIsNotFileUriException: Exception {
        public string Path;
        public Uri Uri;
        public PathIsNotFileUriException( string path, Uri uri ) {
            Path = path;
            Uri = uri;
        }
    }
}
