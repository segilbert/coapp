//-----------------------------------------------------------------------
// <copyright company="Codeplex Foundation">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Scripting.Utility {

    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;

    /// <summary>
    /// Enumeration of different token types
    /// </summary>
    public enum TokenType {
        Plus,
        PlusPlus,
        PlusEquals,

        Minus,
        MinusMinus,
        MinusEquals,
        DashArrow,

        Asterisk,
        AsteriskEquals,

        Equal,
        EqualEqual,
        Lambda,

        Slash,
        SlashEquals,
        LineComment,
        MultilineComment,

        Bar,
        BarBar,
        BarEquals,

        Ampersand,
        AmpersandAmpersand,
        AmpersandEquals,

        Percent,
        PercentEquals,

        LessThan,
        LessThanEquals,
        BitShiftLeft,
        BitShiftLeftEquals,

        GreaterThan,
        GreaterThanEquals,
        BitShiftRight,
        BitShiftRightEquals,

        Bang,
        BangEquals,

        Power,
        PowerEquals,

        Tilde,

        QuestionMark,
        QuestionMarkQuestionMark,

        OpenBrace,
        CloseBrace,
        OpenBracket,
        CloseBracket,
        OpenParenthesis,
        CloseParenthesis,

        Dot,
        Comma,
        Colon,
        Semicolon,

        Pound,

        Unicode,

        Keyword,

        Identifier,

        StringLiteral,
        NumericLiteral,
        CharLiteral,

        WhiteSpace,

        Unknown
    }

    /// <summary>
    /// Represents a Token along with the textual representation of the token
    /// </summary>
    public struct Token {
        /// <summary>
        /// The TokenType of the token
        /// </summary>
        public TokenType Type { get; set; }
        
        /// <summary>
        /// the data associated with the Token
        /// </summary>
        public dynamic Data { get; set; }

        /// <summary>
        /// Indicates whether two instance are equal.
        /// </summary>
        /// <param name="first">first instance</param>
        /// <param name="second">second instance</param>
        /// <returns>True if equal</returns>
        public static bool operator==(Token first, Token second) {
            return first.Type == second.Type && first.Data == second.Data;
        }

        /// <summary>
        /// Indicates whether two instance are inequal.
        /// </summary>
        /// <param name="first">first instance</param>
        /// <param name="second">second instance</param>
        /// <returns>True if inequal</returns>
        public static bool operator !=(Token first, Token second) {
            return !(first.Type == second.Type && first.Data == second.Data);
        }

        /// <summary>
        /// Indicates whether this instance and a specified token are equal.
        /// </summary>
        /// <returns>
        /// true if <paramref name="other"/> and this instance are the same type and represent the same value; otherwise, false.
        /// </returns>
        /// <param name="other">Another object to compare to. </param><filterpriority>2</filterpriority>
        public bool Equals(Token other) {
            return Equals(other.Type, Type) && Equals(other.Data, Data);
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <returns>
        /// true if <paramref name="obj"/> and this instance are the same type and represent the same value; otherwise, false.
        /// </returns>
        /// <param name="obj">Another object to compare to. </param><filterpriority>2</filterpriority>
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
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer that is the hash code for this instance.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode() {
            unchecked {
                return (Type.GetHashCode()*397) ^ (Data != null ? Data.GetHashCode() : 0);
            }
        }
    }

    /// <summary>
    ///   A moderatly generic tokenizer class 
    ///   -----------------------------------
    /// <para>
    ///   This is designed to tokenize (not parse or lexically analyze) c#, but is flexible in implementation
    ///   to handle certainly every c-style language, and probably most languages.</para>
    /// <para>
    ///   It should be pretty darned fast. Not the absolute fastest it could run, but a fair balance between
    ///   speed and complexity I'd wager.</para>
    /// </summary>
    [SuppressMessage("Microsoft.StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "It's too much work.")]
    public class Tokenizer {
        #region Standard Static Tokens

        public static readonly Token Space = new Token { Type = TokenType.WhiteSpace, Data = " " };
        public static readonly Token Eol = new Token { Type = TokenType.WhiteSpace, Data = "\n" };
        public static readonly Token Tab = new Token { Type = TokenType.WhiteSpace, Data = "\t" };

        public static readonly Token Plus = new Token { Type = TokenType.Plus, Data = "+" };
        public static readonly Token PlusPlus = new Token { Type = TokenType.PlusPlus, Data = "++" };
        public static readonly Token PlusEquals = new Token { Type = TokenType.PlusEquals, Data = "+=" };

        public static readonly Token Minus = new Token { Type = TokenType.Minus, Data = "-" };
        public static readonly Token MinusMinus = new Token { Type = TokenType.MinusMinus, Data = "--" };
        public static readonly Token MinusEquals = new Token { Type = TokenType.MinusEquals, Data = "-=" };
        public static readonly Token DashArrow = new Token { Type = TokenType.DashArrow, Data = "->" };

        public static readonly Token Star = new Token { Type = TokenType.Asterisk, Data = "*" };
        public static readonly Token StarEquals = new Token { Type = TokenType.AsteriskEquals, Data = "*=" };

        public static readonly Token Equal = new Token { Type = TokenType.Equal, Data = "=" };
        public static readonly Token EqualEqual = new Token { Type = TokenType.EqualEqual, Data = "==" };
        public static readonly Token Lambda = new Token { Type = TokenType.Lambda, Data = "=>" };

        public static readonly Token Slash = new Token { Type = TokenType.Slash, Data = "/" };
        public static readonly Token SlashEquals = new Token { Type = TokenType.SlashEquals, Data = "/=" };

        public static readonly Token Bar = new Token { Type = TokenType.Bar, Data = "|" };
        public static readonly Token BarBar = new Token { Type = TokenType.BarBar, Data = "||" };
        public static readonly Token BarEquals = new Token { Type = TokenType.BarEquals, Data = "|=" };

        public static readonly Token Ampersand = new Token { Type = TokenType.Ampersand, Data = "&" };
        public static readonly Token AmpersandAmpersand = new Token { Type = TokenType.AmpersandAmpersand, Data = "&&" };
        public static readonly Token AmpersandEquals = new Token { Type = TokenType.AmpersandEquals, Data = "&=" };

        public static readonly Token Percent = new Token { Type = TokenType.Percent, Data = "%" };
        public static readonly Token PercentEquals = new Token { Type = TokenType.PercentEquals, Data = "%=" };

        public static readonly Token LessThan = new Token { Type = TokenType.LessThan, Data = "<" };
        public static readonly Token LessThanEquals = new Token { Type = TokenType.LessThanEquals, Data = "<=" };
        public static readonly Token BitShiftLeft = new Token { Type = TokenType.BitShiftLeft, Data = "<<" };
        public static readonly Token BitShiftLeftEquals = new Token { Type = TokenType.BitShiftLeftEquals, Data = "<<=" };

        public static readonly Token GreaterThan = new Token { Type = TokenType.GreaterThan, Data = ">" };
        public static readonly Token GreaterThanEquals = new Token { Type = TokenType.GreaterThanEquals, Data = ">=" };
        public static readonly Token BitShiftRight = new Token { Type = TokenType.BitShiftRight, Data = ">>" };
        public static readonly Token BitShiftRightEquals = new Token { Type = TokenType.BitShiftRightEquals, Data = ">>=" };

        public static readonly Token Bang = new Token { Type = TokenType.Bang, Data = "!" };
        public static readonly Token BangEquals = new Token { Type = TokenType.BangEquals, Data = "!=" };

        public static readonly Token Power = new Token { Type = TokenType.Power, Data = "^" };
        public static readonly Token PowerEquals = new Token { Type = TokenType.PowerEquals, Data = "^=" };

        public static readonly Token Tilde = new Token { Type = TokenType.Tilde, Data = "~" };

        public static readonly Token QuestionMark = new Token { Type = TokenType.QuestionMark, Data = "?" };
        public static readonly Token QuestionMarkQuestionMark = new Token { Type = TokenType.QuestionMarkQuestionMark, Data = "??" };

        public static readonly Token OpenBrace = new Token { Type = TokenType.OpenBrace, Data = "{" };
        public static readonly Token CloseBrace = new Token { Type = TokenType.CloseBrace, Data = "}" };
        public static readonly Token OpenBracket = new Token { Type = TokenType.OpenBracket, Data = "[" };
        public static readonly Token CloseBracket = new Token { Type = TokenType.CloseBracket, Data = "]" };
        public static readonly Token OpenParenthesis = new Token { Type = TokenType.OpenParenthesis, Data = "(" };
        public static readonly Token CloseParenthesis = new Token { Type = TokenType.CloseParenthesis, Data = ")" };

        public static readonly Token Dot = new Token { Type = TokenType.Dot, Data = "." };
        public static readonly Token Comma = new Token { Type = TokenType.Comma, Data = "," };
        public static readonly Token Colon = new Token { Type = TokenType.Colon, Data = ":" };
        public static readonly Token Semicolon = new Token { Type = TokenType.Semicolon, Data = ";" };

        #endregion

        protected char[] Text { get; set; }

        protected HashSet<string> Keywords { get; set; }
        protected int Index { get; set; }
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

        protected virtual void Tokenize() {
            for(Index = 0; Index < Text.Length; Index++) {
                RecognizeNextCharacter();

                switch(CurrentCharacter) {
                    case '~':
                        Tokens.Add(Tilde);
                        break;

                    case '?':
                        ParseQuestionMark();
                        break;

                    case '{':
                        Tokens.Add(OpenBrace);
                        break;

                    case '}':
                        Tokens.Add(CloseBrace);
                        break;

                    case '[':
                        Tokens.Add(OpenBracket);
                        break;

                    case ']':
                        Tokens.Add(CloseBracket);
                        break;

                    case '(':
                        Tokens.Add(OpenParenthesis);
                        break;

                    case ')':
                        Tokens.Add(CloseParenthesis);
                        break;

                    case '.':
                        Tokens.Add(Dot);
                        break;

                    case ',':
                        Tokens.Add(Comma);
                        break;

                    case ':':
                        Tokens.Add(Colon);
                        break;

                    case ';':
                        Tokens.Add(Semicolon);
                        break;

                    case '\r':
                        if(NextCharacter == '\n') {
                            Index++;
                        }

                        Tokens.Add(Eol);
                        break;

                    case '\n':
                        Tokens.Add(Eol);
                        break;

                    case '\t':
                        Tokens.Add(Tab);
                        break;

                    case ' ':
                        Tokens.Add(Space);
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

            Tokens.Add(new Token { Type = TokenType.NumericLiteral, Data = new string(Text, start, (Index - start) + 1) });
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

            Tokens.Add(new Token { Type = TokenType.NumericLiteral, Data = new string(Text, start, (Index - start) + 1) });
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

            Tokens.Add(new Token { Type = TokenType.NumericLiteral, Data = new string(Text, start, (Index - start) + 1) });
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

            Tokens.Add(new Token { Type = TokenType.StringLiteral, Data = new string(Text, start, (Index - start) + 1) });
        }

        protected virtual void ParsePower() {
            if(NextCharacter == '=') {
                Tokens.Add(PowerEquals);
                Index++;
                return;
            }

            Tokens.Add(Power);
        }

        protected virtual void ParseBang() {
            if(NextCharacter == '=') {
                Tokens.Add(BangEquals);
                Index++;
                return;
            }

            Tokens.Add(Bang);
        }

        protected virtual void ParseGreaterThan() {
            switch(NextCharacter) {
                case '=':
                    Tokens.Add(GreaterThanEquals);
                    Index++;
                    break;
                case '>':
                    if(NextNextCharacter == '=') {
                        Tokens.Add(BitShiftRightEquals);
                        Index += 2;
                        break;
                    }

                    Tokens.Add(BitShiftRight);
                    Index++;
                    break;
                default:
                    Tokens.Add(GreaterThan);
                    break;
            }
        }

        protected virtual void ParseLessThan() {
            switch(NextCharacter) {
                case '=':
                    Tokens.Add(LessThanEquals);
                    Index++;
                    break;
                case '<':
                    if(NextNextCharacter == '=') {
                        Tokens.Add(BitShiftLeftEquals);
                        Index += 2;
                        break;
                    }

                    Tokens.Add(BitShiftLeft);
                    Index++;
                    break;
                default:
                    Tokens.Add(LessThan);
                    break;
            }
        }

        protected virtual void ParsePercent() {
            if(NextCharacter == '=') {
                Tokens.Add(PercentEquals);
                Index++;
                return;
            }

            Tokens.Add(Percent);
        }

        protected virtual void ParseAmpersand() {
            switch(NextCharacter) {
                case '&':
                    Tokens.Add(AmpersandAmpersand);
                    Index++;
                    break;
                case '=':
                    Tokens.Add(AmpersandEquals);
                    Index++;
                    break;
                default:
                    Tokens.Add(Ampersand);
                    break;
            }
        }

        protected virtual void ParseBar() {
            switch(NextCharacter) {
                case '|':
                    Tokens.Add(BarBar);
                    Index++;
                    break;
                case '=':
                    Tokens.Add(BarEquals);
                    Index++;
                    break;
                default:
                    Tokens.Add(Bar);
                    break;
            }
        }

        protected virtual void ParseSlash() {
            if(NextCharacter == '=') {
                Tokens.Add(SlashEquals);
                Index++;
                return;
            }

            if(NextCharacter == '*') {
                // multiline comment
                var start = Index;
                Index += 2;
                while(Index < (Text.Length - 1)) {
                    if(Text[Index] == '*' && Text[Index + 1] == '/') {
                        Tokens.Add(new Token { Type = TokenType.MultilineComment, Data = new string(Text, start, (Index - start) + 1) });
                        start = -1;
                        break;
                    }

                    Index++;
                }

                if(start > -1) {
                    // didn't find the close marker (star slash) 
                    // adding an incomplete comment at the end I guess.
                    Tokens.Add(new Token { Type = TokenType.MultilineComment, Data = new string(Text, start, (Index - start) + 1) });
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
                        Tokens.Add(new Token { Type = TokenType.LineComment, Data = new string(Text, start, (Index - start) + 1) });
                        start = -1;
                        break;
                    }

                    Index++;
                }

                if(start > -1) {
                    // adding a comment at the end I guess.
                    Tokens.Add(new Token { Type = TokenType.LineComment, Data = new string(Text, start, (Index - start) + 1) });
                }

                Index++;
                return;
            }

            Tokens.Add(Slash);
        }

        protected virtual void ParseEquals() {
            if(NextCharacter == '=') {
                Tokens.Add(EqualEqual);
                Index++;
                return;
            }

            Tokens.Add(Equal);
        }

        protected virtual void ParseStar() {
            if(NextCharacter == '=') {
                Tokens.Add(StarEquals);
                Index++;
                return;
            }

            Tokens.Add(Star);
        }

        protected virtual void ParseMinus() {
            switch(NextCharacter) {
                case '-':
                    Tokens.Add(MinusMinus);
                    Index++;
                    break;
                case '=':
                    Tokens.Add(MinusEquals);
                    Index++;
                    break;
                case '>':
                    Tokens.Add(DashArrow);
                    Index++;
                    break;
                default:
                    Tokens.Add(Minus);
                    break;
            }
        }

        protected virtual void ParsePlus() {
            switch(NextCharacter) {
                case '+':
                    Tokens.Add(PlusPlus);
                    Index++;
                    break;
                case '=':
                    Tokens.Add(PlusEquals);
                    Index++;
                    break;
                default:
                    Tokens.Add(Plus);
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
                    Tokens.Add(new Token { Type = TokenType.Pound, Data = new string(Text, start, (Index - start) + 1) });
                    start = -1;
                    break;
                }

                Index++;
            }

            if(start > -1) {
                // adding directive at the end I guess.
                Tokens.Add(new Token { Type = TokenType.Pound, Data = new string(Text, start, (Index - start) + 1) });
            }

            Index++;
            return;
        }

        protected virtual void ParseQuestionMark() {
            if(NextCharacter == '?') {
                Tokens.Add(QuestionMarkQuestionMark);
                Index++;
                return;
            }

            Tokens.Add(QuestionMark);
        }

        /// <summary>
        ///   Returns true if the current character is in the UNICODE classes: Lu Ll Lt Lm Lo NI _
        /// </summary>
        protected bool IsCurrentCharacterIdentifierStartCharacter {
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
        protected bool IsCurrentCharacterIdentifierPartCharacter {
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
                Tokens.Add(new Token { Type = Keywords.Contains(identifier) ? TokenType.Keyword : TokenType.Identifier, Data = identifier });
                return;
            }

            Tokens.Add(new Token { Type = TokenType.Unknown, Data = CurrentCharacter });
        }

        public static List<Token> Tokenize(string text) {
            return Tokenize(string.IsNullOrEmpty(text) ? new char[0] : text.ToCharArray());
        }

        public static List<Token> Tokenize(char[] text) {
            var tokenizer = new Tokenizer(text);
            tokenizer.Tokenize();
            return tokenizer.Tokens;
        }
    }
}