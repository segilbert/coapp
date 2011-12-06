using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Toolkit.Exceptions {
    using Extensions;

    public class UnknownAccountException : CoAppException{
        internal string _account ;
        public UnknownAccountException(string account) : base("Unknown account '{0}'".format(account)) {
            _account = account;
        }
    }
}
