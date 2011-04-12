using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Toolkit.Engine {
    public enum CompositionAction {
        SymlinkFile,
        SymlinkFolder,
        Shortcut,
        
    }

    public class CompositionRule {
        public CompositionAction Action { set; get; }
        public string Location { set; get; }
        public string Target { set; get; }
        public string Parameters { set; get; }
    }
}
