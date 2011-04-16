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
        private Package _package;
        private static readonly Lazy<IEnumerable<string>> _types = new Lazy<IEnumerable<string>>(() => Enum.GetNames(typeof(CompositionAction)));
        public static bool IsCompositionAction(dynamic text) {
            return _types.Value.Contains((string)text, StringComparer.CurrentCultureIgnoreCase);
        }
        public CompositionRule(Package package) {
            _package = package;
        }

        public CompositionAction Action { set; get; }

        private string _location;
        public string Location {
            set { _location = _package.ResolveVariables(value); }
            get { return _location; }
        }

        private string _target;
        public string Target {
            set { _target = _package.ResolveVariables(value); }
            get { return _target; }
        }

        private string _parameters;
        public string Parameters {
            set { _parameters = _package.ResolveVariables(value); }
            get { return _parameters; }
        }
    }
}
