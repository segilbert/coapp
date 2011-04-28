//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Scripting.Languages.PropertySheet {
    using System.Collections.Generic;
    using Utility;

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

        protected override bool IsCurrentCharacterIdentifierPartCharacter {
            get {
                if (CurrentCharacter == '-') {
                    return true;
                }

                return base.IsCurrentCharacterIdentifierPartCharacter;
            }
        }

        /// <summary>
        ///   Tokenizes the source code and returns a list of tokens
        /// </summary>
        /// <param name = "text">The CPS source code to tokenize (as a string)</param>
        /// <returns>A List of tokens</returns>
        public new static List<Token> Tokenize(string text) {
            return Tokenize(string.IsNullOrEmpty(text) ? new char[0] : text.ToCharArray());
        }

        /// <summary>
        ///   Tokenizes the source code and returns a list of tokens
        /// </summary>
        /// <param name = "text">The CPS source code to tokenize (as an array of characters)</param>
        /// <returns>A List of tokens</returns>
        public new static List<Token> Tokenize(char[] text) {
            var tokenizer = new PropertySheetTokenizer(text);
            tokenizer.Tokenize();
            return tokenizer.Tokens;
        }

        protected override bool PoachParse() {
            if (CurrentCharacter == '-') {
            }

            if (CurrentCharacter == '[') {
                int start = Index + 1;
                while (CharsLeft > 0) {
                    AdvanceAndRecognize();

                    if (CurrentCharacter == ']') {
                        break;
                    }
                }

                string selectorParameter = new string(Text, start, (Index - start)).Trim();
                Tokens.Add(new Token {Type = TokenType.SelectorParameter, Data = selectorParameter});

                return true;
            }
            return false;
        }

        /// <summary>
        ///   Handles the '@' case 
        /// </summary>
        protected override void ParseOther() {
            var start = Index;
            if (CurrentCharacter == '@') {
                if (NextCharacter == '"') {
                    ParseAtStringLiteral();
                    return;
                }

                if (CharsLeft == 0) {
                    Tokens.Add(new Token { Type = TokenType.Unknown, Data = "@" });
                    return;
                }

                AdvanceAndRecognize();

                if (!IsCurrentCharacterIdentifierStartCharacter) {
                    Tokens.Add(new Token { Type = TokenType.Unknown, Data = "@" });
                    Index--; // rewind back to last character.
                    return;
                }
            }

            ParseIdentifier(start);
        }

        /// <summary>
        ///   Parses source code for a string starting with an at symbol ( @ )
        /// </summary>
        protected virtual void ParseAtStringLiteral() {
            // @"..."
            Index += 2;
            var start = Index;
            
            RecognizeNextCharacter();
            do {
                if (CurrentCharacter == '"' && NextCharacter == '"') {
                    Index++;
                }

                AdvanceAndRecognize();
            } while (CurrentCharacter != '"' || (CurrentCharacter == '"' && NextCharacter == '"'));

            Tokens.Add(new Token { Type = TokenType.StringLiteral, Data = new string(Text, start, (Index - start) ), RawData = "@Literal"});
        }
    }
}