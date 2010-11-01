//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using CoApp.Toolkit.Scripting.Utility;

namespace CoApp.Toolkit.Scripting {
    public class PropertySheetTokenizer : Tokenizer {

        /// <summary>
        ///   The list of keywords for Cascading Property Sheets
        /// </summary>
        private static readonly HashSet<string> CpsKeywords = new HashSet<string>();

          /// <summary>
        ///   Protected Constructor for tokenizing CPS code. Public access via the static Tokenize methods.
        /// </summary>
        /// <param name = "text">the array of characters to tokenize.</param>
        protected PropertySheetTokenizer(char[] text)
            : base(text) {
                Keywords = CpsKeywords;
        }
        /// <summary>
        ///   Tokenizes the source code and returns a list of tokens
        /// </summary>
        /// <param name = "text">The CPS source code to tokenize (as a string)</param>
        /// <returns>A List of tokens</returns>
        public static new List<Token> Tokenize(string text) {
            return Tokenize(string.IsNullOrEmpty(text) ? new char[0] : text.ToCharArray());
        }

        /// <summary>
        ///   Tokenizes the source code and returns a list of tokens
        /// </summary>
        /// <param name = "text">The CPS source code to tokenize (as an array of characters)</param>
        /// <returns>A List of tokens</returns>
        public static new List<Token> Tokenize(char[] text) {
            var tokenizer = new PropertySheetTokenizer(text);
            tokenizer.Tokenize();
            return tokenizer.Tokens;
        }

        protected override bool PoachParse() {
            if(CurrentCharacter == '-') {
            }


            if(CurrentCharacter == '[' ) {
                var start = Index+1;
                while(CharsLeft > 0) {
                    AdvanceAndRecognize();

                    if(CurrentCharacter == ']' ) {
                        break;
                    }
                }

                var selectorParameter = new string(Text, start, (Index - start) ).Trim();
                Tokens.Add(new Token { Type = TokenType.SelectorParameter, Data = selectorParameter });
                
                return true;

            }
            return false;
        }

        protected override bool IsCurrentCharacterIdentifierPartCharacter {
            get {
                if( CurrentCharacter == '-' )
                    return true;

                return base.IsCurrentCharacterIdentifierPartCharacter;
            }
        }
    }
}


