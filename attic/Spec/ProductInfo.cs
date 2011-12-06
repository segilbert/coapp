//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Spec {
    using Scripting.Languages.PropertySheet;

    public class ProductInfo : PropertySheetItem {
        public string ProductName {
            get {
                return Rule["product-name"].AsString();
            }
            set {
                Rule.SetSingleValue("product-name", value);
            }
        }

        public string OriginalSourceLocation {
            get {
                return Rule["original-source-location"].AsString();
            }
            set {
                Rule.SetSingleValue("original-source-location", value);
            }
        }

        public string OriginalSourceWebsite {
            get {
                return Rule["original-source-website"].AsString();
            }
            set {
                Rule.SetSingleValue("original-source-website", value);
            }
        }

        public string OriginalAuthor {
            get {
                return Rule["original-author"].AsString();
            }
            set {
                Rule.SetSingleValue("original-author", value);
            }
        }
        public string OriginalVersion {
            get {
                return Rule["original-version"].AsString();
            }
            set {
                Rule.SetSingleValue("original-version", value);
            }
        }

        public string License {
            get {
                return Rule["license"].AsString();
            }
            set {
                Rule.SetSingleValue("license", value);
            }
        }

        public string Packager {
            get {
                return Rule["packager"].AsString();
            }
            set {
                Rule.SetSingleValue("packager", value);
            }
        }

        public string Publisher {
            get {
                return Rule["publisher"].AsString();
            }
            set {
                Rule.SetSingleValue("publisher", value);
            }
        }

        public string Version {
            get {
                return Rule["version"].AsString();
            }
            set {
                Rule.SetSingleValue("version", value);
            }
        }

        public ProductInfo(Rule rule): base(rule) {
            
        }
    }
}