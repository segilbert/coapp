﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.1
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace CoApp.CLI.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("CoApp.CLI.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to To the force the use the version given, use --as-specified={0} .
        /// </summary>
        internal static string AsSpecifiedHint {
            get {
                return ResourceManager.GetString("AsSpecifiedHint", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to To automatically accept the latest version, use --upgrade={0}.
        /// </summary>
        internal static string AutoAcceptHint {
            get {
                return ResourceManager.GetString("AutoAcceptHint", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Could not download.
        /// </summary>
        internal static string CouldNotDownload {
            get {
                return ResourceManager.GetString("CouldNotDownload", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to Install.
        /// </summary>
        internal static string FailedToInstall {
            get {
                return ResourceManager.GetString("FailedToInstall", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Use --help for command line assistance..
        /// </summary>
        internal static string ForCommandLineHelp {
            get {
                return ResourceManager.GetString("ForCommandLineHelp", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Usage:
        ///-------
        ///
        ///CoApp [options] &lt;command&gt; &lt;parameters&gt;
        ///
        ///Options:
        ///--------
        ///    --help                  this help
        ///    --nologo                don&apos;t display the logo
        ///    --load-config=&lt;file&gt;    loads configuration from &lt;file&gt;
        ///    --verbose               prints verbose messages
        ///
        ///    --pretend               doesn&apos;t actually alter the system
        ///
        ///    --as-specified[=&lt;pkg&gt;]  Install the specific package(s) specified 
        ///                            even if a newer version is available. 
        ///
        ///    --upgrade[=&lt;p [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string HelpText {
            get {
                return ResourceManager.GetString("HelpText", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Command &apos;install&apos; requires at least one package..
        /// </summary>
        internal static string InstallRequiresPackageName {
            get {
                return ResourceManager.GetString("InstallRequiresPackageName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The given file is not a valid package [{0}].
        /// </summary>
        internal static string InvalidPackage {
            get {
                return ResourceManager.GetString("InvalidPackage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Missing command..
        /// </summary>
        internal static string MissingCommand {
            get {
                return ResourceManager.GetString("MissingCommand", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Command &apos;list&apos; requires a parameter: either &apos;packages&apos; or &apos;repo&apos;..
        /// </summary>
        internal static string MissingParameterForList {
            get {
                return ResourceManager.GetString("MissingParameterForList", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Missing {0} {1}.
        /// </summary>
        internal static string MissingPkgText {
            get {
                return ResourceManager.GetString("MissingPkgText", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Option {0} requires a location..
        /// </summary>
        internal static string OptionRequiresLocation {
            get {
                return ResourceManager.GetString("OptionRequiresLocation", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The package {0} can not be installed because the following dependencies can not be installed:.
        /// </summary>
        internal static string PackageDependenciesCantInstall {
            get {
                return ResourceManager.GetString("PackageDependenciesCantInstall", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Package {0} failed to install.
        /// </summary>
        internal static string PackageFailedInstall {
            get {
                return ResourceManager.GetString("PackageFailedInstall", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The specified package {0} has multiple matches:.
        /// </summary>
        internal static string PackageHasMultipleMatches {
            get {
                return ResourceManager.GetString("PackageHasMultipleMatches", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Package {0} can be possibly upgraded to a newer version..
        /// </summary>
        internal static string PackageHasPossibleNewerVersion {
            get {
                return ResourceManager.GetString("PackageHasPossibleNewerVersion", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Package not found: {0}.
        /// </summary>
        internal static string PackageNotFound {
            get {
                return ResourceManager.GetString("PackageNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Command &apos;remove&apos; requires at least one package..
        /// </summary>
        internal static string RemoveRequiresPackageName {
            get {
                return ResourceManager.GetString("RemoveRequiresPackageName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The following newer packages can supercede the package:.
        /// </summary>
        internal static string TheFollowingPackageSupercede {
            get {
                return ResourceManager.GetString("TheFollowingPackageSupercede", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unknown command [{0}].
        /// </summary>
        internal static string UnknownCommand {
            get {
                return ResourceManager.GetString("UnknownCommand", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unknown parameter [--{0}].
        /// </summary>
        internal static string UnknownParameter {
            get {
                return ResourceManager.GetString("UnknownParameter", resourceCulture);
            }
        }
    }
}