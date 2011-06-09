//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Scripting.Utility {
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;

    /// <summary>
    ///   A moderatly generic tokenizer class 
    ///   -----------------------------------
    ///   <para>
    ///     This is designed to tokenize (not parse or lexically analyze) c#, but is flexible in implementation
    ///     to handle certainly every c-style language, and probably most languages.</para>
    ///   <para>
    ///     It should be pretty darned fast. Not the absolute fastest it could run, but a fair balance between
    ///     speed and complexity I'd wager.</para>
    /// </summary>
    [SuppressMessage("Microsoft.StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "It's too much work.")]
    public class Tokenizer {
        #region Standard Static Tokens

        public static readonly Token Space = new Token {Type = TokenType.WhiteSpace, Data = " "};
        public static readonly Token Eol = new Token {Type = TokenType.WhiteSpace, Data = "\n"};
        public static readonly Token Tab = new Token {Type = TokenType.WhiteSpace, Data = "\t"};

        public static readonly Token Plus = new Token {Type = TokenType.Plus, Data = "+"};
        public static readonly Token PlusPlus = new Token {Type = TokenType.PlusPlus, Data = "++"};
        public static readonly Token PlusEquals = new Token {Type = TokenType.PlusEquals, Data = "+="};

        public static readonly Token Minus = new Token {Type = TokenType.Minus, Data = "-"};
        public static readonly Token MinusMinus = new Token {Type = TokenType.MinusMinus, Data = "--"};
        public static readonly Token MinusEquals = new Token {Type = TokenType.MinusEquals, Data = "-="};
        public static readonly Token DashArrow = new Token {Type = TokenType.DashArrow, Data = "->"};

        public static readonly Token Star = new Token {Type = TokenType.Asterisk, Data = "*"};
        public static readonly Token StarEquals = new Token {Type = TokenType.AsteriskEquals, Data = "*="};

        public static readonly Token Equal = new Token {Type = TokenType.Equal, Data = "="};
        public static readonly Token EqualEqual = new Token {Type = TokenType.EqualEqual, Data = "=="};
        public static readonly Token Lambda = new Token {Type = TokenType.Lambda, Data = "=>"};

        public static readonly Token Slash = new Token {Type = TokenType.Slash, Data = "/"};
        public static readonly Token SlashEquals = new Token {Type = TokenType.SlashEquals, Data = "/="};

        public static readonly Token Bar = new Token {Type = TokenType.Bar, Data = "|"};
        public static readonly Token BarBar = new Token {Type = TokenType.BarBar, Data = "||"};
        public static readonly Token BarEquals = new Token {Type = TokenType.BarEquals, Data = "|="};

        public static readonly Token Ampersand = new Token {Type = TokenType.Ampersand, Data = "&"};
        public static readonly Token AmpersandAmpersand = new Token {Type = TokenType.AmpersandAmpersand, Data = "&&"};
        public static readonly Token AmpersandEquals = new Token {Type = TokenType.AmpersandEquals, Data = "&="};

        public static readonly Token Percent = new Token {Type = TokenType.Percent, Data = "%"};
        public static readonly Token PercentEquals = new Token {Type = TokenType.PercentEquals, Data = "%="};

        public static readonly Token LessThan = new Token {Type = TokenType.LessThan, Data = "<"};
        public static readonly Token LessThanEquals = new Token {Type = TokenType.LessThanEquals, Data = "<="};
        public static readonly Token BitShiftLeft = new Token {Type = TokenType.BitShiftLeft, Data = "<<"};
        public static readonly Token BitShiftLeftEquals = new Token {Type = TokenType.BitShiftLeftEquals, Data = "<<="};

        public static readonly Token GreaterThan = new Token {Type = TokenType.GreaterThan, Data = ">"};
        public static readonly Token GreaterThanEquals = new Token {Type = TokenType.GreaterThanEquals, Data = ">="};
        public static readonly Token BitShiftRight = new Token {Type = TokenType.BitShiftRight, Data = ">>"};
        public static readonly Token BitShiftRightEquals = new Token {Type = TokenType.BitShiftRightEquals, Data = ">>="};

        public static readonly Token Bang = new Token {Type = TokenType.Bang, Data = "!"};
        public static readonly Token BangEquals = new Token {Type = TokenType.BangEquals, Data = "!="};

        public static readonly Token Dollar = new Token { Type = TokenType.Bang, Data = "$" };

        public static readonly Token Power = new Token {Type = TokenType.Power, Data = "^"};
        public static readonly Token PowerEquals = new Token {Type = TokenType.PowerEquals, Data = "^="};

        public static readonly Token Tilde = new Token {Type = TokenType.Tilde, Data = "~"};

        public static readonly Token QuestionMark = new Token {Type = TokenType.QuestionMark, Data = "?"};
        public static readonly Token QuestionMarkQuestionMark = new Token {Type = TokenType.QuestionMarkQuestionMark, Data = "??"};

        public static readonly Token OpenBrace = new Token {Type = TokenType.OpenBrace, Data = "{"};
        public static readonly Token CloseBrace = new Token {Type = TokenType.CloseBrace, Data = "}"};
        public static readonly Token OpenBracket = new Token {Type = TokenType.OpenBracket, Data = "["};
        public static readonly Token CloseBracket = new Token {Type = TokenType.CloseBracket, Data = "]"};
        public static readonly Token OpenParenthesis = new Token {Type = TokenType.OpenParenthesis, Data = "("};
        public static readonly Token CloseParenthesis = new Token {Type = TokenType.CloseParenthesis, Data = ")"};

        public static readonly Token Dot = new Token {Type = TokenType.Dot, Data = "."};
        public static readonly Token Comma = new Token {Type = TokenType.Comma, Data = ","};
        public static readonly Token Colon = new Token {Type = TokenType.Colon, Data = ":"};
        public static readonly Token Semicolon = new Token {Type = TokenType.Semicolon, Data = ";"};
        public static readonly Token Pound = new Token { Type = TokenType.Pound, Data = "#" };

        #endregion

        protected char[] Text { get; set; }

        protected HashSet<string> Keywords { get; set; }
        private int _index;
        private int row = 1;
        private int column = 1;
        private List<int> linelengths = new List<int>();

        protected int Index {
            get { return _index; } 
            set {
                if( value == 0) {
                    _index = 0;
                    column = 1;
                    row = 1;
                    return;
                }
                var delta = value - _index;

                if (delta == 0) {
                    return;
                }

                while( delta < 0 ) {
                    _index--;
                    delta++;
                    if (Index <= 0 )
                        return;

                    switch (Text[_index]) {
                        case '\n':
                            row--;
                            column = linelengths[row - 1];
                            linelengths.Remove(row - 1);
                            break;
                        case '\r':
                            /* ignore */
                            break;
                        default:
                            column--;
                            break;
                    }
                }
                while(delta > 0) {
                    _index++;
                    delta--;

                    if (Index >= Text.Length)
                        return;

                    switch(Text[_index] ) {
                        case '\n':
                            linelengths.Add(column);
                            column = 1;
                            row++;
                            break;
                        case '\r':
                            column = 1;
                            break;
                        default:
                            column++;
                            break;
                    }
                }
            }
        }

        protected int CharsLeft { get; set; }
        protected char CurrentCharacter { get; set; }
        protected char NextCharacter { get; set; }
        protected char NextNextCharacter { get; set; }

        protected List<Token> Tokens { get; set; }

        protected Tokenizer(char[] text) {
            Text = text;
            Tokens = new List<Token>();
            Keywords = new HashSet<string>();
        }

        protected void RecognizeNextCharacter() {
            CharsLeft = (Text.Length - Index) - 1; // not not including the current character.
            CurrentCharacter = Text[Index];
            NextCharacter = CharsLeft > 0 ? Text[Index + 1] : '\u0000';
            NextNextCharacter = CharsLeft > 1 ? Text[Index + 2] : '\u0000';
        }

        protected void AdvanceAndRecognize() {
            Index++;
            RecognizeNextCharacter();
        }

        protected void AddToken(Token token) {
            token.Row = row;
            token.Column = column - 1;
            Tokens.Add(token);
        }

        protected virtual void Tokenize() {
            for(Index = 0; Index < Text.Length; Index++) {
                RecognizeNextCharacter();

                if(!PoachParse()) {
                    switch(CurrentCharacter) {
                        case '~':
                            AddToken(Tilde);
                            break;

                        case '?':
                            ParseQuestionMark();
                            break;

                        case '{':
                            AddToken(OpenBrace);
                            break;

                        case '}':
                            AddToken(CloseBrace);
                            break;

                        case '[':
                            AddToken(OpenBracket);
                            break;

                        case ']':
                            AddToken(CloseBracket);
                            break;

                        case '(':
                            AddToken(OpenParenthesis);
                            break;

                        case ')':
                            AddToken(CloseParenthesis);
                            break;

                        case '.':
                            AddToken(Dot);
                            break;

                        case ',':
                            AddToken(Comma);
                            break;

                        case ':':
                            AddToken(Colon);
                            break;

                        case ';':
                            AddToken(Semicolon);
                            break;

                        case '\r':
                            if(NextCharacter == '\n') {
                                Index++;
                            }

                            AddToken(Eol);
                            break;

                        case '\n':
                            AddToken(Eol);
                            break;

                        case '\t':
                            AddToken(Tab);
                            break;

                        case ' ':
                            AddToken(Space);
                            break;

                        case '+':
                            ParsePlus();
                            break;

                        case '-':
                            ParseMinus();
                            break;

                        case '*':
                            ParseStar();
                            break;

                        case '=':
                            ParseEquals();
                            break;

                        case '/':
                            ParseSlash();
                            break;

                        case '|':
                            ParseBar();
                            break;

                        case '&':
                            ParseAmpersand();
                            break;

                        case '%':
                            ParsePercent();
                            break;

                        case '<':
                            ParseLessThan();
                            break;

                        case '>':
                            ParseGreaterThan();
                            break;

                        case '!':
                            ParseBang();
                            break;

                        case '$':
                            ParseDollar();
                            break;

                        case '^':
                            ParsePower();
                            break;

                        case '#':
                            ParsePound();
                            break;

                        case '\'':
                            ParseCharLiteral();
                            break;

                        case '"':
                            ParseStringLiteral();
                            break;

                        case '0':
                            if(NextCharacter == 'x' || NextCharacter == 'X') {
                                ParseHexadecimalLiteral();
                                break;
                            }

                            ParseNumericLiteral();
                            break;
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                            ParseNumericLiteral();
                            break;

                        default:
                            ParseOther();
                            break;
                    }
                }
            }
        }

        protected bool IsCurrentCharacterNumeric {
            get { return CurrentCharacter >= 0 && CurrentCharacter <= 9; }
        }

        protected bool IsCurrentCharacterHexadecimal {
            get { return (CurrentCharacter >= 0 && CurrentCharacter <= 9) || (CurrentCharacter >= 'a' && CurrentCharacter <= 'f') || (CurrentCharacter >= 'A' && CurrentCharacter <= 'F'); }
        }

        protected virtual void ParseNumericLiteral() {
            // #[###][U|u|L|l|UL|Ul|uL|ul|LU|Lu|lU|lu]
            // or 
            // #[###][.#[###]][f|F|d|D|m|M]
            // or
            // #[###].[.#[###]]+|-#[###]E|e#[###]
            var start = Index;

            while(IsCurrentCharacterNumeric) {
                AdvanceAndRecognize();
            }

            if(CurrentCharacter == 'u' || CurrentCharacter == 'U') {
                AdvanceAndRecognize();

                if(CurrentCharacter == 'L' || CurrentCharacter == 'l') {
                    AdvanceAndRecognize();
                }
            }
            else if(CurrentCharacter == 'l' || CurrentCharacter == 'L') {
                AdvanceAndRecognize();

                if(CurrentCharacter == 'u' || CurrentCharacter == 'U') {
                    AdvanceAndRecognize();
                }
            }
            else if(CurrentCharacter == 'f' || CurrentCharacter == 'F' || CurrentCharacter == 'd' || CurrentCharacter == 'D' || CurrentCharacter == 'm' || CurrentCharacter == 'M') {
                AdvanceAndRecognize();
            }
            else if(CurrentCharacter == '.') {
                AdvanceAndRecognize();

                while(IsCurrentCharacterNumeric) {
                    AdvanceAndRecognize();
                }

                if(CurrentCharacter == 'e' || CurrentCharacter == 'E') {
                    AdvanceAndRecognize();

                    if(CurrentCharacter == '+' || CurrentCharacter == '-') {
                        AdvanceAndRecognize();
                    }

                    while(IsCurrentCharacterNumeric) {
                        AdvanceAndRecognize();
                    }
                }
            }

            AddToken(new Token {Type = TokenType.NumericLiteral, Data = new string(Text, start, (Index - start) + 1)});
        }

        protected virtual void ParseHexadecimalLiteral() {
            // 0xH[HHH][U|u|L|l|UL|Ul|uL|ul|LU|Lu|lU|lu]
            var start = Index;
            Index += 2;
            RecognizeNextCharacter();
            while(IsCurrentCharacterHexadecimal) {
                AdvanceAndRecognize();
            }

            if(CurrentCharacter == 'u' || CurrentCharacter == 'U') {
                AdvanceAndRecognize();

                if(CurrentCharacter == 'L' || CurrentCharacter == 'l') {
                    AdvanceAndRecognize();
                }
            }
            else if(CurrentCharacter == 'l' || CurrentCharacter == 'L') {
                AdvanceAndRecognize();

                if(CurrentCharacter == 'u' || CurrentCharacter == 'U') {
                    AdvanceAndRecognize();
                }
            }

            AddToken(new Token {Type = TokenType.NumericLiteral, Data = new string(Text, start, (Index - start) + 1)});
        }

        protected virtual void ParseCharLiteral() {
            // 'c'
            // \' \" \\ \0 \a \b \f \n \r \t \v
            // \xH[HHH]
            var start = Index;
            AdvanceAndRecognize();
            if(CurrentCharacter == '\\') {
                if(CurrentCharacter == '\'' || CurrentCharacter == '\"' || CurrentCharacter == '\\' || CurrentCharacter == '0' || CurrentCharacter == 'a' || CurrentCharacter == 'b' || CurrentCharacter == 'f' || CurrentCharacter == 'n' || CurrentCharacter == 'r' || CurrentCharacter == 't' || CurrentCharacter == 'v') {
                    AdvanceAndRecognize();
                }
                else if(CurrentCharacter == 'x') {
                    AdvanceAndRecognize();
                    while(IsCurrentCharacterHexadecimal) {
                        AdvanceAndRecognize();
                    }
                }
            }

            AddToken(new Token {Type = TokenType.NumericLiteral, Data = new string(Text, start, (Index - start) + 1)});
        }

        protected virtual void ParseStringLiteral() {
            // "....."
            var start = Index;
            AdvanceAndRecognize();
            while(CurrentCharacter != '"' && CurrentCharacter != '\r' && CurrentCharacter != '\n') {
                AdvanceAndRecognize();
                if(CurrentCharacter == '\\') {
                    Index += 2;
                    RecognizeNextCharacter();
                }
            }

            var rawData = new string(Text, start, (Index - start) + 1);
            var data = rawData;
            data = data.Replace("\\\\", "\\");// .Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\t", "\t");
            data = data.Substring(1, data.Length - 2);
            
            AddToken(new Token {Type = TokenType.StringLiteral, RawData = rawData, Data = data});
        }

        protected virtual void ParsePower() {
            if(NextCharacter == '=') {
                AddToken(PowerEquals);
                Index++;
                return;
            }

            AddToken(Power);
        }

        protected virtual void ParseBang() {
            if (NextCharacter == '=') {
                AddToken(BangEquals);
                Index++;
                return;
            }

            AddToken(Bang);
        }

        protected virtual void ParseDollar() {

            AddToken(Dollar);
        }

        protected virtual void ParseGreaterThan() {
            switch(NextCharacter) {
                case '=':
                    AddToken(GreaterThanEquals);
                    Index++;
                    break;
                case '>':
                    if(NextNextCharacter == '=') {
                        AddToken(BitShiftRightEquals);
                        Index += 2;
                        break;
                    }

                    AddToken(BitShiftRight);
                    Index++;
                    break;
                default:
                    AddToken(GreaterThan);
                    break;
            }
        }

        protected virtual void ParseLessThan() {
            switch(NextCharacter) {
                case '=':
                    AddToken(LessThanEquals);
                    Index++;
                    break;
                case '<':
                    if(NextNextCharacter == '=') {
                        AddToken(BitShiftLeftEquals);
                        Index += 2;
                        break;
                    }

                    AddToken(BitShiftLeft);
                    Index++;
                    break;
                default:
                    AddToken(LessThan);
                    break;
            }
        }

        protected virtual void ParsePercent() {
            if(NextCharacter == '=') {
                AddToken(PercentEquals);
                Index++;
                return;
            }

            AddToken(Percent);
        }

        protected virtual void ParseAmpersand() {
            switch(NextCharacter) {
                case '&':
                    AddToken(AmpersandAmpersand);
                    Index++;
                    break;
                case '=':
                    AddToken(AmpersandEquals);
                    Index++;
                    break;
                default:
                    AddToken(Ampersand);
                    break;
            }
        }

        protected virtual void ParseBar() {
            switch(NextCharacter) {
                case '|':
                    AddToken(BarBar);
                    Index++;
                    break;
                case '=':
                    AddToken(BarEquals);
                    Index++;
                    break;
                default:
                    AddToken(Bar);
                    break;
            }
        }

        protected virtual void ParseSlash() {
            if(NextCharacter == '=') {
                AddToken(SlashEquals);
                Index++;
                return;
            }

            if(NextCharacter == '*') {
                // multiline comment
                var start = Index;
                Index += 2;
                while(Index < (Text.Length - 1)) {
                    if(Text[Index] == '*' && Text[Index + 1] == '/') {
                        AddToken(new Token {Type = TokenType.MultilineComment, Data = new string(Text, start, (Index - start) + 2)});
                        start = -1;
                        break;
                    }

                    Index++;
                }

                if(start > -1) {
                    // didn't find the close marker (star slash) 
                    // adding an incomplete comment at the end I guess.
                    AddToken(new Token {Type = TokenType.MultilineComment, Data = new string(Text, start, (Index - start) + 1)});
                }

                Index++;
                return;
            }

            if(NextCharacter == '/') {
                // line comment
                var start = Index;
                Index += 2;
                while(Index < Text.Length) {
                    if(Text[Index] == '\r' || Text[Index] == '\n') {
                        if (Index >= Text.Length || Text[Index] == '\r' && Text[Index + 1] == '\n') {
                            // if this is a CR, is there an LF after it?
                            Index++;
                        }
                        AddToken(new Token {Type = TokenType.LineComment, Data = new string(Text, start, (Index - start) + 1)});
                        return;
                        
                    }
                    Index++;
                }

                if(start > -1) {
                    // adding a comment at the end I guess.
                    AddToken(new Token {Type = TokenType.LineComment, Data = new string(Text, start, (Index - start) + 1)});
                }
                return;
            }

            AddToken(Slash);
        }

        protected virtual void ParseEquals() {
            if(NextCharacter == '=') {
                AddToken(EqualEqual);
                Index++;
                return;
            }

            AddToken(Equal);
        }

        protected virtual void ParseStar() {
            if(NextCharacter == '=') {
                AddToken(StarEquals);
                Index++;
                return;
            }

            AddToken(Star);
        }

        protected virtual void ParseMinus() {
            switch(NextCharacter) {
                case '-':
                    AddToken(MinusMinus);
                    Index++;
                    break;
                case '=':
                    AddToken(MinusEquals);
                    Index++;
                    break;
                case '>':
                    AddToken(DashArrow);
                    Index++;
                    break;
                default:
                    AddToken(Minus);
                    break;
            }
        }

        protected virtual void ParsePlus() {
            switch(NextCharacter) {
                case '+':
                    AddToken(PlusPlus);
                    Index++;
                    break;
                case '=':
                    AddToken(PlusEquals);
                    Index++;
                    break;
                default:
                    AddToken(Plus);
                    break;
            }
        }

        protected virtual void ParsePound() {
            // standard one line preproc-style directive
            var start = Index;
            Index++;
            while(Index < Text.Length) {
                if(Text[Index] == '\\') {
                    Index++;
                    if(Index < Text.Length && Text[Index] == '\r') {
                        Index++;
                    }

                    if(Index < Text.Length && Text[Index + 1] == '\n') {
                        Index++;
                    }
                }

                if(Text[Index] == '\r' || Text[Index] == '\n') {
                    AddToken(new Token {Type = TokenType.Pound, Data = new string(Text, start, (Index - start) + 1)});
                    start = -1;
                    break;
                }

                Index++;
            }

            if(start > -1) {
                // adding directive at the end I guess.
                AddToken(new Token {Type = TokenType.Pound, Data = new string(Text, start, (Index - start) + 1)});
            }

            Index++;
            return;
        }

        protected virtual void ParseQuestionMark() {
            if(NextCharacter == '?') {
                AddToken(QuestionMarkQuestionMark);
                Index++;
                return;
            }

            AddToken(QuestionMark);
        }

        /// <summary>
        ///   Returns true if the current character is in the UNICODE classes: Lu Ll Lt Lm Lo NI _
        /// </summary>
        protected virtual bool IsCurrentCharacterIdentifierStartCharacter {
            get {
                if(CurrentCharacter == '_') {
                    return true;
                }

                switch(CharUnicodeInfo.GetUnicodeCategory(CurrentCharacter)) {
                    case UnicodeCategory.UppercaseLetter:
                    case UnicodeCategory.LowercaseLetter:
                    case UnicodeCategory.TitlecaseLetter:
                    case UnicodeCategory.ModifierLetter:
                    case UnicodeCategory.OtherLetter:
                    case UnicodeCategory.LetterNumber:
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        ///   Returns true if the current character is in the UNICODE classes: Lu Ll Lt Lm Lo NI _ Nd Pc Cf Mn Mc
        /// </summary>
        protected virtual bool IsCurrentCharacterIdentifierPartCharacter {
            get {
                if(CurrentCharacter == '_') {
                    return true;
                }

                switch(CharUnicodeInfo.GetUnicodeCategory(CurrentCharacter)) {
                    case UnicodeCategory.UppercaseLetter:
                    case UnicodeCategory.LowercaseLetter:
                    case UnicodeCategory.TitlecaseLetter:
                    case UnicodeCategory.ModifierLetter:
                    case UnicodeCategory.OtherLetter:
                    case UnicodeCategory.LetterNumber:
                    case UnicodeCategory.DecimalDigitNumber:
                    case UnicodeCategory.ConnectorPunctuation:
                    case UnicodeCategory.Format:
                    case UnicodeCategory.NonSpacingMark:
                    case UnicodeCategory.SpacingCombiningMark:
                        return true;
                }

                return false;
            }
        }

        protected virtual void ParseOther() {
            ParseIdentifier(Index);
        }

        protected virtual bool PoachParse() {
            return false;
        }

        protected void ParseIdentifier(int start) {
            if(IsCurrentCharacterIdentifierStartCharacter) {
                while(CharsLeft > 0) {
                    AdvanceAndRecognize();

                    if(!IsCurrentCharacterIdentifierPartCharacter) {
                        Index--; // rewind back to last character.
                        break;
                    }
                }

                var identifier = new string(Text, start, (Index - start) + 1);
                AddToken(new Token {Type = Keywords.Contains(identifier) ? TokenType.Keyword : TokenType.Identifier, Data = identifier});
                return;
            }

            AddToken(new Token {Type = TokenType.Unknown, Data = CurrentCharacter});
        }

        public static IEnumerable<Token> Tokenize(string text) {
            return Tokenize(string.IsNullOrEmpty(text) ? new char[0] : text.ToCharArray());
        }

        public static IEnumerable<Token> Tokenize(char[] text) {
            var tokenizer = new Tokenizer(text);
            tokenizer.Tokenize();
            return tokenizer.Tokens;
        }
    }
}