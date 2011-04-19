//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Trace {
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Serialization;

    public partial class CommandLine {
        [XmlIgnore]
        public string Line {
            get { return line; }
            set {
                line = value;
                parameters = ParseResolveResponseFiles(value).Skip(1).ToList();
            }
        }

        public static IEnumerable<string> SplitParameterString(string rawParameter) {
            char[] text = rawParameter.ToCharArray();
            int len = text.Length;
            parseState state = parseState.none;
            int begin = 0;

            for (int i = 0; i < len; i++) {
                switch (state) {
                    case parseState.none:
                        if (text[i] == '"') {
                            begin = i + 1;
                            state = parseState.inquotedparam;
                            break;
                        }

                        if (text[i] == ' ') {
                            begin = i + 1;
                            break;
                        }

                        state = parseState.inparam;
                        break;

                    case parseState.inparam:
                        if (text[i] == ' ') {
                            state = parseState.none;
                            yield return new string(text, begin, i - begin);
                            begin = i + 1;
                            break;
                        }
                        break;

                    case parseState.inquotedparam:
                        if (text[i] == '\\' && i < (len - 1) && text[i + 1] == '"') {
                            i++;
                            break;
                        }
                        if (text[i] == '"') {
                            state = parseState.none;
                            yield return new string(text, begin, i - begin);
                            begin = i + 1;
                            break;
                        }
                        break;
                }
            }
            if (len - begin > 0) {
                yield return new string(text, begin, len - begin);
            }
        }

        private IEnumerable<string> ParseResolveResponseFiles(string rawParameterText) {
            foreach (string par in SplitParameterString(rawParameterText)) {
                if (par.StartsWith("@") && par.Length > 1) {
                    string fname = par.Substring(1);
                    if (System.IO.File.Exists(fname)) {
                        foreach (string l in System.IO.File.ReadAllLines(fname)) {
                            yield return l;
                        }
                        continue;
                    }
                }
                yield return par;
            }
        }

        #region Nested type: parseState

        private enum parseState {
            none,
            inparam,
            inquotedparam
        }

        #endregion
    }
}