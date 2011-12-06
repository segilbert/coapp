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
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Exceptions;
    using Extensions;
    using Scripting.Languages.PropertySheet;

    public class SpecFile : PropertySheet {
        private readonly Indexer<ProductInfo> _productInfo;
        private readonly Indexer<Link> _link;

        public readonly Indexer<FileGroup> FileGroups;
        public readonly Indexer<Process> Processes;
        public readonly Indexer<Event> Events;
        public readonly Indexer<Library> Libraries;

        public Link Link {
            get {
                return _link["unique"];
            }
        }
        
        public ProductInfo ProductInfo{
            get {
                return _productInfo["unique"];
            }
        }

        public SpecFile() {
            FileGroups = new Indexer<FileGroup>(
                () => from rule in FileGroupRules select rule.Parameter ?? rule.Name,
                ruleName => from rule in FileGroupRules where (rule.Name == ruleName || rule.Parameter == ruleName) select rule,
                ruleName => {
                    var rule = new Rule {
                        Name = ruleName
                    };
                    _rules.Add(rule.FullSelector, rule);
                    return rule;
                });

            Processes = new Indexer<Process>(
                () => from rule in ProcessRules select rule.Parameter,
                parameter => from rule in ProcessRules where rule.Parameter.Equals( parameter, StringComparison.CurrentCultureIgnoreCase) select rule,
                parameter => {
                    var rule = new Rule {
                        Class = "process",
                        Parameter = parameter
                    };
                    _rules.Add(rule.FullSelector, rule);
                    return rule;
                });

            Events = new Indexer<Event>(
                () => from rule in EventRules select rule.Id,
                id => from rule in EventRules where rule.Id == id select rule,
                id => {
                    var rule = new Rule {
                        Class = "event",
                        Id = EventRules.Max(each => each.Id)+1
                    };
                    _rules.Add(rule.FullSelector, rule);
                    return rule;
                });
         
            Libraries = new Indexer<Library>(
                () => from rule in LibraryRules select rule.Parameter,
                parameter => from rule in LibraryRules where rule.Parameter == parameter select rule,
                parameter => {
                    var rule =new Rule {
                        Name = "library",
                        Parameter = parameter
                    };
                    _rules.Add(rule.FullSelector, rule);
                    return rule;
                });  

               _link= new Indexer<Link>(
                () => "unique".SingleItemAsEnumerable(),
                unique => LinkRules,
                unique => {
                    var rule = new Rule {
                        Class = "link"
                    };
                    _rules.Add(rule.FullSelector, rule);
                    return rule;
                });              
                
                _productInfo= new Indexer<ProductInfo>(
                () => "unique".SingleItemAsEnumerable(),
                unique => ProductInfoRules,
                unique => {
                    var rule = new Rule {
                        Id = "product-info"
                    };
                    _rules.Add(rule.FullSelector, rule);
                    return rule;
                });        
      

        }

        private IEnumerable<Rule> FileGroupRules {
            get {
                return from rule in Rules
                       where
                           string.IsNullOrEmpty(rule.Id) && !rule.Name.Equals("library", StringComparison.CurrentCultureIgnoreCase) &&
                               ((rule.Name == "*" && rule.Class.Equals("file-group", StringComparison.CurrentCultureIgnoreCase) &&
                                   !string.IsNullOrEmpty(rule.Parameter)) ||
                                       (rule.Name != "*" && string.IsNullOrEmpty(rule.Class) && string.IsNullOrEmpty(rule.Parameter)))
                       select rule;
            }
        }

        private IEnumerable<Rule> ProcessRules {
            get {
                return from rule in Rules
                       where string.IsNullOrEmpty(rule.Id) && rule.Name == "*" && rule.Class == "process"
                       select rule;
            }
        }

        private IEnumerable<Rule> EventRules {
            get {
                return from rule in Rules
                       where !string.IsNullOrEmpty(rule.Id) && rule.Name == "*" && rule.Class == "event"
                       select rule;
            }
        }

        private IEnumerable<Rule> LibraryRules {
            get {
                return from rule in Rules
                       where string.IsNullOrEmpty(rule.Id) && string.IsNullOrEmpty(rule.Class)  && !string.IsNullOrEmpty(rule.Parameter) && rule.Name == "library" 
                       select rule;
            }
        }

        private IEnumerable<Rule> LinkRules {
            get {
                return from rule in Rules
                       where string.IsNullOrEmpty(rule.Id) && string.IsNullOrEmpty(rule.Parameter) && rule.Name == "*" && rule.Class == "link"
                       select rule;
            }

        } 

        private IEnumerable<Rule> ProductInfoRules {
            get {
                return from rule in Rules
                       where string.IsNullOrEmpty(rule.Class) && string.IsNullOrEmpty(rule.Parameter) && rule.Name == "*" && rule.Id== "product-info"
                       select rule;
            }
        } 

        public void Validate() {
            if( LinkRules.Count() > 1) {
                throw new EndUserRuleException( LinkRules.First(),"SPEC 200", "Multiple .link classes in spec file."  );
            }
            
            var unrecognized = Rules.Except(FileGroupRules.Union(ProcessRules).Union(EventRules).Union(LinkRules).Union(LibraryRules));
            if( unrecognized.Any()) {
                throw new EndUserRuleException( unrecognized.First(),"SPEC 201", "Unrecognized rule in spec file"  );
            }
        }

        public new static SpecFile Load(string path) {
            var specfile = (SpecFile)PropertySheetParser.Parse(File.ReadAllText(path), path, new SpecFile());

            specfile.Validate();
            specfile.Filename = path;

            foreach( var proc in specfile.Processes ) {
                proc.Load();
            }

            return specfile;
        }

        public override void Save(string path) {
            // fix: make sure that events are numbered correctly.
            foreach (var v in Events.Where(v => v.Priority == -1)) {
                v.Priority = Events.Max(each => each.Priority) + 1;
            }

            // And, push the values from the .process rules into the page 
            foreach( var proc in Processes ) {
                proc.Save();
            }

            base.Save(path);
        }

        public override IEnumerable<string> Keys {
            get {
                return base.Keys.OrderBy(each => each , new Extensions.Comparer<string>((x,y) => { 
                    if( x.StartsWith("#")) {
                        return y.StartsWith("#") ? x.CompareTo(y) : -1;
                    }
                    if( x.StartsWith(".")) {
                        return y.StartsWith(".") ? x.CompareTo(y) : 1;
                    }
                    if( y.StartsWith(".")) {
                        return -1;
                    }
                    if( y.StartsWith("#")) {
                        return 1;
                    }
                    if( x.StartsWith("library")) {
                        return y.StartsWith("library") ? x.CompareTo(y) : -1;
                    }
                    return y.StartsWith("library") ? 1 : 0;}));
            }
        }
    }
}
