//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Scripting.Utility {
    /// <summary>
    ///   Represents a Token along with the textual representation of the token
    /// </summary>
    public struct Token {
        /// <summary>
        ///   The TokenType of the token
        /// </summary>
        public TokenType Type { get; set; }

        /// <summary>
        ///   the data associated with the Token
        /// </summary>
        public dynamic Data { get; set; }

        /// <summary>
        ///    the data in its raw state.
        /// </summary>
        public dynamic RawData { get; set; }

        /// <summary>
        /// The Row in the source file where this token started
        /// </summary>
        public int Row { get; set; }

        /// <summary>
        /// The column in the source file where this token started
        /// </summary>
        public int Column { get; set; }

        /// <summary>
        ///   Indicates whether two instance are equal.
        /// </summary>
        /// <param name = "first">first instance</param>
        /// <param name = "second">second instance</param>
        /// <returns>True if equal</returns>
        public static bool operator ==(Token first, Token second) {
            return first.Type == second.Type && first.Data == second.Data;
        }

        /// <summary>
        ///   Indicates whether two instance are inequal.
        /// </summary>
        /// <param name = "first">first instance</param>
        /// <param name = "second">second instance</param>
        /// <returns>True if inequal</returns>
        public static bool operator !=(Token first, Token second) {
            return !(first.Type == second.Type && first.Data == second.Data);
        }

        /// <summary>
        ///   Indicates whether this instance and a specified token are equal.
        /// </summary>
        /// <returns>
        ///   true if <paramref name = "other" /> and this instance are the same type and represent the same value; otherwise, false.
        /// </returns>
        /// <param name = "other">Another object to compare to. </param>
        /// <filterpriority>2</filterpriority>
        public bool Equals(Token other) {
            return Equals(other.Type, Type) && Equals(other.Data, Data);
        }

        /// <summary>
        ///   Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <returns>
        ///   true if <paramref name = "obj" /> and this instance are the same type and represent the same value; otherwise, false.
        /// </returns>
        /// <param name = "obj">Another object to compare to. </param>
        /// <filterpriority>2</filterpriority>
        public override bool Equals(object obj) {
            if(ReferenceEquals(null, obj)) {
                return false;
            }
            if(obj.GetType() != typeof(Token)) {
                return false;
            }
            return Equals((Token) obj);
        }

        /// <summary>
        ///   Returns the hash code for this instance.
        /// </summary>
        /// <returns>
        ///   A 32-bit signed integer that is the hash code for this instance.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode() {
            unchecked {
                return (Type.GetHashCode()*397) ^ (Data != null ? Data.GetHashCode() : 0);
            }
        }
    }
}