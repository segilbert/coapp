//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Eric Schultz. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Linq;
using CoApp.Toolkit.Extensions;

namespace CoApp.Toolkit.Package
{
    public struct Huid
    {
        internal string Name { get; private set; }
        internal string Version { get; private set; }
        internal string Architecture { get; private set; }
        internal string PublicKeyToken { get; private set; }
        internal string[] Additional { get; private set; }
        public Huid(string name, string version, string architecture, string publicKeyToken, params string[] additional) : this()
        {
            this.Name = name;
            this.Version = version;
            this.Architecture = architecture;
            this.PublicKeyToken = publicKeyToken;
            this.Additional = additional;
        }

        internal Guid ToGuid()
        {
            return this;
        }

        public static implicit operator Guid(Huid huid)
        {
            var concatAddl = huid.Additional.Aggregate("", (current, s) => current + s);

            var hash = (huid.Name + huid.Version + huid.Architecture + huid.PublicKeyToken + concatAddl).MD5Hash();

            return Guid.Parse(hash);
            
        }

        public static explicit operator String(Huid huid)
        {
            return huid.ToString();
        }


        public override string ToString()
        {
            return ((Guid)this).ToString("B").ToUpper();
        }

        public string ToString(string format)
        {
            return ((Guid)this).ToString(format).ToUpper();
        }


       
    }
}
