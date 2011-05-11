//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Scripting.Languages.CSharp {
    using System.Collections.Generic;
    using Utility;

    /// <summary>
    ///   A tokenizer customized for the c# language.
    /// </summary>
    public class CSharpTokenizer : Tokenizer {
        /// <summary>
        ///   The list of keywords for C#
        /// </summary>
        private static readonly HashSet<string> CSKeywords = new HashSet<string>(new[] {
                                                                                           "add", "dynamic", "from", "get", "global", "group", "into", "join", "let", "orderby", "partial", "remove",
                                                                                           "select", "set", "value", "var", "where", "yield", "abstract", "as", "base", "bool", "break", "byte", "case",
                                                                                           "catch", "char", "checked", "class", "const", "continue", "decimal", "default", "delegate", "do", "double",
                                                                                           "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto",
                                                                                           "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object",
                                                                                           "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte",
                                                                                           "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try",
                                                                                           "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
                                                                                       });

        /// <summary>
        ///   Protected Constructor for tokenizing c# code. Public access via the static Tokenize methods.
        /// </summary>
        /// <param name = "text">the array of characters to tokenize.</param>
        protected CSharpTokenizer(char[] text)
            : base(text) {
            Keywords = CSKeywords;
        }

        /// <summary>
        ///   Handles the 'other' case for C#
        /// </summary>
        protected override void ParseOther() {
            var start = Index;
            if(CurrentCharacter == '@') {
                if(NextCharacter == '"') {
                    ParseAtStringLiteral();
                    return;
                }

                if(CharsLeft == 0) {
                    AddToken(new Token {Type = TokenType.Unknown, Data = "@"});
                    return;
                }

                AdvanceAndRecognize();

                if(!IsCurrentCharacterIdentifierStartCharacter) {
                    AddToken(new Token {Type = TokenType.Unknown, Data = "@"});
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
            var start = Index;
            Index += 2;
            RecognizeNextCharacter();
            do {
                if(CurrentCharacter == '"' && NextCharacter == '"') {
                    Index++;
                }

                AdvanceAndRecognize();
            } while(CurrentCharacter != '"' || (CurrentCharacter == '"' && NextCharacter == '"'));

            AddToken(new Token {Type = TokenType.StringLiteral, Data = new string(Text, start, (Index - start) + 1)});
        }

        /// <summary>
        ///   Tokenizes the source code and returns a list of tokens
        /// </summary>
        /// <param name = "text">The C# source code to tokenize (as a string)</param>
        /// <returns>A List of tokens</returns>
        public static new List<Token> Tokenize(string text) {
            return Tokenize(string.IsNullOrEmpty(text) ? new char[0] : text.ToCharArray());
        }

        /// <summary>
        ///   Tokenizes the source code and returns a list of tokens
        /// </summary>
        /// <param name = "text">The C# source code to tokenize (as an array of characters)</param>
        /// <returns>A List of tokens</returns>
        public static new List<Token> Tokenize(char[] text) {
            var tokenizer = new CSharpTokenizer(text);
            tokenizer.Tokenize();
            return tokenizer.Tokens;
        }
    }
}