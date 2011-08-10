using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Toolkit.Engine {
    /// <summary>
    /// The type of action for this composition rule
    /// </summary>
    /// <remarks></remarks>
    public enum CompositionAction {
        /// <summary>
        /// Create a symlink to a file
        /// </summary>
        SymlinkFile,
        /// <summary>
        /// Create a symlink to a folder
        /// </summary>
        SymlinkFolder,
        /// <summary>
        /// Create a .lnk shortcut to a file
        /// </summary>
        Shortcut,
        
    }

    /// <summary>
    /// A composition rule contains the data to perform package composition
    /// </summary>
    /// <remarks></remarks>
    public class CompositionRule {
        /// <summary>
        /// The package for which this rule is created
        /// </summary>
        private Package _package;
        /// <summary>
        /// a list of the different types of composition actions as a collection of strings.
        /// </summary>
        private static readonly Lazy<IEnumerable<string>> _types = new Lazy<IEnumerable<string>>(() => Enum.GetNames(typeof(CompositionAction)));
        /// <summary>
        /// Determines whether [is composition action] [the specified text].
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns><c>true</c> if [is composition action] [the specified text]; otherwise, <c>false</c>.</returns>
        /// <remarks></remarks>
        public static bool IsCompositionAction(dynamic text) {
            return _types.Value.Contains((string)text, StringComparer.CurrentCultureIgnoreCase);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="CompositionRule"/> class.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <remarks></remarks>
        public CompositionRule(Package package) {
            _package = package;
        }

        /// <summary>
        /// Gets or sets the action.
        /// </summary>
        /// <value>The action.</value>
        /// <remarks></remarks>
        public CompositionAction Action { set; get; }

        /// <summary>
        /// 
        /// </summary>
        private string _location;
        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        /// <value>The location.</value>
        /// <remarks></remarks>
        public string Location {
            set { _location = _package.ResolveVariables(value); }
            get { return _location; }
        }

        /// <summary>
        /// 
        /// </summary>
        private string _target;
        /// <summary>
        /// Gets or sets the target.
        /// </summary>
        /// <value>The target.</value>
        /// <remarks></remarks>
        public string Target {
            set { _target = _package.ResolveVariables(value); }
            get { return _target; }
        }

        /// <summary>
        /// 
        /// </summary>
        private string _parameters;
        /// <summary>
        /// Gets or sets the parameters.
        /// </summary>
        /// <value>The parameters.</value>
        /// <remarks></remarks>
        public string Parameters {
            set { _parameters = _package.ResolveVariables(value); }
            get { return _parameters; }
        }
    }
}
