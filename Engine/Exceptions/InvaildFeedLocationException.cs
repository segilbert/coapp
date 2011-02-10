using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Toolkit.Engine.Exceptions {
    public class InvaildFeedLocationException : Exception {
        public string Location;
        public InvaildFeedLocationException(string location) {
            Location = location;
        }
    }
}
