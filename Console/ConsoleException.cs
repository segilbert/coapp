using System;
using CoApp.Toolkit.Extensions;

namespace CoApp.Toolkit.Console {
    public class ConsoleException : Exception {
        public ConsoleException(string reason, params object[] parameters ): base(reason.format(parameters)) {
            
        }
    }
}
