//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine {
    using System.Text.RegularExpressions;
    using Extensions;

    public class PackageName {
        private static readonly char[] _slashes = new [] {'\\', '/' };
        private static readonly Regex _canonicalName = new Regex(@"^(.*)-(\d{1,5}\.\d{1,5}\.\d{1,5}\.\d{1,5})-(any|x86|x64|arm)-([0-9a-f]{16})$", RegexOptions.IgnoreCase);
        private static readonly Regex[] _partialName = new[] {
            new Regex(@"^(.*)-(\d{1,5}|\*)(\.\d{1,5}|\.\*)(\.\d{1,5}|\.\*)(\.\d{1,5}|\.\*)-(any|x86|x64|arm|all|\*)-([0-9a-f]{16}|\*)$", RegexOptions.IgnoreCase),
            new Regex(@"^(.*)-(\d{1,5}|\*)(\.\d{1,5}|\.\*)(\.\d{1,5}|\.\*)(\.\d{1,5}|\.\*)-(any|x86|x64|arm|all|\*)$", RegexOptions.IgnoreCase),
            new Regex(@"^(.*)-(\d{1,5}|\*)(\.\d{1,5}|\.\*)(\.\d{1,5}|\.\*)(\.\d{1,5}|\.\*)$", RegexOptions.IgnoreCase),
            new Regex(@"^(.*)-(\d{1,5}|\*)(\.\d{1,5}|\.\*)(\.\d{1,5}|\.\*)$", RegexOptions.IgnoreCase),
            new Regex(@"^(.*)-(\d{1,5}|\*)(\.\d{1,5}|\.\*)$", RegexOptions.IgnoreCase),
            new Regex(@"^(.*)-(\d{1,5}|\*)$", RegexOptions.IgnoreCase)
        };

        public string CanonicalName { get; private set; }
        public string Name { get; private set; }
        public string Version { get; private set; }
        public string Version1 { get; private set; }
        public string Version2 { get; private set; }
        public string Version3 { get; private set; }
        public string Version4 { get; private set; }
        public string Arch { get; private set; }
        public string PublicKeyToken { get; private set; }

        public bool IsPartialMatch { get { return !IsFullMatch && !Name.IsNullOrEmpty(); }}
        public bool IsFullMatch { get { return (!string.IsNullOrEmpty(CanonicalName)); } }

        private void SetFieldsFromMatch( Match match ) {
            var c = match.Groups.Count;

            if( c <= 1) {
                return;
            }

            Name = c <= 1 ? string.Empty : match.Groups[1].Captures[0].Value;
            Version1 = c <= 2 ? string.Empty : match.Groups[2].Captures[0].Value;
            Version2 = c <= 3 ? string.Empty : match.Groups[3].Captures[0].Value;
            Version3 = c <= 4 ? string.Empty : match.Groups[4].Captures[0].Value;
            Version4 = c <= 5 ? string.Empty : match.Groups[5].Captures[0].Value;
            Arch = c <= 6 ? string.Empty : match.Groups[6].Captures[0].Value;
            PublicKeyToken = c <= 7 ? string.Empty : match.Groups[7].Captures[0].Value;

            Version = Version1 + Version2 + Version3 + Version4;
        }

        private PackageName(string potentialPartialPackageName) {
            if (potentialPartialPackageName.IndexOfAny(_slashes) > -1) {
                return;
            }
                
            var match = _canonicalName.Match(potentialPartialPackageName.ToLower());
            if (match.Success) {
                // perfect canonical match for a name
                CanonicalName = potentialPartialPackageName;
                SetFieldsFromMatch(match);
                return;
            }

            foreach( var rx in _partialName ) {
                match = rx.Match(potentialPartialPackageName);
                if( match.Success ) {
                    SetFieldsFromMatch(match);
                    return;
                }
            }

            Name = potentialPartialPackageName;
            return;
        }

        public static PackageName Parse(string potentialPartialPackageName) {
            return new PackageName(potentialPartialPackageName);
        }
    }
}