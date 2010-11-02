//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Original Copyright (c) 2009 Microsoft Corporation. All rights reserved.
//     Changes Copyright (c) 2010  Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

// -----------------------------------------------------------------------
// Original Code: 
// (c) 2009 Microsoft Corporation -- All rights reserved
// This code is licensed under the MS-PL
// http://www.opensource.org/licenses/ms-pl.html
// Courtesy of the Open Source Techology Center: http://port25.technet.com
// -----------------------------------------------------------------------

namespace CoApp.Toolkit.Extensions {
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text.RegularExpressions;

    public class ComplexOption {
        public string WholePrefix; // stuff in the []
        public string WholeValue; // stuff after the []
        public List<string> PrefixParameters = new List<string>(); // individual items in the []
        public Dictionary<string, string> Values = new Dictionary<string, string>(); // individual key/values after the []
    }

    public static class CommandLineExtensions {
        private static Dictionary<string, List<string>> switches;
        private static List<string> parameters;

        public static Dictionary<string, List<string>> Switches(this string[] args) {
            if(switches != null) {
                return switches;
            }

            var firstarg = 0;
            switches = new Dictionary<string, List<string>>();

            // load a <exe>.properties file in the same location as the executing assembly.
            var assemblypath = Assembly.GetEntryAssembly().Location;
            var propertiespath = "{0}\\{1}.properties".format(Path.GetDirectoryName(assemblypath), Path.GetFileNameWithoutExtension(assemblypath));
            if(File.Exists(propertiespath)) {
                propertiespath.LoadConfiguration();
            }

            while(firstarg < args.Length && args[firstarg].StartsWith("--")) {
                var arg = args[firstarg].Substring(2).ToLower();
                var param = "";
                int pos;

                if((pos = arg.IndexOf("=")) > -1) {
                    param = args[firstarg].Substring(pos + 3);
                    arg = arg.Substring(0, pos);
                    if(string.IsNullOrEmpty(param) || string.IsNullOrEmpty(arg)) {
                        "Invalid Option :{0}".Print(args[firstarg].Substring(2).ToLower());
                        switches.Clear();
                        switches.Add("help", new List<string>());
                        return switches;
                    }
                }
                if(arg.Equals("load-config")) {
                    // loads the config file, and then continues parsing this line.
                    LoadConfiguration(param);
                    firstarg++;
                    continue;
                }

                if(!switches.ContainsKey(arg)) {
                    switches.Add(arg, new List<string>());
                }

                switches[arg].Add(param);
                firstarg++;
            }
            return switches;
        }

        public static void LoadConfiguration(this string file) {
            if(switches == null) {
                switches = new Dictionary<string, List<string>>();
            }

            var param = "";
            var category = "";

            string arg;
            int pos;
            if(File.Exists(file)) {
                var lines = File.ReadAllLines(file);
                for(var ln = 0; ln < lines.Length; ln++) {
                    var line = lines[ln].Trim();
                    while(line.EndsWith("\\") && ln < lines.Length) {
                        line = line.Substring(0, line.Length - 1);
                        if(++ln < lines.Length) {
                            line += lines[ln].Trim();
                        }
                    }
                    arg = line;

                    param = "";

                    if(arg.IndexOf("[") == 0) {
                        // category 
                        category = arg.Substring(1, arg.IndexOf(']')-1).Trim();
                        continue;
                    }

                    if(string.IsNullOrEmpty(arg) || arg.StartsWith(";") || arg.StartsWith("#")) // comments
                    {
                        continue;
                    }

                    if(!string.IsNullOrEmpty(category))
                    arg = "{0}-{1}".format(category, arg);

                    if((pos = arg.IndexOf("=")) > -1) {
                        param = arg.Substring(pos + 1);
                        arg = arg.Substring(0, pos).ToLower();

                        if(string.IsNullOrEmpty(param) || string.IsNullOrEmpty(arg)) {
                            "Invalid Option in config file [{0}]: {1}".Print(file, line.Trim());
                            switches.Add("help", new List<string>());
                            return;
                        }
                    }

                    if(!switches.ContainsKey(arg)) {
                        switches.Add(arg, new List<string>());
                    }

                    switches[arg].Add(param);
                }
            }
            else {
                "Unable to find configuration file [{0}]".Print(param);
            }
        }

        // handles complex option switches
        // RX for splitting comma seperated values:
        //  http://dotnetslackers.com/Regex/re-19977_Regex_This_regex_splits_comma_or_semicolon_separated_lists_of_optionally_quoted_strings_It_hand.aspx
        //      @"\s*[;,]\s*(?!(?<=(?:^|[;,])\s*""(?:[^""]|""""|\\"")*[;,])(?:[^""]|""""|\\"")*""\s*(?:[;,]|$))"
        //  http://regexlib.com/REDetails.aspx?regexp_id=621
        //      @",(?!(?<=(?:^|,)\s*\x22(?:[^\x22]|\x22\x22|\\\x22)*,)(?:[^\x22]|\x22\x22|\\\x22)*\x22\s*(?:,|$))"
        public static List<ComplexOption> GetComplexOptions(this List<string> rawParameterList) {
            var optionList = new List<ComplexOption>();
            foreach(string p in rawParameterList) {
                var m = Regex.Match(p, @"\[(?>\"".*?\""|\[(?<DEPTH>)|\](?<-DEPTH>)|[^[]]?)*\](?(DEPTH)(?!))");
                if(m.Success) {
                    var co = new ComplexOption();
                    var v = m.Groups[0].Value;
                    var len = v.Length;
                    co.WholePrefix = v.Substring(1, len - 2);
                    co.WholeValue = p.Substring(len);

                    var parameterStrings = Regex.Split(co.WholePrefix, @",(?!(?<=(?:^|,)\s*\x22(?:[^\x22]|\x22\x22|\\\x22)*,)(?:[^\x22]|\x22\x22|\\\x22)*\x22\s*(?:,|$))");
                    foreach(string q in parameterStrings) {
                        v = q.Trim();
                        if(v[0] == '"' && v[v.Length - 1] == '"') {
                            v = v.Trim('"');
                        }
                        co.PrefixParameters.Add(v);
                    }

                    var values = co.WholeValue.Split('&');
                    foreach(string q in values) {
                        var pos = q.IndexOf('=');
                        if(pos > -1 && pos < q.Length - 1) {
                            co.Values.Add(q.Substring(0, pos).UrlDecode(), q.Substring(pos + 1).UrlDecode());
                        }
                        else {
                            co.Values.Add(q.Trim('='), "");
                        }
                    }
                    optionList.Add(co);
                }
            }
            return optionList;
        }

        public static List<string> Parameters(this string[] args) {
            if(parameters != null) {
                return parameters;
            }

            var index = 0;
            parameters = new List<string>();

            while(index < args.Length && args[index].StartsWith("--")) {
                index++;
            }

            while(index < args.Length) {
                parameters.Add(args[index]);
                index++;
            }
            return parameters;
        }

        public const string HelpConfigSyntax = @"
Advanced Command Line Configuration Files 
-----------------------------------------
You may pass any double-dashed command line options in a configuration file 
that is loaded with --load-config=<file>.

Inside the configuration file, omit the double dash prefix; simply put 
each option on a seperate line.

On the command line:

    --some-option=foo 

would become the following inside the configuration file: 

    some-option=foo

Additionally, options in the configuration file can be grouped together in 
categories. The category is simply syntatic sugar for simplifying the command
line.

Categories are declared with the square brackets: [] 

The category is appended to options that follow its declaration.

A configuration file expressed as:

source-option=foo
source-option=bar
source-option=bin
source-add=baz
source-ignore=bug

can also be expressed as:

[source]
option=foo
option=bar
option=bin
add=baz
ignore=bug
";
    }
}